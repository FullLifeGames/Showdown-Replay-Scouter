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
    public class ShowdownReplayCollector : IReplayCollector
    {
        public ShowdownReplayCollector() : this(null) { }

        private readonly IDistributedCache? _cache;
        public ShowdownReplayCollector(IDistributedCache? cache)
        {
            _cache = cache;
        }

        public async IAsyncEnumerable<CollectedReplay> CollectReplaysAsync(IEnumerable<string> users, IEnumerable<string>? tiers = null, IEnumerable<string>? opponents = null)
        {
            foreach (var user in users)
            {
                var publicReplayUrls = new List<Uri>();
                await foreach (var showdownReplay in RetrieveReplays(user))
                {
                    foreach (var showdownReplayUrl in CollectShowdownReplayUrl(showdownReplay, user, tiers, opponents))
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
                        && (tiers?.Contains(RegexUtil.Regex(cachedLink.Format)) != false)
                ))
                {
                    yield return new CollectedReplay(cachedLink.ReplayLog, user);
                }
            }
        }

        private async IAsyncEnumerable<string> RetrieveReplays(string user)
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

            while (!json.Contains("\"ERROR: page limit is 25\"") && !json.Contains("[]"))
            {
                yield return json;
                cachingPages.Add(json);
                page++;
                json = await Common.HttpClient.GetStringAsync($"{fullUrl}&page={page}").ConfigureAwait(false);
            }

            var cachingPagesString = JsonConvert.SerializeObject(cachingPages);
            _cache?.SetString(fullUrl, cachingPagesString);
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

        private static IEnumerable<Uri> CollectShowdownReplayUrl(string json, string user, IEnumerable<string>? tiers = null, IEnumerable<string>? opponents = null)
        {
            var regexUser = RegexUtil.Regex(user);
            IEnumerable<string>? regexOpponents = null;
            if (opponents != null)
            {
                regexOpponents = opponents.Select((opponent) => RegexUtil.Regex(opponent));
            }
            regexOpponents ??= new List<string>();

            var analyzedTiers = tiers;
            if (analyzedTiers != null)
            {
                analyzedTiers = analyzedTiers.Select((tier) => tier.ToLower());
            }

            foreach (var replayEntry in
                JsonConvert.DeserializeObject<List<ReplayEntry>>(json) ?? new List<ReplayEntry>())
            {
                var format = replayEntry.Format;
                if (analyzedTiers == null || analyzedTiers.Any((tier) => tier == RegexUtil.Regex(format)))
                {
                    var validatedOpponent = true;
                    if (opponents?.Any() == true)
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
    }
}
