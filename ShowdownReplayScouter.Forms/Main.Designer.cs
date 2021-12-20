
namespace ShowdownReplayScouter.Forms
{
    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.ScoutReplayButton = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.DescriptionLabel = new System.Windows.Forms.Label();
            this.LinksLabel = new System.Windows.Forms.Label();
            this.LinksTextBox = new System.Windows.Forms.TextBox();
            this.OpponentLabel = new System.Windows.Forms.Label();
            this.OpponentTextBox = new System.Windows.Forms.TextBox();
            this.TierLabel = new System.Windows.Forms.Label();
            this.TierTextBox = new System.Windows.Forms.TextBox();
            this.UsernameLabel = new System.Windows.Forms.Label();
            this.UsernameTextBox = new System.Windows.Forms.TextBox();
            this.OutputWindow = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ScoutReplayButton
            // 
            this.ScoutReplayButton.Enabled = false;
            this.ScoutReplayButton.Location = new System.Drawing.Point(15, 513);
            this.ScoutReplayButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ScoutReplayButton.Name = "ScoutReplayButton";
            this.ScoutReplayButton.Size = new System.Drawing.Size(265, 43);
            this.ScoutReplayButton.TabIndex = 4;
            this.ScoutReplayButton.Text = "Scout Replays";
            this.ScoutReplayButton.UseVisualStyleBackColor = true;
            this.ScoutReplayButton.Click += new System.EventHandler(this.Scout_Replay_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Location = new System.Drawing.Point(14, 16);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.DescriptionLabel);
            this.splitContainer1.Panel1.Controls.Add(this.LinksLabel);
            this.splitContainer1.Panel1.Controls.Add(this.LinksTextBox);
            this.splitContainer1.Panel1.Controls.Add(this.OpponentLabel);
            this.splitContainer1.Panel1.Controls.Add(this.OpponentTextBox);
            this.splitContainer1.Panel1.Controls.Add(this.TierLabel);
            this.splitContainer1.Panel1.Controls.Add(this.TierTextBox);
            this.splitContainer1.Panel1.Controls.Add(this.UsernameLabel);
            this.splitContainer1.Panel1.Controls.Add(this.UsernameTextBox);
            this.splitContainer1.Panel1.Controls.Add(this.ScoutReplayButton);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.OutputWindow);
            this.splitContainer1.Size = new System.Drawing.Size(887, 568);
            this.splitContainer1.SplitterDistance = 294;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 1;
            // 
            // DescriptionLabel
            // 
            this.DescriptionLabel.AutoSize = true;
            this.DescriptionLabel.Location = new System.Drawing.Point(15, 7);
            this.DescriptionLabel.Name = "DescriptionLabel";
            this.DescriptionLabel.Size = new System.Drawing.Size(255, 20);
            this.DescriptionLabel.TabIndex = 9;
            this.DescriptionLabel.Text = "Minimum: Username or list of replays";
            // 
            // LinksLabel
            // 
            this.LinksLabel.AutoSize = true;
            this.LinksLabel.Location = new System.Drawing.Point(15, 253);
            this.LinksLabel.Name = "LinksLabel";
            this.LinksLabel.Size = new System.Drawing.Size(162, 20);
            this.LinksLabel.TabIndex = 8;
            this.LinksLabel.Text = "Replay Links (Optional)";
            // 
            // LinksTextBox
            // 
            this.LinksTextBox.Location = new System.Drawing.Point(15, 277);
            this.LinksTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.LinksTextBox.Multiline = true;
            this.LinksTextBox.Name = "LinksTextBox";
            this.LinksTextBox.PlaceholderText = "e.g. \"https://replay.pokemonshowdown.com/gen8ou-1089874504\"";
            this.LinksTextBox.Size = new System.Drawing.Size(265, 212);
            this.LinksTextBox.TabIndex = 3;
            this.LinksTextBox.TextChanged += new System.EventHandler(this.LinksTextBox_TextChanged);
            // 
            // OpponentLabel
            // 
            this.OpponentLabel.AutoSize = true;
            this.OpponentLabel.Location = new System.Drawing.Point(15, 187);
            this.OpponentLabel.Name = "OpponentLabel";
            this.OpponentLabel.Size = new System.Drawing.Size(148, 20);
            this.OpponentLabel.TabIndex = 6;
            this.OpponentLabel.Text = "Opponent (Optional)";
            // 
            // OpponentTextBox
            // 
            this.OpponentTextBox.Location = new System.Drawing.Point(15, 211);
            this.OpponentTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.OpponentTextBox.Name = "OpponentTextBox";
            this.OpponentTextBox.PlaceholderText = "e.g. \"fulllifegames\"";
            this.OpponentTextBox.Size = new System.Drawing.Size(265, 27);
            this.OpponentTextBox.TabIndex = 2;
            // 
            // TierLabel
            // 
            this.TierLabel.AutoSize = true;
            this.TierLabel.Location = new System.Drawing.Point(15, 117);
            this.TierLabel.Name = "TierLabel";
            this.TierLabel.Size = new System.Drawing.Size(106, 20);
            this.TierLabel.TabIndex = 4;
            this.TierLabel.Text = "Tier (Optional)";
            // 
            // TierTextBox
            // 
            this.TierTextBox.Location = new System.Drawing.Point(15, 141);
            this.TierTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.TierTextBox.Name = "TierTextBox";
            this.TierTextBox.PlaceholderText = "e.g. \"gen8ou\"";
            this.TierTextBox.Size = new System.Drawing.Size(265, 27);
            this.TierTextBox.TabIndex = 1;
            this.TierTextBox.TextChanged += new System.EventHandler(this.TierTextBox_TextChanged);
            // 
            // UsernameLabel
            // 
            this.UsernameLabel.AutoSize = true;
            this.UsernameLabel.Location = new System.Drawing.Point(15, 49);
            this.UsernameLabel.Name = "UsernameLabel";
            this.UsernameLabel.Size = new System.Drawing.Size(147, 20);
            this.UsernameLabel.TabIndex = 2;
            this.UsernameLabel.Text = "Username (Optional)";
            // 
            // UsernameTextBox
            // 
            this.UsernameTextBox.Location = new System.Drawing.Point(15, 73);
            this.UsernameTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.UsernameTextBox.Name = "UsernameTextBox";
            this.UsernameTextBox.PlaceholderText = "e.g. \"fulllifegames\"";
            this.UsernameTextBox.Size = new System.Drawing.Size(265, 27);
            this.UsernameTextBox.TabIndex = 0;
            this.UsernameTextBox.TextChanged += new System.EventHandler(this.UsernameTextBox_TextChanged);
            // 
            // OutputWindow
            // 
            this.OutputWindow.Location = new System.Drawing.Point(3, 4);
            this.OutputWindow.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.OutputWindow.Multiline = true;
            this.OutputWindow.Name = "OutputWindow";
            this.OutputWindow.ReadOnly = true;
            this.OutputWindow.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.OutputWindow.Size = new System.Drawing.Size(580, 559);
            this.OutputWindow.TabIndex = 0;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(914, 600);
            this.Controls.Add(this.splitContainer1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "Main";
            this.Text = "Showdown Replay Scouter";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button ScoutReplayButton;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TextBox OutputWindow;
        private System.Windows.Forms.Label TierLabel;
        private System.Windows.Forms.TextBox TierTextBox;
        private System.Windows.Forms.Label UsernameLabel;
        private System.Windows.Forms.TextBox UsernameTextBox;
        private System.Windows.Forms.Label OpponentLabel;
        private System.Windows.Forms.TextBox OpponentTextBox;
        private System.Windows.Forms.Label LinksLabel;
        private System.Windows.Forms.TextBox LinksTextBox;
        private System.Windows.Forms.Label DescriptionLabel;
    }
}

