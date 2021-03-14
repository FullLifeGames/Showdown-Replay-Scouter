using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Showdown_Replay_Scouter
{
    public partial class Showdown : Form
    {

        private string user;
        private List<string> htmlText;

        public Showdown(string user, List<string> htmlText)
        {
            this.user = user;
            this.htmlText = htmlText;
            InitializeComponent();
        }

        public static void ParseShowdownReplays(List<string> html, string tier, bool oppCheck, string noRegexUser, string opp, List<string> links)
        {
            string showdownReplays = html[0];
            string tempBattle = "";
            foreach (string line in showdownReplays.Split('\n'))
            {
                if (line.Contains("<small>"))
                {
                    string tmpTier = line.Substring(line.IndexOf("<small>") + 7, line.IndexOf("<br") - (line.IndexOf("<small>") + 7));
                    if (tier == null || tier == Form1.Regex(tmpTier))
                    {
                        if (oppCheck)
                        {
                            int countPlayers = 0;
                            string temp = line.Substring(line.IndexOf("<strong>") + 8);
                            string playerone = temp.Substring(0, temp.IndexOf("</"));
                            if (Form1.Regex(playerone) == Form1.Regex(noRegexUser) || Form1.Regex(playerone) == opp)
                            {
                                countPlayers++;
                            }
                            temp = temp.Substring(temp.IndexOf("<strong>") + 8);
                            string playertwo = temp.Substring(0, temp.IndexOf("</"));
                            if (Form1.Regex(playertwo) == Form1.Regex(noRegexUser) || Form1.Regex(playertwo) == opp)
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

        private void Showdown_Load(object sender, EventArgs e)
        {
            webBrowser1.Url = new System.Uri("http://replay.pokemonshowdown.com/search/?user=" + user);
        }

        private void Showdown_FormClosing(object sender, FormClosingEventArgs e)
        {
            htmlText.Add(webBrowser1.Document.Body.InnerHtml);
            if(thread != null)
            {
                if(thread.IsAlive)
                {
                    running = false;
                }
            }
        }

        Thread thread;
        volatile bool running = true;

        private void WebBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (thread == null)
            {
                thread = new Thread(delegate()
                {
                    Thread.Sleep(1000);
                    for (int i = 0; i < 1000 && running; i++)
                    {
                        webBrowser1.BeginInvoke((MethodInvoker)delegate
                        {
                            HtmlElementCollection classButton = webBrowser1.Document.All;
                            bool activated = false;
                            foreach (HtmlElement element in classButton)
                            {
                                if (element.GetAttribute("name") == "moreResults")
                                {
                                    element.InvokeMember("click");
                                    activated = true;
                                }
                            }
                            if (!activated)
                            {
                                this.BeginInvoke((MethodInvoker)delegate
                                {
                                    this.Close();
                                });
                            }
                        });
                        Thread.Sleep(600);
                    }
                });
                thread.Start();
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            thread.Join();
            this.Close();
        }

    }
}
