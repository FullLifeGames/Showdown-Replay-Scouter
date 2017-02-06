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

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
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

        private void button1_Click(object sender, EventArgs e)
        {
            thread.Join();
            this.Close();
        }

    }
}
