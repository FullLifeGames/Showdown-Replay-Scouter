using Newtonsoft.Json;
using ShowdownReplayScouter.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShowdownReplayScouter.Core.ReplayCollectors
{
    public class ShowdownReplayCollector : IReplayCollector
    {
        public async IAsyncEnumerable<CollectedReplay> CollectReplaysAsync(IEnumerable<string> users, IEnumerable<string> tiers = null, IEnumerable<string> opponents = null)
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

        private static async IAsyncEnumerable<string> RetrieveReplays(string user)
        {
            var regexUser = RegexUtil.Regex(user);

            var page = 1;
            var json = await Common.HttpClient.GetStringAsync($"https://replay.pokemonshowdown.com/search.json?user={regexUser}&page={page}").ConfigureAwait(false);

            while (!json.Contains("\"ERROR: page limit is 25\"") && !json.Contains("[]"))
            {
                yield return json;
                page++;
                json = await Common.HttpClient.GetStringAsync($"https://replay.pokemonshowdown.com/search.json?user={regexUser}&page={page}").ConfigureAwait(false);
            }
        }

        private IEnumerable<Uri> CollectShowdownReplayUrl(string json, string user, IEnumerable<string> tiers = null, IEnumerable<string> opponents = null)
        {
            var regexUser = RegexUtil.Regex(user);
            IEnumerable<string> regexOpponents = null;
            if (opponents != null)
            {
                regexOpponents = opponents.Select((opponent) => RegexUtil.Regex(opponent));
            }

            var analyzedTiers = tiers;
            if (analyzedTiers != null)
            {
                analyzedTiers = analyzedTiers.Select((tier) => tier.ToLower());
            }

            var replayEntries = JsonConvert.DeserializeObject<List<ReplayEntry>>(json);

            foreach (var replayEntry in replayEntries)
            {
                var format = replayEntry.format;
                if (analyzedTiers?.Any((tier) => tier == RegexUtil.Regex(format)) == true)
                {
                    var validatedOpponent = true;
                    if (opponents?.Any() == true)
                    {
                        validatedOpponent = false;
                        var countPlayers = 0;
                        var regexPlayerOne = RegexUtil.Regex(replayEntry.p1);
                        if (regexPlayerOne == regexUser || regexOpponents.Any((opponent) => opponent == regexPlayerOne))
                        {
                            countPlayers++;
                        }

                        var regexPlayerTwo = RegexUtil.Regex(replayEntry.p2);
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
                        yield return new Uri($"https://replay.pokemonshowdown.com/{replayEntry.id}");
                    }
                }
            }
        }
    }
}
