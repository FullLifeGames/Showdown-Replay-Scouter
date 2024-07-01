using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.Util;

namespace ShowdownReplayScouter.Core.ReplayCollectors
{
    public class ApiShowdownReplayCollector : IReplayCollector
    {
        public ApiShowdownReplayCollector()
            : this(null) { }

        private readonly IDistributedCache? _cache;

        public ApiShowdownReplayCollector(IDistributedCache? cache)
        {
            _cache = cache;
        }

        public async IAsyncEnumerable<CollectedReplay> CollectReplaysAsync(
            ScoutingRequest scoutingRequest
        )
        {
            if (
                scoutingRequest.Users is not null
                && scoutingRequest.Tiers is not null
                && scoutingRequest.Tiers.Any()
            )
            {
                foreach (var user in scoutingRequest.Users)
                {
                    foreach (var tier in scoutingRequest.Tiers)
                    {
                        var publicReplayUrls = new List<Uri>();
                        await foreach (
                            var showdownReplay in RetrieveReplaysForUserAndTier(user, tier)
                        )
                        {
                            foreach (
                                var showdownReplayUrl in CollectShowdownReplayUrl(
                                    showdownReplay,
                                    user,
                                    scoutingRequest
                                )
                            )
                            {
                                publicReplayUrls.Add(showdownReplayUrl);
                                yield return new CollectedReplay(showdownReplayUrl, user);
                            }
                        }
                    }
                }
                yield break;
            }
            else if (scoutingRequest.Users is not null)
            {
                foreach (var user in scoutingRequest.Users)
                {
                    var publicReplayUrls = new List<Uri>();
                    await foreach (
                        var showdownReplay in RetrieveReplaysForUserAndTier(user, tier: null)
                    )
                    {
                        foreach (
                            var showdownReplayUrl in CollectShowdownReplayUrl(
                                showdownReplay,
                                user,
                                scoutingRequest
                            )
                        )
                        {
                            publicReplayUrls.Add(showdownReplayUrl);
                            yield return new CollectedReplay(showdownReplayUrl, user);
                        }
                    }

                    // Retrieve from cache
                    if (_cache == null)
                    {
                        continue;
                    }
                    string? cachedString = null;
                    try
                    {
                        cachedString = _cache.GetString($"replays-{user}");
                    }
                    catch (NullReferenceException) { }
                    if (cachedString == null)
                    {
                        continue;
                    }
                    var cachedLinks = JsonConvert.DeserializeObject<IEnumerable<CachedLink>>(
                        cachedString
                    );
                    if (cachedLinks == null)
                    {
                        continue;
                    }
                    foreach (
                        var cachedLink in cachedLinks.Where(
                            (cachedLink) =>
                                !publicReplayUrls.Contains(cachedLink.ReplayLog)
                                && (
                                    scoutingRequest.Tiers?.Contains(
                                        RegexUtil.Regex(cachedLink.Format)
                                    ) != false
                                )
                        )
                    )
                    {
                        yield return new CollectedReplay(cachedLink.ReplayLog, user);
                    }
                }
                yield break;
            }
            else if (scoutingRequest.Tiers is not null)
            {
                foreach (var tier in scoutingRequest.Tiers)
                {
                    await foreach (var showdownReplay in RetrieveReplaysForTier(tier))
                    {
                        foreach (
                            var showdownReplayUrl in CollectShowdownReplayUrl(
                                showdownReplay,
                                null,
                                scoutingRequest
                            )
                        )
                        {
                            yield return new CollectedReplay(showdownReplayUrl, null);
                        }
                    }
                }
                yield break;
            }
        }

        private async IAsyncEnumerable<string> RetrieveReplaysForUserAndTier(
            string user,
            string? tier
        )
        {
            var regexUser = RegexUtil.Regex(user);

            var fullUrl = $"https://replay.pokemonshowdown.com/search.json?user={regexUser}";
            fullUrl += tier is not null ? $"&format={tier}" : "";
            var currentUrl = $"{fullUrl}";

            var response = await Common.HttpClient.GetAsync(currentUrl).ConfigureAwait(false);
            // Broken request
            if (!response.IsSuccessStatusCode)
            {
                yield break;
            }
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var cachedPages = await GetCachedPages(fullUrl, currentUrl, json).ConfigureAwait(false);
            if (cachedPages != null)
            {
                foreach (var cachedPage in cachedPages)
                {
                    yield return cachedPage;
                }
                yield break;
            }

            yield return json;
            var replayList = JsonConvert.DeserializeObject<ReplayEntry[]>(json);
            long? before = replayList?.LastOrDefault()?.Uploadtime;

            _cache?.SetString(currentUrl, json);

            List<string> cachingPages = [json];

            while (replayList?.Length == 51)
            {
                response = await Common
                    .HttpClient.GetAsync($"{fullUrl}&before={before}")
                    .ConfigureAwait(false);
                // Broken request
                if (!response.IsSuccessStatusCode)
                {
                    yield break;
                }
                json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                replayList = JsonConvert.DeserializeObject<ReplayEntry[]>(json);
                before = replayList?.LastOrDefault()?.Uploadtime;
                // Error handling, sometimes "<" is returned from the API
                if (json.StartsWith("<"))
                {
                    // Assume that the request will work
                    json = await Common
                        .HttpClient.GetStringAsync($"{fullUrl}&before={before}")
                        .ConfigureAwait(false);
                    // If return twice, at least don't break everything
                    if (json.StartsWith("<"))
                    {
                        break;
                    }
                }
                yield return json;
                cachingPages.Add(json);
            }

            var cachingPagesString = JsonConvert.SerializeObject(cachingPages);
            _cache?.SetString(fullUrl, cachingPagesString);
        }

