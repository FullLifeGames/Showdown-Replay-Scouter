using ShowdownReplayScouter.Core.Util;
using System;
using System.Collections.Generic;

namespace ShowdownReplayScouter.Core.ReplayCollectors
{
    public class ShowdownReplayCollector : IReplayCollector
    {
        public async IAsyncEnumerable<Uri> CollectReplaysAsync(string user, string tier = null, string opponent = null)
        {
            await foreach (var showdownReplay in RetrieveHtml(user))
            {
                foreach (var showdownReplayUrl in CollectShowdownReplayUrl(showdownReplay, user, tier, opponent))
                {
                    yield return showdownReplayUrl;
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

        private IEnumerable<Uri> CollectShowdownReplayUrl(string html, string user, string tier = null, string opponent = null)
        {
            var regexUser = RegexUtil.Regex(user);
            string regexOpponent = null;
            if (opponent != null)
            {
                regexOpponent = RegexUtil.Regex(opponent);
            }

            var analyzedTier = tier;
            if (analyzedTier != null)
            {
                analyzedTier = analyzedTier.ToLower();
            }

            foreach (var line in html.Split('\n'))
            {
                if (line.Contains("<small>"))
                {
                    var tmpTier = line[(line.IndexOf("<small>") + 7)..line.IndexOf("<br")];
                    if (analyzedTier == null || analyzedTier == RegexUtil.Regex(tmpTier))
                    {
                        var validatedOpponent = true;
                        if (opponent != null)
                        {
                            validatedOpponent = false;
                            var countPlayers = 0;
                            var temp = line[(line.IndexOf("<strong>") + 8)..];
                            var playerone = temp[..temp.IndexOf("</")];
                            var regexPlayerOne = RegexUtil.Regex(playerone);
                            if (regexPlayerOne == regexUser || regexPlayerOne == regexOpponent)
                            {
                                countPlayers++;
                            }

                            temp = temp[(temp.IndexOf("<strong>") + 8)..];
                            var playertwo = temp[..temp.IndexOf("</")];
                            var regexPlayerTwo = RegexUtil.Regex(playertwo);
                            if (regexPlayerTwo == regexUser || regexPlayerTwo == regexOpponent)
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
