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
    public partial class Google : Form
    {

        private string user;
        private List<string> htmlText;
        private volatile int start = 0;

        public Google(string user, List<string> htmlText)
        {
            this.user = user;
            this.htmlText = htmlText;
            InitializeComponent();
        }

        private void Google_Load(object sender, EventArgs e)
        {
            webBrowser1.Url = new System.Uri("https://www.google.com/search?q=intitle:%22Pok%C3%A9mon%20Showdown%22%20" + user + "&safe=off&filter=0&nfpr=1&num=1000&start=" + start);
        }

        private void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {    
            if(webBrowser1.DocumentText != "")
            {
                if (webBrowser1.DocumentText.Split('\n').Length < 30)
                {

                }
                else if (!htmlText.Contains(webBrowser1.DocumentText))
                {
                    htmlText.Add(webBrowser1.DocumentText);
                }
                else
                {
                    this.Close();
                }
            }
        }

        Thread thread = null;
        private volatile bool running = true;

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (thread == null)
            {
                if (webBrowser1.DocumentText.Split('\n').Length >= 30)
                {
                    thread = new Thread(delegate()
                    {
                        Thread.Sleep(400);
                        try
                        {
                            for (int i = 0; i < 1000 && running; i++)
                            {
                                int length = 0;
                                do
                                {
                                    webBrowser1.BeginInvoke((MethodInvoker)delegate
                                    {
                                        length = webBrowser1.DocumentText.Split('\n').Length;
                                    });
                                    Thread.Sleep(600);
                                } while (length < 30);
                                webBrowser1.BeginInvoke((MethodInvoker)delegate
                                {
                                    start += 100;
                                    try
                                    {
                                        webBrowser1.Url = new System.Uri("https://www.google.com/search?q=intitle:%22Pok%C3%A9mon%20Showdown%22%20" + user + "&safe=off&filter=0&nfpr=1&num=1000&start=" + start);
                                    }
                                    catch (Exception)
                                    {}
                                });
                            }
                        }
                        catch (InvalidOperationException exception)
                        {

                        }
                    });
                    thread.Start();
                }
                else
                {
                    this.BringToFront();
                }
            }
        }

        private void Google_FormClosing(object sender, FormClosingEventArgs e)
        { 
            if (!htmlText.Contains(webBrowser1.DocumentText))
            {
                htmlText.Add(webBrowser1.DocumentText);
            }
            if (thread != null)
            {
                if (thread.IsAlive)
                {
                    running = false;
                }
            }
        }

    }
}
