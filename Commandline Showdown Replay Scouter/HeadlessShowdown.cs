using Commandline_Showdown_Replay_Scouter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Showdown_Replay_Scouter
{
    public class HeadlessShowdown
    {
        private string user;
        private List<string> htmlText;
        private HttpClient HttpClient;

        public HeadlessShowdown(string user, List<string> htmlText, HttpClient HttpClient)
        {
            this.user = user;
            this.htmlText = htmlText;
            this.HttpClient = HttpClient;
        }

        public async Task GetReplaysForUser()
        {
            int page = 1;
            string pageHtml = await HttpClient.GetStringAsync("https://replay.pokemonshowdown.com/search?user=" + user + "&page=" + page);

            while (!pageHtml.Contains("Can't search any further back"))
            {
                htmlText.Add(pageHtml);
                pageHtml = await HttpClient.GetStringAsync("https://replay.pokemonshowdown.com/search?user=" + user + "&page=" + page);
                page++;
            }
        }

        public void ParseShowdownReplays(List<string> html, string tier, bool oppCheck, string noRegexUser, string opp, List<string> links)
        {
            foreach (string showdownReplays in html)
            {
                string tempBattle = "";
                foreach (string line in showdownReplays.Split('\n'))
                {
                    if (line.Contains("<small>"))
                    {
                        string tmpTier = line.Substring(line.IndexOf("<small>") + 7, line.IndexOf("<br") - (line.IndexOf("<small>") + 7));
                        if (tier == null || tier == Program.Regex(tmpTier))
                        {
                            if (oppCheck)
                            {
                                int countPlayers = 0;
                                string temp = line.Substring(line.IndexOf("<strong>") + 8);
                                string playerone = temp.Substring(0, temp.IndexOf("</"));
                                if (Program.Regex(playerone) == Program.Regex(noRegexUser) || Program.Regex(playerone) == opp)
                                {
                                    countPlayers++;
                                }
                                temp = temp.Substring(temp.IndexOf("<strong>") + 8);
                                string playertwo = temp.Substring(0, temp.IndexOf("</"));
                                if (Program.Regex(playertwo) == Program.Regex(noRegexUser) || Program.Regex(playertwo) == opp)
                                {
                                    countPlayers++;
                                }
                                if (countPlayers == 2)
                                {
                                    oppCheck = false;
                                }
                            }
                            if (!oppCheck)
                            {
                                tempBattle = line.Substring(line.IndexOf("\"") + 1);
                                tempBattle = tempBattle.Substring(0, tempBattle.IndexOf("\""));
                                tempBattle = "http://replay.pokemonshowdown.com" + tempBattle;
                                links.Add(tempBattle);
                            }
                        }
                    }
                }
            }
        }
    }
}
