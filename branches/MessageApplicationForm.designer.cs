using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Net;
using System.Data;
using System.Collections;

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
        /// 

        private static volatile MessageApplicationForm instance;
        private static object syncRoot = new Object();

        public static MessageApplicationForm messageFormInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new MessageApplicationForm();
                    }
                }
                return instance;
            }
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.Options = new System.Windows.Forms.MenuItem();
            this.ChatMenuItem = new System.Windows.Forms.MenuItem();
            this.ExitMenuItem = new System.Windows.Forms.MenuItem();
            this.BuddyList = new System.Windows.Forms.ListView();
            this.MessageTextBox = new System.Windows.Forms.TextBox();
            this.LabelID = new System.Windows.Forms.Label();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.ChatList = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.Options});
            // 
            // Options
            // 
            this.Options.Index = 0;
            this.Options.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.ChatMenuItem,
            this.ExitMenuItem});
            this.Options.Text = "Message Options";
            // 
            // ChatMenuItem
            // 
            this.ChatMenuItem.Index = 0;
            this.ChatMenuItem.Text = "";
            // 
            // ExitMenuItem
            // 
            this.ExitMenuItem.Index = 1;
            this.ExitMenuItem.Text = "";
            // 
            // BuddyList
            // 
            this.BuddyList.Location = new System.Drawing.Point(0, 0);
            this.BuddyList.Name = "BuddyList";
            this.BuddyList.Size = new System.Drawing.Size(101, 103);
            this.BuddyList.TabIndex = 3;
            this.BuddyList.UseCompatibleStateImageBehavior = false;
            // 
            // MessageTextBox
            // 
            this.MessageTextBox.Location = new System.Drawing.Point(145, 162);
            this.MessageTextBox.Name = "MessageTextBox";
            this.MessageTextBox.Size = new System.Drawing.Size(214, 20);
            this.MessageTextBox.TabIndex = 1;
            // 
            // LabelID
            // 
            this.LabelID.Location = new System.Drawing.Point(-3, 119);
            this.LabelID.Name = "LabelID";
            this.LabelID.Size = new System.Drawing.Size(173, 22);
            this.LabelID.TabIndex = 0;
            this.LabelID.Text = "  ";
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Items.AddRange(new object[] {
            "CHAT",
            "SEND",
            "CLOSE",
            "EXIT"});
            this.listBox1.Location = new System.Drawing.Point(6, 162);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(95, 56);
            this.listBox1.TabIndex = 4;
            this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // ChatList
            // 
            this.ChatList.Location = new System.Drawing.Point(145, 15);
            this.ChatList.Name = "ChatList";
            this.ChatList.Size = new System.Drawing.Size(214, 97);
            this.ChatList.TabIndex = 5;
            this.ChatList.UseCompatibleStateImageBehavior = false;
            // 
            // MessageApplicationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(371, 250);
            this.Controls.Add(this.ChatList);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.LabelID);
            this.Controls.Add(this.BuddyList);
            this.Controls.Add(this.MessageTextBox);
            this.Menu = this.mainMenu1;
            this.Name = "MessageApplicationForm";
            this.Text = "MessageApplicationForm";
            this.Load += new System.EventHandler(this.MessageApplicationForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuItem Options;
        private System.Windows.Forms.MenuItem ChatMenuItem;
        private System.Windows.Forms.MenuItem ExitMenuItem;
        private System.Windows.Forms.ListView BuddyList;
        public System.Windows.Forms.TextBox MessageTextBox;
        private System.Windows.Forms.Label LabelID;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.ListView ChatList;
    }
}