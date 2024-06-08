using NeoSmart.Caching.Sqlite;
using ShowdownReplayScouter.Core.ReplayScouter;
using ShowdownReplayScouter.Core.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace ShowdownReplayScouter.Forms
{
    public partial class Main : Form
    {
        private readonly ReplayScouter _replayScouter;

        public Main()
        {
            SQLitePCL.Batteries_V2.Init();
            _replayScouter = new Core.ReplayScouter.ShowdownReplayScouter(
                new SqliteCache(
                    new SqliteCacheOptions()
                    {
                        MemoryOnly = false,
                        CachePath = "ShowdownReplayScouter.db",
                    }
                )
            );

            InitializeComponent();
        }

        private async void Scout_Replay_Click(object sender, EventArgs e)
        {
            IEnumerable<Uri> linkUris = null;
            try
            {
                var linksText = LinksTextBox.Text.Trim();
                if (linksText.Length > 0)
                {
                    linkUris = linksText.Split('\n')
                        .Select((link) => new Uri(link.Trim()));
                }
            }
            catch
            {
                MessageBox.Show("Links can only contain Uris");
                return;
            }

            UsernameTextBox.ReadOnly = true;
            TierTextBox.ReadOnly = true;
            OpponentTextBox.ReadOnly = true;
            LinksTextBox.ReadOnly = true;
            ScoutReplayButton.Enabled = false;

            var scoutingRequest = new Core.Data.ScoutingRequest()
            {
                Users = UsernameTextBox.Text.Trim() != "" ? UsernameTextBox.Text.Trim().Split(',').Select((user) => user.Trim()) : null,
                Tiers = TierTextBox.Text.Trim() != "" ? TierTextBox.Text.Trim().Split(',').Select((tier) => tier.Trim()) : null,
                Opponents = OpponentTextBox.Text.Trim() != "" ? OpponentTextBox.Text.Trim().Split(',').Select((opponent) => opponent.Trim()) : null,
                Links = linkUris
            };

            var result = await _replayScouter.ScoutReplaysAsync(scoutingRequest).ConfigureAwait(false);

            var output = OutputPrinter.Print(scoutingRequest, result.Teams);

            Invoke((MethodInvoker)delegate
            {
                OutputWindow.Text = output;

                UsernameTextBox.ReadOnly = false;
                TierTextBox.ReadOnly = false;
                OpponentTextBox.ReadOnly = false;
                LinksTextBox.ReadOnly = false;
                ScoutReplayButton.Enabled = true;
            });
        }

        private void Button_Update()
        {
            ScoutReplayButton.Enabled =
                UsernameTextBox.Text.Trim().Length > 0
                || LinksTextBox.Text.Trim().Length > 0;
        }

        private void UsernameTextBox_TextChanged(object sender, EventArgs e)
        {
            Button_Update();
        }

        private void TierTextBox_TextChanged(object sender, EventArgs e)
        {
            Button_Update();
        }

        private void LinksTextBox_TextChanged(object sender, EventArgs e)
        {
            Button_Update();
        }
    }
}
