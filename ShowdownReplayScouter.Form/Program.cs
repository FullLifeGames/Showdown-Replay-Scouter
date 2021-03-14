using System;
using System.Windows.Forms;

namespace Showdown_Replay_Scouter
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length > 1)
            {
                Application.Run(new Form1(args));
            }
            else
            {
                Application.Run(new Form1());
            }
        }
    }
}
