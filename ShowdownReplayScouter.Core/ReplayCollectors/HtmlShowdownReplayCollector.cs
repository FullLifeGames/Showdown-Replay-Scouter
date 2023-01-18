using ShowdownReplayScouter.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShowdownReplayScouter.Core.ReplayCollectors
{
    [Obsolete("Legacy implementation of the replay collection", true)]
    public class HtmlShowdownReplayCollector : IReplayCollector
    {
        public async IAsyncEnumerable<CollectedReplay> CollectReplaysAsync(IEnumerable<string> users, IEnumerable<string>? tiers = null, IEnumerable<string>? opponents = null)
        {
            foreach (var user in users)
            {
                await foreach (var showdownReplay in RetrieveHtml(user))
                {
                    foreach (var showdownReplayUrl in CollectShowdownReplayUrl(showdownReplay, user, tiers, opponents))
                    {
                        yield return new CollectedReplay(showdownReplayUrl, user);
                    }
                }
            }
        }

        private static async IAsyncEnumerable<string> RetrieveHtml(string user)
        {
            var regexUser = RegexUtil.Regex(user);

            var page = 1;
            var pageHtml = await Common.HttpClient.GetStringAsync($"https://replay.pokemonshowdown.com/search?user={regexUser}&page={page}").ConfigureAwait(false);

            while (!pageHtml.Contains("<li>No results found</li>") && !pageHtml.Contains("<li>Can't search any further back</li>"))
            {
                yield return pageHtml;
                page++;
                pageHtml = await Common.HttpClient.GetStringAsync($"https://replay.pokemonshowdown.com/search?user={regexUser}&page={page}").ConfigureAwait(false);
            }
        }

        private IEnumerable<Uri> CollectShowdownReplayUrl(string html, string user, IEnumerable<string>? tiers = null, IEnumerable<string>? opponents = null)
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

            foreach (var line in html.Split('\n'))
            {
                if (line.Contains("<small>"))
                {
                    var tmpTier = line[(line.IndexOf("<small>") + 7)..line.IndexOf("<br")];
                    if (analyzedTiers?.Any((tier) => tier == RegexUtil.Regex(tmpTier)) == true)
                    {
                        var validatedOpponent = true;
                        if (opponents?.Any() == true)
                        {
                            validatedOpponent = false;
                            var countPlayers = 0;
                            var temp = line[(line.IndexOf("<strong>") + 8)..];
                            var playerone = temp[..temp.IndexOf("</")];
                            var regexPlayerOne = RegexUtil.Regex(playerone);
                            if (regexPlayerOne == regexUser || regexOpponents.Any((opponent) => opponent == regexPlayerOne))
                            {
                                countPlayers++;
                            }

                            temp = temp[(temp.IndexOf("<strong>") + 8)..];
                            var playertwo = temp[..temp.IndexOf("</")];
                            var regexPlayerTwo = RegexUtil.Regex(playertwo);
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
                            var tempBattle = line[(line.IndexOf("\"") + 1)..];
                            tempBattle = tempBattle[..tempBattle.IndexOf("\"")];
                            tempBattle = "http://replay.pokemonshowdown.com" + tempBattle;
                            yield return new Uri(tempBattle);
                        }
                    }
                }
            }
        }
    }
}