        private static bool IsInScope(ReplayEntry replayEntry, ScoutingRequest scoutingRequest)
        {
            if (scoutingRequest.MinimumDate is not null)
            {
                if (replayEntry.Uploadtime < ToUnixTime(scoutingRequest.MinimumDate.Value))
                {
                    return false;
                }
            }
            if (scoutingRequest.MaximumDate is not null)
            {
                if (replayEntry.Uploadtime > ToUnixTime(scoutingRequest.MaximumDate.Value))
                {
                    return false;
                }
            }
            return true;
        }

        private static async IAsyncEnumerable<string> RetrieveReplaysForTier(string tier)
        {
            var regexTier = RegexUtil.Regex(tier);

            var fullUrl = $"https://replay.pokemonshowdown.com/search.json?format={regexTier}";

            yield return await Common.HttpClient.GetStringAsync(fullUrl).ConfigureAwait(false);
        }

        private async Task<IEnumerable<string>?> GetCachedPages(
            string fullUrl,
            string pageUrl,
            string json
        )
        {
            IEnumerable<string>? cachedPages = null;
            if (_cache != null)
            {
                string? cachedPageUrl = null;
                try
                {
                    cachedPageUrl = await _cache.GetStringAsync(pageUrl).ConfigureAwait(false);
                }
                catch (NullReferenceException) { }
                if (json == cachedPageUrl)
                {
                    string? cachedStrings = null;
                    try
                    {
                        cachedStrings = await _cache.GetStringAsync(fullUrl).ConfigureAwait(false);
                    }
                    catch (NullReferenceException) { }
                    if (cachedStrings != null)
                    {
                        cachedPages = JsonConvert.DeserializeObject<IEnumerable<string>>(
                            cachedStrings
                        );
                    }
                }
            }
            return cachedPages;
        }

        private static IEnumerable<Uri> CollectShowdownReplayUrl(
            string json,
            string? user,
            ScoutingRequest scoutingRequest
        )
        {
            var opponents = scoutingRequest.Opponents;

            var regexUser = RegexUtil.Regex(user);
            IEnumerable<string>? regexOpponents = null;
            if (opponents is not null)
            {
                regexOpponents = opponents.Select(RegexUtil.Regex);
            }
            regexOpponents ??= [];

            var analyzedTiers = scoutingRequest.Tiers;
            if (analyzedTiers is not null)
            {
                analyzedTiers = analyzedTiers.Select((tier) => tier.ToLower());
            }

            foreach (
                var replayEntry in JsonConvert.DeserializeObject<List<ReplayEntry>>(json) ?? []
            )
            {
                var format = replayEntry.Format;
                if (!IsInScope(replayEntry, scoutingRequest))
                {
                    continue;
                }

                if (
                    analyzedTiers?.Any() != true
                    || analyzedTiers.Any((tier) => tier == RegexUtil.Regex(format))
                )
                {
                    var validatedOpponent = true;
                    if (opponents?.Any() == true && user is not null)
                    {
                        validatedOpponent = false;
                        var countPlayers = 0;
                        var regexPlayerOne = RegexUtil.Regex(replayEntry.Players[0]);
                        if (
                            regexPlayerOne == regexUser
                            || regexOpponents.Any((opponent) => opponent == regexPlayerOne)
                        )
                        {
                            countPlayers++;
                        }

                        var regexPlayerTwo = RegexUtil.Regex(replayEntry.Players[1]);
                        if (
                            regexPlayerTwo == regexUser
                            || regexOpponents.Any((opponent) => opponent == regexPlayerTwo)
                        )
                        {
                            countPlayers++;
                        }

                        if (countPlayers == 2)
                        {
                            validatedOpponent = true;
                        }
                    }
                    if (validatedOpponent)
                    {
                        yield return new Uri(
                            $"https://replay.pokemonshowdown.com/{replayEntry.Id}"
                        );
                    }
                }
            }
        }

        // Convert datetime to UNIX time
        public static long ToUnixTime(DateTime dateTime)
        {
            DateTimeOffset dto = new(dateTime.ToUniversalTime());
            return dto.ToUnixTimeSeconds();
        }
    }
}
