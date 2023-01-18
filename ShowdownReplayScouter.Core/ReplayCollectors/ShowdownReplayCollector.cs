using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
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
                await foreach (var showdownReplay in RetrieveReplays(user))
                {
                    foreach (var showdownReplayUrl in CollectShowdownReplayUrl(showdownReplay, user, tiers, opponents))
                    {
                        yield return new CollectedReplay(showdownReplayUrl, user);
                    }
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
                var cachedPageUrl = await _cache.GetStringAsync(pageUrl).ConfigureAwait(false);
                if (json == cachedPageUrl)
                {
                    var cachedStrings = await _cache.GetStringAsync(fullUrl).ConfigureAwait(false);
                    if (cachedStrings != null)
                    {
                        cachedPages = JsonConvert.DeserializeObject<IEnumerable<string>>(cachedStrings);
                    }
                }
            }
            return cachedPages;
        }

        private IEnumerable<Uri> CollectShowdownReplayUrl(string json, string user, IEnumerable<string>? tiers = null, IEnumerable<string>? opponents = null)
        {
            var regexUser = RegexUtil.Regex(user);
            IEnumerable<string>? regexOpponents = null;
            if (opponents != null)
            {
                regexOpponents = opponents.Select((opponent) => RegexUtil.Regex(opponent));
            }
            if (regexOpponents == null)
            {
                regexOpponents = new List<string>();
            }

            var analyzedTiers = tiers;
            if (analyzedTiers != null)
            {
                analyzedTiers = analyzedTiers.Select((tier) => tier.ToLower());
            }

            var replayEntries = JsonConvert.DeserializeObject<List<ReplayEntry>>(json);
            if (replayEntries == null)
            {
                replayEntries = new List<ReplayEntry>();
            }

            foreach (var replayEntry in replayEntries)
            {
                var format = replayEntry.Format;
                if (analyzedTiers?.Any((tier) => tier == RegexUtil.Regex(format)) == true)
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
