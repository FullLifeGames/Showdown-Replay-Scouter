using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using System.IO;

namespace Showdown_Replay_Scouter
{
    public partial class Form1 : Form
    {
        private Dictionary<string, Dictionary<int, List<string>>> teamToLinks = new Dictionary<string,Dictionary<int, List<string>>>();
        public Dictionary<string, List<Team>> saveRef = new Dictionary<string, List<Team>>();
        private Dictionary<string, List<string>> userToGoogleLinks = new Dictionary<string, List<string>>();
        public Thread thread = null;
        private string tempListe = "";
        private string tempTeam = "";
        private List<string> teamBox = new List<string>();

        private string[] args = null;

        public Form1()
        {
            InitializeComponent();
        }

        public Form1(string[] args)
        {
            this.args = args;
            InitializeComponent();
        }

        public void Button1_Click(object sender, EventArgs e)
        {
            if (thread == null || !thread.IsAlive)
            {
                thread = new Thread(delegate()
                {
                    teamToLinks = new Dictionary<string, Dictionary<int, List<string>>>();
                    bool bisaboard = false;
                    bool tournament = tournamentCheckBox.Checked;
                    bool showdown = showdownCheckbox.Checked;
                    bool google = googleCheckbox.Checked;
                    saveRef = new Dictionary<string, List<Team>>();
                    if (textBox1.Text.Trim() != "")
                    {
                        button1.BeginInvoke((MethodInvoker)delegate
                        {
                            button1.Enabled = false;
                        });
                        button2.BeginInvoke((MethodInvoker)delegate
                        {
                            button2.Enabled = false;
                        });
                        button3.BeginInvoke((MethodInvoker)delegate
                        {
                            button3.Enabled = false;
                        });
                        textBox2.BeginInvoke((MethodInvoker)delegate
                        {
                            textBox2.Text = "";
                        });
                        tempListe = "";
                        comboBox1.BeginInvoke((MethodInvoker)delegate
                        {
                            comboBox1.Items.Clear();
                        });
                        teamBox.Clear();
                        string[] users = textBox1.Text.Split('\n');
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
                                    noRegexUser = tmpuser[0];
                                    tier = Regex(tmpuser[1]);
                                    if (tmpuser.Length > 2)
                                    {
                                        opp = Regex(tmpuser[2]);
                                        noRegexOpp = tmpuser[2];
                                    }
                                    if (opp != null)
                                    {
                                        tmpuser[0] = tmpuser[2];
                                    }
                                    textBox2.BeginInvoke((MethodInvoker)delegate
                                    {
                                        textBox2.Text += tmpuser[0].Trim() + " (" + tmpuser[1].Trim() + "):\r\n\r\n";                                       
                                    });
                                    tempListe += tmpuser[0].Trim() + " (" + tmpuser[1].Trim() + "):\r\n\r\n";
                                }
                                else
                                {
                                    string[] tmpuser = use.Split('|');
                                    textBox2.BeginInvoke((MethodInvoker)delegate
                                    {
                                        textBox2.Text += tmpuser[0].Trim() + ":\r\n\r\n";
                                    }); 
                                    tempListe += tmpuser[0].Trim() + ":\r\n\r\n";
                                    user = Regex(tmpuser[0]);
                                    noRegexUser = tmpuser[0];
                                }

                                string tempBattle = "";

                                Bisaboard(bisaboard, client, links, user, tier, ref tempBattle, opp);

                                TournamentFunction(tournament, client, links, user, tier, tempBattle, opp);

                                tempBattle = Showdown(showdown, client, links, noRegexUser, tier, tempBattle, opp);

                                if (google)
                                {                                    
                                    List<string> htmlText = new List<string>();
                                    if (!userToGoogleLinks.ContainsKey(noRegexUser))
                                    {
                                        using (Google g = new Google(noRegexUser, htmlText))
                                        {
                                            g.ShowDialog();
                                        }
                                        userToGoogleLinks.Add(noRegexUser, htmlText);
                                    }
                                    else
                                    {
                                        htmlText = userToGoogleLinks[noRegexUser];
                                    }
                                    var doc = new HtmlAgilityPack.HtmlDocument();
                                    foreach (string html in htmlText)
                                    {
                                        doc.LoadHtml(html);
                                        foreach (HtmlNode htmlNode in doc.QuerySelectorAll(".r a"))
                                        {
                                            foreach (var attr in htmlNode.Attributes)
                                            {
                                                if (attr.Name == "href")
                                                {
                                                    if ((attr.Value.Contains("replay.pokemonshowdown.com/") || attr.Value.Contains("pokemonshowdown.com/replay")) && !attr.Value.Contains("replay.pokemonshowdown.com/search") && !attr.Value.Contains("pokemonshowdown.com/replay/&") && attr.Value != "http://pokemonshowdown.com/replay/")
                                                    {                                                        
                                                        string tempLinks = attr.Value;
                                                        if (tempLinks.Contains("&"))
                                                        {
                                                            tempLinks = tempLinks.Substring(0, tempLinks.IndexOf("&"));
                                                        }
                                                        if (tempLinks.Contains("-"))
                                                        {
                                                            tempLinks = tempLinks.Substring(tempLinks.LastIndexOf("/") + 1, tempLinks.LastIndexOf("-") - (tempLinks.LastIndexOf("/") + 1));
                                                        }
                                                        if (tempLinks.Contains("-"))
                                                        {
                                                            tempLinks = tempLinks.Substring(tempLinks.LastIndexOf("-") + 1);
                                                        }
                                                        if (tier == null || Regex(tempLinks) == tier)
                                                        {
                                                            if (!links.Contains(attr.Value))
                                                            {
                                                                bool oppCheck = false;
                                                                if (opp != null)
                                                                {
                                                                    oppCheck = true;
                                                                    int oppCount = 0;
                                                                    string battle = attr.OwnerNode.InnerText;
                                                                    string playerone = battle.Substring(battle.IndexOf(":") + 1, battle.IndexOf(" vs. ") - (battle.IndexOf(":") + 1));
                                                                    if (Regex(playerone) == user || Regex(playerone) == opp)
                                                                    {
                                                                        oppCount++;
                                                                    }
                                                                    string playertwo = battle.Substring(battle.IndexOf(" vs. ") + 5, ((battle.Contains("-")) ? battle.LastIndexOf("-") : battle.IndexOf("...")) - (battle.IndexOf(" vs. ") + 5));
                                                                    if (Regex(playertwo) == user || Regex(playertwo) == opp)
                                                                    {
                                                                        oppCount++;
                                                                    }
                                                                    if (oppCount == 2)
                                                                    {
                                                                        oppCheck = false;
                                                                    }
                                                                }
                                                                if (!oppCheck)
                                                                {
                                                                    if (Regex(attr.OwnerNode.InnerText).Contains(user))
                                                                    {
                                                                        string link = attr.Value;
                                                                        if (link.Contains("/url?q="))
                                                                        {
                                                                            link = link.Substring(7, link.IndexOf("&amp;") - 7);
                                                                        }
                                                                        links.Add(link);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

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
                                    comboBox1.BeginInvoke((MethodInvoker)delegate
                                    {
                                        comboBox1.Items.Add(String.Join(", ", t.pokemon));
                                    });
                                    teamBox.Add(String.Join(", ", t.pokemon));
                                    textBox2.BeginInvoke((MethodInvoker)delegate
                                    {
                                        textBox2.Text += String.Join(", ", t.pokemon) + ":\r\n";
                                        foreach (string link in kv.Value)
                                        {
                                            textBox2.Text += link + "\r\n";
                                        }
                                        textBox2.Text += "\r\n";
                                    });
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

                                textBox2.BeginInvoke((MethodInvoker)delegate
                                {
                                    textBox2.Text += "\r\n";
                                });
                                tempListe += "\r\n";
                            }
                            comboBox1.BeginInvoke((MethodInvoker)delegate
                            {
                                if (comboBox1.Items.Count != 0)
                                {
                                    comboBox1.SelectedIndex = 0;
                                }
                            });
                        }
                    }
                    button1.BeginInvoke((MethodInvoker)delegate
                    {
                        button1.Enabled = true;
                    });
                    button2.BeginInvoke((MethodInvoker)delegate
                    {
                        if (comboBox1.Items.Count != 0)
                        {
                            button2.Enabled = true;
                        }
                    });
                    button3.BeginInvoke((MethodInvoker)delegate
                    {
                        if (comboBox1.Items.Count != 0)
                        {
                            button3.Enabled = true;
                        }
                    });
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }
        }

        private void TournamentFunction(bool tournament, WebClient client, List<string> links, string user, string tier, string tempBattle, string opp)
        {
            if (tournament)
            {
                Tournament tour = new Tournament(tournament, client, links, tier, tempBattle, opp);
                tour.AddReplaysForUser(user);
            }
        }

        private void Bisaboard(bool bisaboard, WebClient client, List<string> links, string user, string tier, ref string tempBattle, string opp)
        {
            if (bisaboard)
            {
                bool tierFound = false;
                bool tbody = false;
                int tr = 0;
                client.Headers.Add("User-Agent: Other");
                string bisaboardReplays = client.DownloadString("http://showdown.bisaboard.de/replays/");
                foreach (string line in bisaboardReplays.Split('\n'))
                {
                    if (line.Contains("<h2>") && tierFound)
                    {
                        break;
                    }
                    else if (line.Contains("<h2>") && tier != null)
                    {
                        string tmp = line.Substring(line.IndexOf("<h2>") + 4);
                        tmp = tmp.Substring(0, tmp.IndexOf("</h2>"));
                        if (tier == Regex(tmp))
                        {
                            tierFound = true;
                        }
                    }
                    else if (tierFound || tier == null)
                    {
                        if (tbody)
                        {
                            if (line.Contains("<tr>"))
                            {
                                tr = 0;
                            }
                            else if (line.Contains("<td>") && tr == 0)
                            {
                                tempBattle = line.Substring(line.IndexOf("\"") + 1);
                                tempBattle = tempBattle.Substring(0, tempBattle.IndexOf("\""));
                                tempBattle = "http://showdown.bisaboard.de/replays/" + tempBattle;
                                tr++;
                            }
                            else if (line.Contains("<td>") && tr == 1)
                            {
                                bool oppCheck = false;
                                if (opp != null)
                                {
                                    oppCheck = true;
                                }
                                string temp = line.Substring(line.IndexOf("<strong>") + 8);
                                string playerone = temp.Substring(0, temp.IndexOf("</"));
                                if (Regex(playerone) == user || Regex(playerone) == opp)
                                {
                                    if (oppCheck) 
                                    {
                                        oppCheck = false;
                                    }
                                    else
                                    {
                                        links.Add(tempBattle);
                                    }
                                }
                                temp = temp.Substring(temp.IndexOf("<strong>") + 8);
                                string playertwo = temp.Substring(0, temp.IndexOf("</"));
                                if (Regex(playertwo) == user || Regex(playertwo) == opp)
                                {
                                    if (!oppCheck)
                                    {
                                        links.Add(tempBattle);
                                    }
                                }
                                tr++;
                            }
                        }
                        else if (line.Contains("<tbody>"))
                        {
                            tbody = true;
                        }
                    }
                }
            }
        }

        private string Showdown(bool showdown, WebClient client, List<string> links, string noRegexUser, string tier, string tempBattle, string opp)
        {
            if (showdown)
            {
                bool oppCheck = (opp != null);
                List<string> html = new List<string>();
                Showdown show_down = new Showdown(noRegexUser, html);
                show_down.ShowDialog();
                string showdownReplays = html[0];
                foreach (string line in showdownReplays.Split('\n'))
                {
                    if (line.Contains("<small>"))
                    {
                        string tmpTier = line.Substring(line.IndexOf("<small>") + 7, line.IndexOf("<br") - (line.IndexOf("<small>") + 7));
                        if (tier == null || tier == Regex(tmpTier))
                        {
                            if (oppCheck)
                            {
                                int countPlayers = 0;
                                string temp = line.Substring(line.IndexOf("<strong>") + 8);
                                string playerone = temp.Substring(0, temp.IndexOf("</"));
                                if (Regex(playerone) == Regex(noRegexUser) || Regex(playerone) == opp)
                                {
                                    countPlayers++;
                                }
                                temp = temp.Substring(temp.IndexOf("<strong>") + 8);
                                string playertwo = temp.Substring(0, temp.IndexOf("</"));
                                if (Regex(playertwo) == Regex(noRegexUser) || Regex(playertwo) == opp)
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
            return tempBattle;
        }

        private string ShowdownOld(bool showdown, WebClient client, List<string> links, string noRegexUser, string tier, string tempBattle, string opp)
        {
            if (showdown)
            {
                bool oppCheck = (opp != null);
                string showdownReplays = client.DownloadString("http://replay.pokemonshowdown.com/search/?user=" + noRegexUser);
                foreach (string line in showdownReplays.Split('\n'))
                {
                    if (line.Contains("<small>"))
                    {
                        string tmpTier = line.Substring(line.IndexOf("<small>") + 7, line.IndexOf("<br") - (line.IndexOf("<small>") + 7));
                        if (tier == null || tier == Regex(tmpTier))
                        {
                            if (oppCheck)
                            {
                                int countPlayers = 0;
                                string temp = line.Substring(line.IndexOf("<strong>") + 8);
                                string playerone = temp.Substring(0, temp.IndexOf("</"));
                                if (Regex(playerone) == Regex(noRegexUser) || Regex(playerone) == opp)
                                {
                                    countPlayers++;
                                }
                                temp = temp.Substring(temp.IndexOf("<strong>") + 8);
                                string playertwo = temp.Substring(0, temp.IndexOf("</"));
                                if (Regex(playertwo) == Regex(noRegexUser) || Regex(playertwo) == opp)
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
            return tempBattle;
        }

        private string Regex(string toFilter)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9]");
            toFilter = rgx.Replace(toFilter, "");
            return toFilter.ToLower();
        }

  /*      private void getTiers()
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("User-Agent: Other");
                string bisaboardReplays = client.DownloadString("http://showdown.bisaboard.de/replays/");
                bool getTiers = false;
                foreach (string line in bisaboardReplays.Split('\n'))
                {
                    if (line.Contains("<ul>"))
                    {
                        getTiers = true;
                    }
                    else if (line.Contains("</ul>"))
                    {
                        break;
                    }
                    else if(getTiers)
                    {
                        string temp = line.Substring(line.IndexOf("<a"));
                        temp = temp.Substring(temp.IndexOf(">") + 1);
                        temp = temp.Substring(0, temp.IndexOf("<"));
                        comboBox1.Items.Add(temp);
                    }
                }

                comboBox1.SelectedItem = "OU";
                
            }
        }
        */
        private void Form1_Load(object sender, EventArgs e)
        {
       //     getTiers();
            if (args != null)
            {
                this.Hide();
                textBox1.Text = args[0].Replace(";", "\r\n");
                Button1_Click(null, null);
                thread.Join();
                Button3_Click(null, null);
                thread.Join();
                this.Close();
            }
        }

        public void TextBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyValue == 65)
                textBox2.SelectAll();  
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (thread != null)
            {
                if (thread.IsAlive)
                {
                    thread.Abort();
                }
            }
        }

        private String GetTeam(string team, bool smogtours)
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
                if (!mons.ContainsKey(p.name))
                {
                    mons.Add(p.name, p);
                }
                pokes.Add(p);
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
                                            Console.WriteLine("Debug");
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
                                    if (!mons.ContainsKey(mon))
                                    {
                                        foreach (KeyValuePair<string, Pokemon> kv in mons)
                                        {
                                            if (Regex(mon).Contains(Regex(kv.Key)))
                                            {
                                                mons.Add(mon, kv.Value);
                                                kv.Value.name = mon;
                                                break;
                                            }
                                        }
                                    }
                                    if (line.Contains("[from]Magic Bounce"))
                                    {
                                        AbilityUpdate(pokes, mons, "Magic Bounce", mon);
                                    }
                                    else
                                    {
                                        if (!mons[mon].moves.Contains(move))
                                        {
                                            mons[mon].moves.Add(move);
                                            textBox3.BeginInvoke((MethodInvoker)delegate
                                            {
                                                textBox3.Text = Pokemon.PrintPokemon(pokes);
                                            });
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
                                if (!mons.ContainsKey(mon))
                                {
                                    foreach (KeyValuePair<string, Pokemon> kv in mons)
                                    {
                                        if (Regex(mon).Contains(Regex(kv.Key)))
                                        {
                                            mons.Add(mon, kv.Value);
                                            kv.Value.name = mon;
                                            break;
                                        }
                                    }
                                }
                                if (mons[mon].name != newmon)
                                {
                                    mons[mon].name = newmon;
                                    textBox3.BeginInvoke((MethodInvoker)delegate
                                    {
                                        textBox3.Text = Pokemon.PrintPokemon(pokes);
                                    });
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
                                    if (!mons.ContainsKey(mon))
                                    {
                                        foreach (KeyValuePair<string, Pokemon> kv in mons)
                                        {
                                            if (Regex(mon).Contains(Regex(kv.Key)))
                                            {
                                                mons.Add(mon, kv.Value);
                                                kv.Value.name = mon;
                                                break;
                                            }
                                        }
                                    }
                                    ItemUpdate(pokes, mons, mon, item);
                                }
                            }
                        }
                        else if (line.Contains("-damage") && line.Contains("Rocky Helmet"))
                        {
                            string[] iteminf = line.Split('|');
                            if (iteminf[5].Contains(playerValue))
                            {
                                string mon = iteminf[5].Split(':')[1].Trim();
                                string item = iteminf[4].Split(':')[1].Trim();
                                if (!mons.ContainsKey(mon))
                                {
                                    foreach (KeyValuePair<string, Pokemon> kv in mons)
                                    {
                                        if (Regex(mon).Contains(Regex(kv.Key)))
                                        {
                                            mons.Add(mon, kv.Value);
                                            kv.Value.name = mon;
                                            break;
                                        }
                                    }
                                }
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
                                    if (!mons.ContainsKey(mon))
                                    {
                                        foreach (KeyValuePair<string, Pokemon> kv in mons)
                                        {
                                            if (Regex(mon).Contains(Regex(kv.Key)))
                                            {
                                                mons.Add(mon, kv.Value);
                                                kv.Value.name = mon;
                                                break;
                                            }
                                        }
                                    }
                                    ItemUpdate(pokes, mons, mon, item);
                                }
                            }
                        }
                        else if (line.Contains("ability") && line.Contains("-damage"))
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
                                    if (!mons.ContainsKey(mon))
                                    {
                                        foreach (KeyValuePair<string, Pokemon> kv in mons)
                                        {
                                            if (Regex(mon).Contains(Regex(kv.Key)))
                                            {
                                                mons.Add(mon, kv.Value);
                                                kv.Value.name = mon;
                                                break;
                                            }
                                        }
                                    }
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
                                    else if(abinf.Contains("[of]") && ContainsLineAnOfAbility(line))
                                    {
                                        mon = abinf.Split(':')[1].Trim();
                                    }
                                }
                            }
                            if (ability != "" && mon != "")
                            {
                                if (!mons.ContainsKey(mon))
                                {
                                    foreach (KeyValuePair<string, Pokemon> kv in mons)
                                    {
                                        if (Regex(mon).Contains(Regex(kv.Key)))
                                        {
                                            mons.Add(mon, kv.Value);
                                            kv.Value.name = mon;
                                            break;
                                        }
                                    }
                                }
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

                                if (!mons.ContainsKey(mon))
                                {
                                    foreach (KeyValuePair<string, Pokemon> kv in mons)
                                    {
                                        if (Regex(mon).Contains(Regex(kv.Key)))
                                        {
                                            mons.Add(mon, kv.Value);
                                            kv.Value.name = mon;
                                            break;
                                        }
                                    }
                                }
                                AbilityUpdate(pokes, mons, ability, mon);
                            }
                        }
                    }
                }
            }

            return Pokemon.PrintPokemon(pokes);
        }

        private void DeterminePlayer(string user, ref string playerValue, ref string playerName, string line)
        {
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
                else if (distance < 10)
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

        private string[] ofAbilites = new string[] {
            "Frisk", "Poison Touch",
            "Electric Surge", "Psychic Surge", "Grassy Surge", "Misty Surge",
            "Drought", "Sand Stream", "Drizzle", "Snow Warning"
        };

        private bool ContainsLineAnOfAbility(string line)
        {
            foreach(string ofAbility in ofAbilites)
            {
                if(line.Contains("ability: " + ofAbility))
                {
                    return true;
                }
            }
            return false;
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (thread == null || !thread.IsAlive)
            {
                bool smogtours = checkBox4.Checked;
                string team = (tempListeIndex != -1) ? teamBox[tempListeIndex] + "" : (comboBox1.Text + "");
                Action exec = delegate()
                {
                    button1.BeginInvoke((MethodInvoker)delegate
                    {
                        button1.Enabled = false;
                    });
                    button2.BeginInvoke((MethodInvoker)delegate
                    {
                        button2.Enabled = false;
                    });

                    button3.BeginInvoke((MethodInvoker)delegate
                    {
                        button3.Enabled = false;
                    });
                    if (team != "")
                    {
                        string returnVal = GetTeam(team, smogtours);
                        if (sender == null)
                        {
                            tempTeam = returnVal;
                        }
                        textBox3.BeginInvoke((MethodInvoker)delegate
                        {
                            textBox3.Text = returnVal;
                        });

                    }
                    button1.BeginInvoke((MethodInvoker)delegate
                    {
                        button1.Enabled = true;
                    });
                    button2.BeginInvoke((MethodInvoker)delegate
                    {
                        button2.Enabled = true;
                    });
                    button3.BeginInvoke((MethodInvoker)delegate
                    {
                        button3.Enabled = true;
                    });
                };
                if (sender != null)
                {
                    thread = new Thread(() => exec());
                    thread.Start();
                }
                else
                {
                    exec();
                }
            }
        }

        private void AbilityUpdate(List<Pokemon> pokes, Dictionary<string, Pokemon> mons, string ability, string mon)
        {            
            if (mons[mon].ability != null)
            {
                if (!mons[mon].ability.Contains(ability))
                {
                    mons[mon].ability += " | " + ability;
                    textBox3.BeginInvoke((MethodInvoker)delegate
                    {
                        textBox3.Text = Pokemon.PrintPokemon(pokes);
                    });
                }
            }
            else
            {
                mons[mon].ability = ability;
                textBox3.BeginInvoke((MethodInvoker)delegate
                {
                    textBox3.Text = Pokemon.PrintPokemon(pokes);
                });
            }
        }

        private void ItemUpdate(List<Pokemon> pokes, Dictionary<string, Pokemon> mons, string mon, string item)
        {
            if (mons[mon].item == null || mons[mon].item == "")
            {
                mons[mon].item = item;
                textBox3.BeginInvoke((MethodInvoker)delegate
                {
                    textBox3.Text = Pokemon.PrintPokemon(pokes);
                });
            }
            else
            {
                if (!mons[mon].item.Contains(item))
                {
                    mons[mon].item += " | " + item;
                    textBox3.BeginInvoke((MethodInvoker)delegate
                    {
                        textBox3.Text = Pokemon.PrintPokemon(pokes);
                    });
                }
            }
        }

        private void TextBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyValue == 65)
                textBox3.SelectAll();  
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyValue == 65)
                textBox1.SelectAll();  
        }

        private int tempListeIndex = -1;

        private void Button3_Click(object sender, EventArgs e)
        {
            if (thread == null || !thread.IsAlive)
            {
                string output = "";
                OpenFileDialog ofd = null;
                DialogResult result = DialogResult.No;
                if (args == null)
                {
                    ofd = new OpenFileDialog();
                    ofd.CheckFileExists = false;
                    result = ofd.ShowDialog(); // Show the dialog.
                }


                if (result == DialogResult.OK || args != null) // Test result.
                {
                    Action exec = delegate ()
                    {
                        button1.BeginInvoke((MethodInvoker)delegate
                        {
                            button1.Enabled = false;
                        });
                        button2.BeginInvoke((MethodInvoker)delegate
                        {
                            button2.Enabled = false;
                        });

                        button3.BeginInvoke((MethodInvoker)delegate
                        {
                            button3.Enabled = false;
                        });
                        bool smogtours = false;
                        checkBox4.BeginInvoke((MethodInvoker)delegate
                        {
                            smogtours = checkBox4.Checked;
                        });
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
                        FileInfo f = new FileInfo((args != null) ? args[1] : ofd.FileName);
                        if (f.Exists)
                        {
                            f.Delete();
                        }
                        File.WriteAllText((args != null) ? args[1] : ofd.FileName, output);
                        MessageBox.Show("Replays were dumped!");

                        button1.BeginInvoke((MethodInvoker)delegate
                        {
                            button1.Enabled = true;
                        });
                        button2.BeginInvoke((MethodInvoker)delegate
                        {
                            button2.Enabled = true;
                        });
                        button3.BeginInvoke((MethodInvoker)delegate
                        {
                            button3.Enabled = true;
                        });
                    };
                    thread = new Thread(() => exec());
                    thread.Start();
                }
                
                tempListeIndex = -1;
            }
        }
    }
}
