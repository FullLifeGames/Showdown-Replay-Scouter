using Showdown_Replay_Scouter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Commandline_Showdown_Replay_Scouter
{
    class Program
    {
        private static Dictionary<string, List<Team>> saveRef;
        private static Dictionary<string, Dictionary<int, List<string>>> teamToLinks;

        static void Main(string[] args)
        {
            if (args.Length <= 1)
            {
                Console.WriteLine("Too few arguments provided! Execution: {file}.exe {user} {path}");
            }
            else
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;

                teamToLinks = new Dictionary<string, Dictionary<int, List<string>>>();
                bool showdown = true;
                saveRef = new Dictionary<string, List<Team>>();
                string tempListe = "";
                List<string> teamBox = new List<string>();
                string[] users = new string[] { args[0] };
                using (WebClient client = new WebClient())
                {
                    foreach (string use in users)
                    {
                        List<string> links = new List<string>();
                        string user = "";
                        string noRegexUser = "";
                        string tier = null;
                        string opp = null;
                        string noRegexOpp = null;
                        if (use.Contains('|'))
                        {
                            string[] tmpuser = use.Split('|');
                            user = Regex(tmpuser[0]);
                            if(user == "")
                            {
                                //Console.WriteLine("Empty user provided! Execution: {file}.exe {user} {path}");
                                return;
                            }
                            noRegexUser = tmpuser[0];
                            tier = Regex(tmpuser[1]);
                            if(tier == "")
                            {
                                //Console.WriteLine("Empty tier provided! Execution: {file}.exe {user} {path}");
                                return;
                            }
                            if (tmpuser.Length > 2)
                            {
                                opp = Regex(tmpuser[2]);
                                noRegexOpp = tmpuser[2];
                            }
                            if (opp != null)
                            {
                                tmpuser[0] = tmpuser[2];
                            }
                            tempListe += tmpuser[0].Trim() + " (" + tmpuser[1].Trim() + "):\r\n\r\n";
                        }
                        else
                        {
                            string[] tmpuser = use.Split('|');
                            tempListe += tmpuser[0].Trim() + ":\r\n\r\n";
                            user = Regex(tmpuser[0]);
                            noRegexUser = tmpuser[0];
                        }

                        string tempBattle = "";

                        tempBattle = Showdown(showdown, client, links, noRegexUser, tier, tempBattle, opp);

                        Dictionary<int, List<string>> teams = new Dictionary<int, List<string>>();
                        List<Team> referenz = new List<Team>();
                        int maxId = 0;
                        if (opp != null)
                        {
                            user = opp;
                            noRegexUser = noRegexOpp;
                        }

                        links = links.Distinct().ToList();

                        foreach (string link in links)
                        {
                            client.Headers.Add("User-Agent: Other");
                            bool timeout = true;
                            string replay = "";
                            int tries = 0;
                            while (timeout && tries < 10)
                            {
                                try
                                {
                                    replay = client.DownloadString(link);
                                    timeout = false;
                                }
                                catch (Exception)
                                {
                                    timeout = true;
                                    tries++;
                                }
                            }
                            string realReplay = replay;
                            string playerValue = "";
                            if (link.Contains("showdown.bisaboard"))
                            {
                                foreach (string line in replay.Split('\n'))
                                {
                                    if (line.Contains("battle.setQueue("))
                                    {
                                        realReplay = string.Join("\n", System.Text.RegularExpressions.Regex.Split(line, "\",\""));
                                        break;
                                    }
                                }
                            }

                            Team team = new Team();
                            int monCount = 0;
                            string playerName = "";
                            foreach (string line in realReplay.Split('\n'))
                            {
                                if (line.Contains("|player"))
                                {
                                    DeterminePlayer(user, ref playerValue, ref playerName, line);
                                }
                                else if (playerValue == "")
                                {
                                    continue;
                                }
                                else if (line.Contains("|poke|"))
                                {
                                    string[] pokeinf = line.Split('|');
                                    if (pokeinf[2] == playerValue)
                                    {
                                        if (monCount == 6)
                                        {
                                            team.pokemon[0] = "undefined";
                                            team.pokemon[1] = "undefined";
                                            team.pokemon[2] = "undefined";
                                            team.pokemon[3] = "undefined";
                                            team.pokemon[4] = "undefined";
                                            team.pokemon[5] = "undefined";
                                            break;
                                        }
                                        team.pokemon[monCount] = pokeinf[3].Split(',')[0];
                                        monCount++;
                                    }
                                }
                                else if ((line.Contains("|switch") || line.Contains("|drag")) && monCount != 6)
                                {
                                    if (line.Contains(playerValue))
                                    {
                                        string[] pokeinf = line.Split('|');
                                        string maybepoke = pokeinf[3].Split(',')[0];
                                        if (!team.pokemon.Contains(maybepoke) && !team.pokemon.Contains("(Lead) " + maybepoke))
                                        {
                                            if (monCount == 0)
                                            {
                                                team.pokemon[monCount] = "(Lead) " + maybepoke;
                                            }
                                            else
                                            {
                                                if (monCount == 6)
                                                {
                                                    team.pokemon[0] = "undefined";
                                                    team.pokemon[1] = "undefined";
                                                    team.pokemon[2] = "undefined";
                                                    team.pokemon[3] = "undefined";
                                                    team.pokemon[4] = "undefined";
                                                    team.pokemon[5] = "undefined";
                                                    break;
                                                }
                                                team.pokemon[monCount] = maybepoke;
                                            }
                                            monCount++;
                                        }
                                    }
                                }
                                else if (monCount == 6)
                                {
                                    break;
                                }
                            }
                            if (team.pokemon[0] == null)
                            {
                                team.pokemon[0] = "undefined";
                                team.pokemon[1] = "undefined";
                                team.pokemon[2] = "undefined";
                                team.pokemon[3] = "undefined";
                                team.pokemon[4] = "undefined";
                                team.pokemon[5] = "undefined";
                            }
                            int id = team.Compare(referenz);
                            if (id == 0)
                            {
                                maxId++;
                                team.id = maxId;
                                List<string> linkTeams = new List<string>();
                                linkTeams.Add(link);
                                teams.Add(team.id, linkTeams);
                                referenz.Add(team);
                            }
                            else
                            {
                                teams[id].Add(link);
                            }

                        }

                        foreach (KeyValuePair<int, List<string>> kv in teams)
                        {
                            Team t = referenz[kv.Key - 1];
                            teamBox.Add(String.Join(", ", t.pokemon));
                            tempListe += String.Join(", ", t.pokemon) + ":\r\n";
                            foreach (string link in kv.Value)
                            {
                                tempListe += link + "\r\n";
                            }
                            tempListe += "\r\n";
                        }

                        if (teamToLinks.ContainsKey(user + " (" + tier + ")"))
                        {
                            int count = teamToLinks[user + " (" + tier + ")"].Count;
                            foreach (KeyValuePair<int, List<string>> kv in teams)
                            {
                                teamToLinks[user + " (" + tier + ")"].Add(kv.Key + count, kv.Value);
                            }
                        }
                        else
                        {
                            teamToLinks.Add(user + " (" + tier + ")", teams);
                        }
                        if (saveRef.ContainsKey(user + " (" + tier + ")"))
                        {
                            int count = saveRef[user + " (" + tier + ")"].Count;
                            foreach (Team t in referenz)
                            {
                                t.id += count;
                                saveRef[user + " (" + tier + ")"].Add(t);
                            }
                        }
                        else
                        {
                            saveRef[user + " (" + tier + ")"] = referenz;
                        }
                        tempListe += "\r\n";
                    }
                }



                bool smogtours = false;

                List<string> userOutput = new List<string>();
                Dictionary<string, string> allUser = new Dictionary<string, string>();
                string dump = tempListe;
                foreach (string dumpLine in dump.Split('\n'))
                {
                    if (dumpLine.Trim().EndsWith("):") || (!dumpLine.Trim().Contains(",") && dumpLine.Trim().EndsWith(":")))
                    {
                        string userTemp = dumpLine.Trim().Substring(0, dumpLine.Trim().IndexOf(":")).Trim();
                        if (userTemp.Contains("("))
                        {
                            string tierTemp = Regex(userTemp.Substring(userTemp.LastIndexOf("(") - 1));
                            userTemp = Regex(userTemp.Substring(0, userTemp.LastIndexOf("(")));
                            allUser.Add(userTemp + " (" + tierTemp + ")", dumpLine.Trim());
                        }
                        else
                        {
                            userTemp = Regex(userTemp);
                            allUser.Add(userTemp, dumpLine.Trim());
                        }
                    }
                }
                int tempListeIndex;
                string output = "";
                string tempTeam = "";
                for (int i = 0; i < teamBox.Count; i++)
                {
                    tempListeIndex = i;
                    Team t = new Team();
                    t.pokemon = teamBox[i].Split(',');
                    string user = t.Compare(saveRef);
                    t.id = t.Compare(saveRef[user]);
                    List<string> links = teamToLinks[user][t.id];
                    int count = 0;
                    foreach (string link in links)
                    {
                        if (smogtours && !link.Contains("smogtours"))
                        {
                            continue;
                        }
                        else
                        {
                            count++;
                        }
                    }

                    if (count > 0)
                    {
                        string team = teamBox[tempListeIndex];
                        if (team != "")
                        {
                            tempTeam = GetTeam(team, smogtours);
                        }
                        if (!userOutput.Contains(user))
                        {
                            output += allUser[(user.Contains("()") ? user.Substring(0, user.IndexOf("(")).Trim() : user)] + "\r\n" + "\r\n";
                            userOutput.Add(user);
                        }
                        output += teamBox[i] + ":\r\n";
                        foreach (string link in links)
                        {
                            if (smogtours && !link.Contains("smogtours"))
                            {
                                continue;
                            }
                            output += link + "\r\n";
                        }
                        output += "\r\n" + tempTeam + "\r\n";
                    }
                }

                // Don't write empty files
                if (output != "")
                {
                    FileInfo f = new FileInfo(args[1]);
                    if (f.Exists)
                    {
                        f.Delete();
                    }
                    File.WriteAllText(args[1], output);
                }

            }
        }

        private static string Showdown(bool showdown, WebClient client, List<string> links, string noRegexUser, string tier, string tempBattle, string opp)
        {
            if (showdown)
            {
                bool oppCheck = (opp != null);
                List<string> html = new List<string>();
                HeadlessShowdown headlessShowdown = new HeadlessShowdown(noRegexUser, html);
                headlessShowdown.GetReplaysForUser();
                headlessShowdown.ParseShowdownReplays(html, tier, oppCheck, noRegexUser, opp, links);
            }
            return tempBattle;
        }

        private static Regex rgx = new Regex("[^a-zA-Z0-9]");
        public static string Regex(string toFilter)
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

        private static int LevenshtenDistanceAcceptable = 3;
        private static void DeterminePlayer(string rawUser, ref string playerValue, ref string playerName, string line)
        {
            string user = rawUser.Split(' ')[0];
            string[] playerinf = line.Split('|');
            if (playerinf.Length > 3)
            {
                int distance = LevenshteinDistance.Compute(Regex(playerinf[3]).Replace(" ", ""), user.Replace(" ", ""));
                if (Regex(playerinf[3]).Replace(" ", "").Contains(user.Replace(" ", "")))
                {
                    if (playerName != "")
                    {
                        if (playerName.Contains(user.Replace(" ", "")))
                        {
                            if (LevenshteinDistance.Compute(playerName, user.Replace(" ", "")) > distance)
                            {
                                playerValue = playerinf[2];
                                playerName = Regex(playerinf[3]).Replace(" ", "");
                            }
                        }
                        else
                        {
                            playerValue = playerinf[2];
                            playerName = Regex(playerinf[3]).Replace(" ", "");
                        }
                    }
                    else
                    {
                        playerValue = playerinf[2];
                        playerName = Regex(playerinf[3]).Replace(" ", "");
                    }
                }
                else if (distance <= LevenshtenDistanceAcceptable)
                {
                    if (playerName != "")
                    {
                        if (LevenshteinDistance.Compute(playerName, user.Replace(" ", "")) > distance && !playerName.Contains(user.Replace(" ", "")))
                        {
                            playerValue = playerinf[2];
                            playerName = Regex(playerinf[3]).Replace(" ", "");
                        }
                    }
                    else
                    {
                        playerValue = playerinf[2];
                        playerName = Regex(playerinf[3]).Replace(" ", "");
                    }
                }
            }
        }

        private static string[] ofAbilites = new string[] {
            "Frisk", "Poison Touch",
            "Electric Surge", "Psychic Surge", "Grassy Surge", "Misty Surge",
            "Drought", "Sand Stream", "Drizzle", "Snow Warning"
        };

        private static bool ContainsLineAnOfAbility(string line)
        {
            foreach (string ofAbility in ofAbilites)
            {
                if (line.Contains("ability: " + ofAbility))
                {
                    return true;
                }
            }
            return false;
        }



        private static String GetTeam(string team, bool smogtours)
        {
            Team t = new Team();
            t.pokemon = team.Split(',');
            string user = t.Compare(saveRef);
            t.id = t.Compare(saveRef[user]);
            List<string> links = teamToLinks[user][t.id];
            List<Pokemon> pokes = new List<Pokemon>();
            Dictionary<string, Pokemon> mons = new Dictionary<string, Pokemon>();
            foreach (string poke in t.pokemon)
            {
                Pokemon p = new Pokemon();
                p.name = poke.Replace("(Lead) ", "");

                string toSetName = p.name;
                if (!mons.ContainsKey(p.name))
                {
                    if (p.name.Contains("Arceus"))
                    {
                        toSetName = "Arceus-*";
                    }
                    else if (p.name.Contains("Silvally"))
                    {
                        toSetName = "Silvally-*";
                    }
                    else if (p.name.Contains("Genesect"))
                    {
                        toSetName = "Genesect-*";
                    }
                    else if (p.name.Contains("Gourgeist"))
                    {
                        toSetName = "Gourgeist-*";
                    }
                    else if (p.name.Contains("Pumpkaboo"))
                    {
                        toSetName = "Pumpkaboo-*";
                    }

                    if (!mons.ContainsKey(toSetName))
                    {
                        mons.Add(toSetName, p);
                    }
                }
                pokes.Add(p);
            }

            if (pokes.Count == 1 && pokes[0].name == "undefined")
            {
                return "";
            }

            foreach (string link in links)
            {
                if (smogtours && !link.Contains("smogtours"))
                {
                    continue;
                }
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent: Other");
                    string replay = client.DownloadString(link);
                    string realReplay = replay;
                    string playerValue = "";
                    if (link.Contains("showdown.bisaboard"))
                    {
                        foreach (string line in replay.Split('\n'))
                        {
                            if (line.Contains("battle.setQueue("))
                            {
                                realReplay = string.Join("\n", System.Text.RegularExpressions.Regex.Split(line, "\",\""));
                                break;
                            }
                        }
                    }

                    string playerName = "";
                    foreach (string line in realReplay.Split('\n'))
                    {
                        if (line.Contains("</script>"))
                        {
                            continue;
                        }
                        else if (line.Contains("|player"))
                        {
                            DeterminePlayer(user, ref playerValue, ref playerName, line);
                        }
                        else if (playerValue == "")
                        {
                            continue;
                        }
                        else if (line.Contains("|switch") || line.Contains("|drag"))
                        {
                            string[] switchinf = line.Split('|');
                            if (switchinf[2].Contains(":"))
                            {
                                string playerString = switchinf[2].Substring(0, switchinf[2].IndexOf(":"));
                                if (playerString.Contains(playerValue))
                                {
                                    string mon = switchinf[2].Split(':')[1].Trim();
                                    if (!mons.ContainsKey(mon))
                                    {
                                        string realmon = switchinf[3].Split(',')[0];
                                        if (realmon.Contains("Arceus") && (!mons.ContainsKey(realmon) || mons[realmon].name == "Arceus-*"))
                                        {
                                            mons["Arceus-*"].name = realmon;
                                            realmon = "Arceus-*";
                                        }
                                        else if (realmon.Contains("Silvally") && (!mons.ContainsKey(realmon) || mons[realmon].name == "Silvally-*"))
                                        {
                                            mons["Silvally-*"].name = realmon;
                                            realmon = "Silvally-*";
                                        }
                                        else if (realmon.Contains("Genesect"))
                                        {
                                            realmon = "Genesect-*";
                                            mons[realmon].name = mon;
                                        }
                                        else if (realmon.Contains("Gourgeist"))
                                        {
                                            realmon = "Gourgeist-*";
                                            mons[realmon].name = mon;
                                        }
                                        else if (realmon.Contains("Pumpkaboo"))
                                        {
                                            realmon = "Pumpkaboo-*";
                                            mons[realmon].name = mon;
                                        }
                                        if (!mons.ContainsKey(realmon))
                                        {
                                            Pokemon p = new Pokemon();
                                            p.name = realmon;
                                            if (!mons.ContainsKey(mon))
                                            {
                                                mons.Add(mon, p);
                                            }
                                            pokes.Add(p);
                                        }
                                        else
                                        {
                                            mons.Add(mon, mons[realmon]);
                                        }
                                    }
                                }
                            }
                        }
                        else if (line.Contains("|move|") && !line.Contains("|-sidestart"))
                        {
                            string[] moveinf = line.Split('|');
                            if (moveinf[2].Contains(":"))
                            {
                                string playerString = moveinf[2].Substring(0, moveinf[2].IndexOf(":"));
                                if (playerString.Contains(playerValue))
                                {
                                    string mon = moveinf[2].Split(':')[1].Trim();
                                    string move = moveinf[3];
                                    AddMonIfNotExists(mons, mon);
                                    if (line.Contains("[from]Magic Bounce"))
                                    {
                                        AbilityUpdate(pokes, mons, "Magic Bounce", mon);
                                    }
                                    else
                                    {
                                        if (!mons[mon].moves.Contains(move))
                                        {
                                            mons[mon].moves.Add(move);
                                        }
                                    }
                                }
                            }
                        }
                        else if (line.Contains("|detailschange"))
                        {
                            string[] detailinf = line.Split('|');
                            if (detailinf[2].Contains(playerValue))
                            {
                                string mon = detailinf[2].Split(':')[1].Trim();
                                string newmon = detailinf[3].Split(',')[0];
                                AddMonIfNotExists(mons, mon);
                                if (mons[mon].name != newmon)
                                {
                                    mons[mon].name = newmon;
                                }
                            }
                        }
                        else if (line.Contains("[from] item:") && !(line.Contains("-damage") && line.Contains("Rocky Helmet")))
                        {
                            string[] iteminf = line.Split('|');
                            if (iteminf[2].Contains(":"))
                            {
                                string playerString = iteminf[2].Substring(0, iteminf[2].IndexOf(":"));

                                if (playerString.Contains(playerValue))
                                {
                                    string mon = iteminf[2].Split(':')[1].Trim();
                                    string item = "";
                                    if (iteminf[4].Contains("item"))
                                    {
                                        item = iteminf[4].Split(':')[1].Trim();
                                    }
                                    else
                                    {
                                        item = iteminf[5].Split(':')[1].Trim();
                                    }
                                    AddMonIfNotExists(mons, mon);
                                    ItemUpdate(pokes, mons, mon, item);
                                }
                            }
                        }
                        else if (line.Contains("-damage") && line.Contains("Rocky Helmet"))
                        {
                            string[] iteminf = line.Split('|');
                            if (iteminf.Length > 5 && iteminf[5].Contains(playerValue))
                            {
                                string mon = iteminf[5].Split(':')[1].Trim();
                                string item = iteminf[4].Split(':')[1].Trim();
                                AddMonIfNotExists(mons, mon);
                                ItemUpdate(pokes, mons, mon, item);
                            }
                        }
                        else if (line.Contains("-enditem"))
                        {
                            string[] iteminf = line.Split('|');
                            if (iteminf[2].Contains(":"))
                            {
                                string playerString = iteminf[2].Substring(0, iteminf[2].IndexOf(":"));

                                if (playerString.Contains(playerValue))
                                {
                                    string mon = iteminf[2].Split(':')[1].Trim();
                                    string item = iteminf[3].Trim();
                                    AddMonIfNotExists(mons, mon);
                                    ItemUpdate(pokes, mons, mon, item);
                                }
                            }
                        }
                        else if ((line.Contains("-ability|") || line.Contains(" ability:")) && line.Contains("-damage"))
                        {
                            string[] abilityinf = line.Split('|');
                            string ability = "";
                            string mon = "";
                            ability = abilityinf[4].Split(':')[1].Trim();
                            if (abilityinf.Length > 5)
                            {
                                mon = abilityinf[5].Split(':')[1].Trim();
                                if (abilityinf[5].Split(':')[1].Contains(playerValue))
                                {
                                    AddMonIfNotExists(mons, mon);
                                    AbilityUpdate(pokes, mons, ability, mon);
                                }
                            }
                        }
                        else if (line.Contains("ability:") && !line.Contains("ability: Imposter"))
                        {
                            string[] abilityinf = line.Split('|');
                            string ability = "";
                            string mon = "";
                            foreach (string abinf in abilityinf)
                            {
                                if (abinf.Contains("ability:"))
                                {
                                    ability = abinf.Split(':')[1].Trim();
                                }
                                else if (abinf.Contains(":") && abinf.Split(':')[0].Contains(playerValue))
                                {
                                    if (!abinf.Contains("[of]") && !ContainsLineAnOfAbility(line))
                                    {
                                        mon = abinf.Split(':')[1].Trim();
                                    }
                                    else if (abinf.Contains("[of]") && ContainsLineAnOfAbility(line))
                                    {
                                        mon = abinf.Split(':')[1].Trim();
                                    }
                                }
                            }
                            if (ability != "" && mon != "")
                            {
                                AddMonIfNotExists(mons, mon);
                                AbilityUpdate(pokes, mons, ability, mon);
                            }
                        }
                        else if (line.Contains("-ability"))
                        {
                            string[] abilityinf = line.Split('|');
                            if (abilityinf[2].Contains(":") && abilityinf[2].Split(':')[0].Contains(playerValue))
                            {
                                string ability = abilityinf[3].Trim();
                                string mon = abilityinf[2].Split(':')[1].Trim();

                                AddMonIfNotExists(mons, mon);
                                AbilityUpdate(pokes, mons, ability, mon);
                            }
                        }
                    }
                }
            }

            return Pokemon.PrintPokemon(pokes);
        }

        private static void AddMonIfNotExists(Dictionary<string, Pokemon> mons, string mon)
        {
            if (!mons.ContainsKey(mon))
            {
                bool found = false;
                foreach (KeyValuePair<string, Pokemon> kv in mons)
                {
                    if (Regex(mon).Contains(Regex(kv.Key)) || Regex(mon + "-Mega").Contains(Regex(kv.Key)) || Regex(mon + "-Origin").Contains(Regex(kv.Key)))
                    {
                        mons.Add(mon, kv.Value);
                        kv.Value.name = mon;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Pokemon p = new Pokemon();
                    mons.Add(mon, p);
                    p.name = mon;
                }
            }
        }

        private static void AbilityUpdate(List<Pokemon> pokes, Dictionary<string, Pokemon> mons, string ability, string mon)
        {
            if (mons[mon].ability != null)
            {
                if (!mons[mon].ability.Contains(ability))
                {
                    mons[mon].ability += " | " + ability;
                }
            }
            else
            {
                mons[mon].ability = ability;
            }
        }

        private static void ItemUpdate(List<Pokemon> pokes, Dictionary<string, Pokemon> mons, string mon, string item)
        {
            if (mons[mon].item == null || mons[mon].item == "")
            {
                mons[mon].item = item;
            }
            else
            {
                if (!mons[mon].item.Contains(item))
                {
                    mons[mon].item += " | " + item;
                }
            }
        }


    }
}
