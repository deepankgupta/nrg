namespace SmartDeviceApplication
{
    partial class MessageApplicationForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MainMenu mainMenu1;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mainMenu1 = new System.Windows.Forms.MainMenu();
            this.Options = new System.Windows.Forms.MenuItem();
            this.ChatMenuItem = new System.Windows.Forms.MenuItem();
            this.ExitMenuItem = new System.Windows.Forms.MenuItem();
            this.BuddyList = new System.Windows.Forms.ListView();
            this.MessageTextBox = new System.Windows.Forms.TextBox();
            this.LabelID = new System.Windows.Forms.Label();
            this.ChatList = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.Add(this.Options);
            // 
            // Options
            // 
            this.Options.MenuItems.Add(this.ChatMenuItem);
            this.Options.MenuItems.Add(this.ExitMenuItem);
            this.Options.Text = "Message Options";
            // 
            // ChatMenuItem
            // 
            this.ChatMenuItem.Text = "CHAT";
            this.ChatMenuItem.Click += new System.EventHandler(this.ChatMenuItem_Click);
            // 
            // ExitMenuItem
            // 
            this.ExitMenuItem.Text = "EXIT";
            this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
            // 
            // BuddyList
            // 
            this.BuddyList.Location = new System.Drawing.Point(-3, 0);
            this.BuddyList.Name = "BuddyList";
            this.BuddyList.Size = new System.Drawing.Size(176, 146);
            this.BuddyList.TabIndex = 3;
            // 
            // MessageTextBox
            // 
            this.MessageTextBox.Location = new System.Drawing.Point(3, 152);
            this.MessageTextBox.Name = "MessageTextBox";
            this.MessageTextBox.Size = new System.Drawing.Size(170, 22);
            this.MessageTextBox.TabIndex = 1;
            // 
            // LabelID
            // 
            this.LabelID.Location = new System.Drawing.Point(3, 152);
            this.LabelID.Name = "LabelID";
            this.LabelID.Size = new System.Drawing.Size(173, 22);
            this.LabelID.Text = "  ";
            // 
            // ChatList
            // 
            this.ChatList.Location = new System.Drawing.Point(0, 3);
            this.ChatList.Name = "ChatList";
            this.ChatList.Size = new System.Drawing.Size(176, 134);
            this.ChatList.TabIndex = 4;
            // 
            // MessageApplicationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(176, 180);
            this.Controls.Add(this.ChatList);
            this.Controls.Add(this.LabelID);
            this.Controls.Add(this.BuddyList);
            this.Controls.Add(this.MessageTextBox);
            this.Menu = this.mainMenu1;
            this.Name = "MessageApplicationForm";
            this.Text = "MessageApplicationForm";
            this.Load += new System.EventHandler(this.MessageApplicationForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.MenuItem Options;
        private System.Windows.Forms.MenuItem ChatMenuItem;
        private System.Windows.Forms.MenuItem ExitMenuItem;
        private System.Windows.Forms.ListView BuddyList;
        private System.Windows.Forms.TextBox MessageTextBox;
        private System.Windows.Forms.Label LabelID;
        private System.Windows.Forms.ListView ChatList;
    }
}