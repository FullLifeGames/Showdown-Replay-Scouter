
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
            ScoutReplayButton = new System.Windows.Forms.Button();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            DescriptionLabel = new System.Windows.Forms.Label();
            LinksLabel = new System.Windows.Forms.Label();
            LinksTextBox = new System.Windows.Forms.TextBox();
            OpponentLabel = new System.Windows.Forms.Label();
            OpponentTextBox = new System.Windows.Forms.TextBox();
            TierLabel = new System.Windows.Forms.Label();
            TierTextBox = new System.Windows.Forms.TextBox();
            UsernameLabel = new System.Windows.Forms.Label();
            UsernameTextBox = new System.Windows.Forms.TextBox();
            OutputWindow = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // ScoutReplayButton
            // 
            ScoutReplayButton.Enabled = false;
            ScoutReplayButton.Location = new System.Drawing.Point(15, 513);
            ScoutReplayButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            ScoutReplayButton.Name = "ScoutReplayButton";
            ScoutReplayButton.Size = new System.Drawing.Size(265, 43);
            ScoutReplayButton.TabIndex = 4;
            ScoutReplayButton.Text = "Scout Replays";
            ScoutReplayButton.UseVisualStyleBackColor = true;
            ScoutReplayButton.Click += Scout_Replay_Click;
            // 
            // splitContainer1
            // 
            splitContainer1.Location = new System.Drawing.Point(14, 16);
            splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(DescriptionLabel);
            splitContainer1.Panel1.Controls.Add(LinksLabel);
            splitContainer1.Panel1.Controls.Add(LinksTextBox);
            splitContainer1.Panel1.Controls.Add(OpponentLabel);
            splitContainer1.Panel1.Controls.Add(OpponentTextBox);
            splitContainer1.Panel1.Controls.Add(TierLabel);
            splitContainer1.Panel1.Controls.Add(TierTextBox);
            splitContainer1.Panel1.Controls.Add(UsernameLabel);
            splitContainer1.Panel1.Controls.Add(UsernameTextBox);
            splitContainer1.Panel1.Controls.Add(ScoutReplayButton);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(OutputWindow);
            splitContainer1.Size = new System.Drawing.Size(887, 568);
            splitContainer1.SplitterDistance = 294;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 1;
            // 
            // DescriptionLabel
            // 
            DescriptionLabel.AutoSize = true;
            DescriptionLabel.Location = new System.Drawing.Point(15, 7);
            DescriptionLabel.Name = "DescriptionLabel";
            DescriptionLabel.Size = new System.Drawing.Size(255, 20);
            DescriptionLabel.TabIndex = 9;
            DescriptionLabel.Text = "Minimum: Username or list of replays";
            // 
            // LinksLabel
            // 
            LinksLabel.AutoSize = true;
            LinksLabel.Location = new System.Drawing.Point(15, 253);
            LinksLabel.Name = "LinksLabel";
            LinksLabel.Size = new System.Drawing.Size(162, 20);
            LinksLabel.TabIndex = 8;
            LinksLabel.Text = "Replay Links (Optional)";
            // 
            // LinksTextBox
            // 
            LinksTextBox.Location = new System.Drawing.Point(15, 277);
            LinksTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            LinksTextBox.MaxLength = 32767999;
            LinksTextBox.Multiline = true;
            LinksTextBox.Name = "LinksTextBox";
            LinksTextBox.PlaceholderText = "e.g. \"https://replay.pokemonshowdown.com/gen8ou-1089874504\"";
            LinksTextBox.Size = new System.Drawing.Size(265, 212);
            LinksTextBox.TabIndex = 3;
            LinksTextBox.TextChanged += LinksTextBox_TextChanged;
            // 
            // OpponentLabel
            // 
            OpponentLabel.AutoSize = true;
            OpponentLabel.Location = new System.Drawing.Point(15, 187);
            OpponentLabel.Name = "OpponentLabel";
            OpponentLabel.Size = new System.Drawing.Size(148, 20);
            OpponentLabel.TabIndex = 6;
            OpponentLabel.Text = "Opponent (Optional)";
            // 
            // OpponentTextBox
            // 
            OpponentTextBox.Location = new System.Drawing.Point(15, 211);
            OpponentTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            OpponentTextBox.Name = "OpponentTextBox";
            OpponentTextBox.PlaceholderText = "e.g. \"fulllifegames, Senor L\"";
            OpponentTextBox.Size = new System.Drawing.Size(265, 27);
            OpponentTextBox.TabIndex = 2;
            // 
            // TierLabel
            // 
            TierLabel.AutoSize = true;
            TierLabel.Location = new System.Drawing.Point(15, 117);
            TierLabel.Name = "TierLabel";
            TierLabel.Size = new System.Drawing.Size(106, 20);
            TierLabel.TabIndex = 4;
            TierLabel.Text = "Tier (Optional)";
            // 
            // TierTextBox
            // 
            TierTextBox.Location = new System.Drawing.Point(15, 141);
            TierTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            TierTextBox.Name = "TierTextBox";
            TierTextBox.PlaceholderText = "e.g. \"gen8ou, gen7ou\"";
            TierTextBox.Size = new System.Drawing.Size(265, 27);
            TierTextBox.TabIndex = 1;
            TierTextBox.TextChanged += TierTextBox_TextChanged;
            // 
            // UsernameLabel
            // 
            UsernameLabel.AutoSize = true;
            UsernameLabel.Location = new System.Drawing.Point(15, 49);
            UsernameLabel.Name = "UsernameLabel";
            UsernameLabel.Size = new System.Drawing.Size(147, 20);
            UsernameLabel.TabIndex = 2;
            UsernameLabel.Text = "Username (Optional)";
            // 
            // UsernameTextBox
            // 
            UsernameTextBox.Location = new System.Drawing.Point(15, 73);
            UsernameTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            UsernameTextBox.Name = "UsernameTextBox";
            UsernameTextBox.PlaceholderText = "e.g. \"fulllifegames, Senor L\"";
            UsernameTextBox.Size = new System.Drawing.Size(265, 27);
            UsernameTextBox.TabIndex = 0;
            UsernameTextBox.TextChanged += UsernameTextBox_TextChanged;
            // 
            // OutputWindow
            // 
            OutputWindow.Location = new System.Drawing.Point(3, 4);
            OutputWindow.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            OutputWindow.Multiline = true;
            OutputWindow.Name = "OutputWindow";
            OutputWindow.ReadOnly = true;
            OutputWindow.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            OutputWindow.Size = new System.Drawing.Size(580, 559);
            OutputWindow.TabIndex = 0;
            // 
            // Main
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(914, 600);
            Controls.Add(splitContainer1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            MaximizeBox = false;
            Name = "Main";
            Text = "Showdown Replay Scouter";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
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

