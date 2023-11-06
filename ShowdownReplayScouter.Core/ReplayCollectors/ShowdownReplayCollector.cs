using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using ShowdownReplayScouter.Core.Data;
using ShowdownReplayScouter.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShowdownReplayScouter.Core.ReplayCollectors
{
    [Obsolete("Deprecated with the new Showdown API")]
    public class ShowdownReplayCollector : IReplayCollector
    {
        public ShowdownReplayCollector() : this(null) { }

        private readonly IDistributedCache? _cache;
        public ShowdownReplayCollector(IDistributedCache? cache)
        {
            _cache = cache;
        }

        public async IAsyncEnumerable<CollectedReplay> CollectReplaysAsync(ScoutingRequest scoutingRequest)
        {
            if (scoutingRequest.Users is not null && scoutingRequest.Tiers is not null && scoutingRequest.Tiers.Any())
            {
                foreach (var user in scoutingRequest.Users)
                {
                    foreach (var tier in scoutingRequest.Tiers)
                    {
                        var publicReplayUrls = new List<Uri>();
                        await foreach (var showdownReplay in RetrieveReplaysForUserAndTier(user, tier, scoutingRequest))
                        {
                            foreach (var showdownReplayUrl in CollectShowdownReplayUrl(showdownReplay, user, scoutingRequest))
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
                    await foreach (var showdownReplay in RetrieveReplaysForUser(user, scoutingRequest))
                    {
                        foreach (var showdownReplayUrl in CollectShowdownReplayUrl(showdownReplay, user, scoutingRequest))
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
                    catch (NullReferenceException)
                    {
                    }
                    if (cachedString == null)
                    {
                        continue;
                    }
                    var cachedLinks = JsonConvert.DeserializeObject<IEnumerable<CachedLink>>(cachedString);
                    if (cachedLinks == null)
                    {
                        continue;
                    }
                    foreach (var cachedLink in cachedLinks.Where(
                        (cachedLink) =>
                            !publicReplayUrls.Contains(cachedLink.ReplayLog)
                            && (scoutingRequest.Tiers?.Contains(RegexUtil.Regex(cachedLink.Format)) != false)
                    ))
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
                        foreach (var showdownReplayUrl in CollectShowdownReplayUrl(showdownReplay, null, scoutingRequest))
                        {
                            yield return new CollectedReplay(showdownReplayUrl, null);
                        }
                    }
                }
                yield break;
            }
        }

        private async IAsyncEnumerable<string> RetrieveReplaysForUserAndTier(string user, string tier, ScoutingRequest scoutingRequest)
        {
            var regexUser = RegexUtil.Regex(user);

            var page = 1;

            var fullUrl = $"https://replay.pokemonshowdown.com/search.json?user={regexUser}&format={tier}";
            var pageUrl = $"{fullUrl}&page={page}";

            var json = await Common.HttpClient.GetStringAsync(pageUrl).ConfigureAwait(false);

            var cachedPages = await GetCachedPages(fullUrl, pageUrl, json).ConfigureAwait(false);
            if (cachedPages != null)
            {
                foreach (var cachedPage in cachedPages)
                {
                    yield return cachedPage;
                }
                yield break;
            }

            _cache?.SetString(pageUrl, json);

            var cachingPages = new List<string>();

            while (!json.Contains("\"ERROR: page limit is 25\"") && !json.Contains("[]") && NeedToContinue(json, scoutingRequest))
            {
                yield return json;
                cachingPages.Add(json);
                page++;
                json = await Common.HttpClient.GetStringAsync($"{fullUrl}&page={page}").ConfigureAwait(false);
            }

            var cachingPagesString = JsonConvert.SerializeObject(cachingPages);
            _cache?.SetString(fullUrl, cachingPagesString);
        }

        private async IAsyncEnumerable<string> RetrieveReplaysForUser(string user, ScoutingRequest scoutingRequest)
        {
            var regexUser = RegexUtil.Regex(user);

            var page = 1;

            var fullUrl = $"https://replay.pokemonshowdown.com/search.json?user={regexUser}";
            var pageUrl = $"{fullUrl}&page={page}";

            var json = await Common.HttpClient.GetStringAsync(pageUrl).ConfigureAwait(false);

            var cachedPages = await GetCachedPages(fullUrl, pageUrl, json).ConfigureAwait(false);
            if (cachedPages != null)
            {
                foreach (var cachedPage in cachedPages)
                {
                    yield return cachedPage;
                }
                yield break;
            }

            _cache?.SetString(pageUrl, json);

            var cachingPages = new List<string>();

            while (!json.Contains("\"ERROR: page limit is 25\"") && !json.Contains("[]") && NeedToContinue(json, scoutingRequest))
            {
                yield return json;
                cachingPages.Add(json);
                page++;
                json = await Common.HttpClient.GetStringAsync($"{fullUrl}&page={page}").ConfigureAwait(false);
            }

            var cachingPagesString = JsonConvert.SerializeObject(cachingPages);
            _cache?.SetString(fullUrl, cachingPagesString);
        }

        private static bool NeedToContinue(string json, ScoutingRequest scoutingRequest)
        {
            var replayEntries = JsonConvert.DeserializeObject<List<ReplayEntry>>(json) ?? new List<ReplayEntry>();
            return replayEntries.Any((replayEntry) => OverMinimum(replayEntry, scoutingRequest));
        }

        private static bool OverMinimum(ReplayEntry replayEntry, ScoutingRequest scoutingRequest)
        {
            if (scoutingRequest.MinimumDate is not null)
            {
                if (replayEntry.Uploadtime < ToUnixTime(scoutingRequest.MinimumDate.Value))
                {
                    return false;
                }
            }
            return true;
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
            var pageUrl = $"{fullUrl}&page={1}";

            yield return await Common.HttpClient.GetStringAsync(pageUrl).ConfigureAwait(false);
        }

        private async Task<IEnumerable<string>?> GetCachedPages(string fullUrl, string pageUrl, string json)
        {
            IEnumerable<string>? cachedPages = null;
            if (_cache != null)
            {
                string? cachedPageUrl = null;
                try
                {
                    cachedPageUrl = await _cache.GetStringAsync(pageUrl).ConfigureAwait(false);
                }
                catch (NullReferenceException)
                {
                }
                if (json == cachedPageUrl)
                {
                    string? cachedStrings = null;
                    try
                    {
                        cachedStrings = await _cache.GetStringAsync(fullUrl).ConfigureAwait(false);
                    }
                    catch (NullReferenceException)
                    {
                    }
                    if (cachedStrings != null)
                    {
                        cachedPages = JsonConvert.DeserializeObject<IEnumerable<string>>(cachedStrings);
                    }
                }
            }
            return cachedPages;
        }

        private static IEnumerable<Uri> CollectShowdownReplayUrl(string json, string? user, ScoutingRequest scoutingRequest)
        {
            var opponents = scoutingRequest.Opponents;

            var regexUser = RegexUtil.Regex(user);
            IEnumerable<string>? regexOpponents = null;
            if (opponents is not null)
            {
                regexOpponents = opponents.Select((opponent) => RegexUtil.Regex(opponent));
            }
            regexOpponents ??= new List<string>();

            var analyzedTiers = scoutingRequest.Tiers;
            if (analyzedTiers is not null)
            {
                analyzedTiers = analyzedTiers.Select((tier) => tier.ToLower());
            }

            foreach (var replayEntry in
                JsonConvert.DeserializeObject<List<ReplayEntry>>(json) ?? new List<ReplayEntry>())
            {
                var format = replayEntry.Format;
                if (!IsInScope(replayEntry, scoutingRequest))
                {
                    continue;
                }

                if (analyzedTiers?.Any() != true || analyzedTiers.Any((tier) => tier == RegexUtil.Regex(format)))
                {
                    var validatedOpponent = true;
                    if (opponents?.Any() == true && user is not null)
                    {
                        validatedOpponent = false;
                        var countPlayers = 0;
                        var regexPlayerOne = RegexUtil.Regex(replayEntry.P1);
                        if (regexPlayerOne == regexUser || regexOpponents.Any((opponent) => opponent == regexPlayerOne))
                        {
                            countPlayers++;
                        }

                        var regexPlayerTwo = RegexUtil.Regex(replayEntry.P2);
                        if (regexPlayerTwo == regexUser || regexOpponents.Any((opponent) => opponent == regexPlayerTwo))
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
                        yield return new Uri($"https://replay.pokemonshowdown.com/{replayEntry.Id}");
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
