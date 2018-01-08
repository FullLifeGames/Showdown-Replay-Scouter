using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace Showdown_Replay_Scouter
{
    internal class Tournament
    {
        private bool tournament;
        private WebClient client;
        private List<string> links;
        private string tier;
        private string tempBattle;
        private string opp;

        private string officialTournamentSite = "http://www.smogon.com/forums/forums/tournaments.34/";
        private string ostForum = "http://www.smogon.com/forums/forums/official-smogon-tournament.463/";

        public Tournament(bool tournament, WebClient client, List<string> links, string tier, string tempBattle, string opp)
        {
            this.tournament = tournament;
            this.client = client;
            this.links = links;
            this.tier = tier;
            this.tempBattle = tempBattle;
            this.opp = opp;
        }

        private static Regex rgx = new Regex("[^a-zA-Z0-9]");
        private static string Regex(string toFilter)
        {
            toFilter = rgx.Replace(toFilter, "");
            return toFilter.ToLower();
        }

        private static Regex rgxWithSpace = new Regex("[^a-zA-Z0-9 ]");
        private static string RegexWithSpace(string toFilter)
        {
            toFilter = rgxWithSpace.Replace(toFilter, "");
            return toFilter.ToLower();
        }


        public void AddReplaysForUser(string user, string userWithSpace)
        {
            string smogonMain = client.DownloadString("http://www.smogon.com/forums/");
            bool scanStartOne = false;

            Dictionary<string, string> tournamentToLinks = new Dictionary<string, string>();
            foreach (string line in smogonMain.Split('\n'))
            {
                if (scanStartOne)
                {
                    if (line.Contains("class=\"subNodeLink subNodeLink--forum"))
                    {
                        string tourName = line.Substring(line.IndexOf(">") + 1);
                        tourName = tourName.Substring(0, tourName.IndexOf("<"));

                        string tourUrl = line.Substring(line.IndexOf("\"") + 1);
                        tourUrl = tourUrl.Substring(0, tourUrl.IndexOf("\""));
                        tourUrl = "http://www.smogon.com" + tourUrl;

                        tournamentToLinks.Add(tourName, tourUrl);
                    }
                    else if (line.Contains("node-stats\""))
                    {
                        scanStartOne = false;
                    }
                }
                else if (line.Contains(">Tournaments<"))
                {
                    scanStartOne = true;
                }
            }
            
            ScanOSTThreads(user, userWithSpace);

            foreach (KeyValuePair<string, string> kv in tournamentToLinks)
            {
                string site = client.DownloadString(kv.Value);
                int pages = 1;
                if (site.Contains("<nav class=\"pageNavWrapper"))
                {
                    string temp = site;
                    while (temp.Contains("pageNav-page"))
                    {
                        temp = temp.Substring(temp.IndexOf("pageNav-page") + "pageNav-page".Length);
                    }
                    temp = temp.Substring(temp.IndexOf(">") + 1);
                    temp = temp.Substring(temp.IndexOf(">") + 1);
                    temp = temp.Substring(0, temp.IndexOf("<"));
                    pages = int.Parse(temp);
                }

                for (int pageCount = 1; pageCount <= pages; pageCount++)
                {
                    site = client.DownloadString(kv.Value + "page-" + pageCount);

                    foreach (string line in site.Split('\n'))
                    {
                        if (line.Contains("data-preview-url"))
                        {
                            string tempInside = line.Substring(line.IndexOf("data-preview-url") + "data-preview-url".Length);
                            tempInside = tempInside.Substring(tempInside.IndexOf("\"") + 1);
                            if (!tempInside.Contains("/preview"))
                            {
                                continue;
                            }
                            tempInside = tempInside.Substring(0, tempInside.IndexOf("/preview") + 1);
                            string url = "http://www.smogon.com" + tempInside;
                            if (!url.Contains("season-") && !url.Contains("signup"))
                            {
                                Console.WriteLine("Currently Scanning: " + url);
                                int beforeCount = links.Count;
                                AnalyzeTopic(url, kv.Key, client, user, userWithSpace);
                                int afterCount = links.Count;
                                Console.WriteLine("Added " + (afterCount - beforeCount) + " Replays");
                                Console.WriteLine();
                            }
                        }
                    }
                }
            }
        }

        private void ScanOSTThreads(string user, string userWithSpace)
        {
            foreach (string siteString in new string[] { ostForum, officialTournamentSite })
            {
                string bigSite = client.DownloadString(siteString);
                int bigPages = 1;
                if (bigSite.Contains("<nav class=\"pageNavWrapper"))
                {
                    string temp = bigSite;
                    while (temp.Contains("pageNav-page"))
                    {
                        temp = temp.Substring(temp.IndexOf("pageNav-page") + "pageNav-page".Length);
                    }
                    temp = temp.Substring(temp.IndexOf(">") + 1);
                    temp = temp.Substring(temp.IndexOf(">") + 1);
                    temp = temp.Substring(0, temp.IndexOf("<"));
                    bigPages = int.Parse(temp);
                }

                for (int pageCount = 1; pageCount <= bigPages; pageCount++)
                {
                    bigSite = client.DownloadString(siteString + "page-" + pageCount);

                    foreach (string line in bigSite.Split('\n'))
                    {
                        if (line.Contains("data-preview-url"))
                        {
                            string tempInside = line.Substring(line.IndexOf("data-preview-url") + "data-preview-url".Length);
                            tempInside = tempInside.Substring(tempInside.IndexOf("\"") + 1);
                            if (!tempInside.Contains("/preview"))
                            {
                                continue;
                            }
                            tempInside = tempInside.Substring(0, tempInside.IndexOf("/preview") + 1);
                            string url = "http://www.smogon.com" + tempInside;
                            if (!url.Contains("season-") && !url.Contains("signup") && url.Contains("official-smogon-tournament"))
                            {
                                Console.WriteLine("Currently Scanning: " + url);
                                int beforeCount = links.Count;
                                AnalyzeTopic(url, "OST", client, user, userWithSpace);
                                int afterCount = links.Count;
                                Console.WriteLine("Added " + (afterCount - beforeCount) + " Replays");
                                Console.WriteLine();
                            }
                        }
                    }
                }
            }
        }

        private void AnalyzeTopic(string url, string tour, WebClient client, string user, string userWithSpace)
        {
            try
            {
                string site = client.DownloadString(url);
                int pages = 1;
                if (site.Contains("<nav class=\"pageNavWrapper"))
                {
                    string temp = site;
                    while (temp.Contains("pageNav-page"))
                    {
                        temp = temp.Substring(temp.IndexOf("pageNav-page") + "pageNav-page".Length);
                    }
                    temp = temp.Substring(temp.IndexOf(">") + 1);
                    temp = temp.Substring(temp.IndexOf(">") + 1);
                    temp = temp.Substring(0, temp.IndexOf("<"));
                    pages = int.Parse(temp);
                }

                for (int pageCount = 1; pageCount <= pages; pageCount++)
                {
                    site = client.DownloadString(url + "page-" + pageCount);

                    bool blockStarted = false;
                    string blockText = "";

                    bool postStarted = false;
                    string postLink = "";
                    int postLikes = 0;
                    DateTime postDate = DateTime.Now;

                    string postedBy = "";

                    bool likeStarted = false;

                    bool timerHeader = false;

                    string lastLine = "";

                    bool canTakeReplay = false;

                    List<string> currentTeams = new List<string>();

                    foreach (string line in site.Split('\n'))
                    {
                        HandleLine(url, tour, pageCount, ref blockStarted, ref blockText, ref postStarted, ref postLink, ref postLikes, ref postDate, ref postedBy, ref likeStarted, ref timerHeader, currentTeams, line, ref lastLine, user, ref canTakeReplay, userWithSpace);
                    }
                }
            }
            catch (WebException e)
            {
                Console.WriteLine("WebException bei: " + url);
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }

        private void HandleLine(string url, string tour, int pageCount, ref bool blockStarted, ref string blockText, ref bool postStarted, ref string postLink, ref int postLikes, ref DateTime postDate, ref string postedBy, ref bool likeStarted, ref bool timerHeader, List<string> currentTeams, string line, ref string lastLine, string user, ref bool canTakeReplay, string userWithSpace)
        {
            if (line.Contains("<article class=\"message message--post js-post js-inlineModContainer"))
            {
                postStarted = true;
                canTakeReplay = false;
            }
            else if (line.StartsWith("\t</article>"))
            {
                postStarted = false;
                canTakeReplay = false;
            }
            else if (line.Contains("data-author=\""))
            {
                postedBy = line.Substring(line.IndexOf("data-author") + 5);
                postedBy = postedBy.Substring(postedBy.IndexOf("\"") + 1);
                postedBy = postedBy.Substring(0, postedBy.IndexOf("\""));
            }
            else if (line.Contains("replay.pokemonshowdown.com/") && (RegexWithSpace(line).Contains(" " + user + " ") || RegexWithSpace(line).Contains(" " + userWithSpace + " ") || canTakeReplay)) 
            {
                string tempLine = line;
                while (tempLine.Contains("replay.pokemonshowdown.com/"))
                {
                    tempLine = tempLine.Substring(tempLine.IndexOf("replay.pokemonshowdown.com/"));
                    int quot = tempLine.Contains("\"") ? tempLine.IndexOf("\"") : int.MaxValue;
                    int arrow = tempLine.Contains("<") ? tempLine.IndexOf("<") : int.MaxValue;
                    string link;
                    if (arrow < quot)
                    {
                        link = tempLine.Substring(0, tempLine.IndexOf("<"));
                    }
                    else
                    {
                        link = tempLine.Substring(0, tempLine.IndexOf("\""));
                    }
                    if (tier != null)
                    {
                        string endPart = link.Replace("replay.pokemonshowdown.com/", "");
                        endPart = endPart.Substring(0, endPart.LastIndexOf("-"));
                        if (endPart.Contains("-"))
                        {
                            endPart = endPart.Substring(endPart.IndexOf("-") + 1);
                        }
                        if (Regex(tier) == Regex(endPart))
                        {
                            links.Add("https://" + link);
                        }
                    }
                    else
                    {
                        links.Add("https://" + link);
                    }
                    if (arrow < quot)
                    {
                        tempLine = tempLine.Substring(tempLine.IndexOf("<"));
                    }
                    else
                    {
                        tempLine = tempLine.Substring(tempLine.IndexOf("\""));
                    }
                }
            }
            else if ((Regex(postedBy) == user && (Regex(line).Contains("gg") || Regex(line).Contains(Regex(" won ")) || Regex(line).Contains(Regex(" win ")) || Regex(line).Contains(Regex(" lost ")) || Regex(line).Contains(Regex(" lose ")))) || Regex(line).Contains(Regex("vs " + user)))
            {
                canTakeReplay = true;
            }
        }
    }
}