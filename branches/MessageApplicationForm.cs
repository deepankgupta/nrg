using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Xml;
using System.IO;

namespace SmartDeviceApplication
{
 
     
    /// <summary>
    /// Main Form Class controlling GUI and Initializing other Threads
    /// and other Objects like RouteTable,AodvProtocol,Network etc .
    /// </summary>
    /// 
    public partial class MessageApplicationForm : Form
    {
        private Hashtable hashBuddyList;
        public ChatWindowPanel showChatWindowDelegate;
        public UpdateChatWindow updateChatWindowDelegate;
        public HideChatWindowPanel hideChatWindowDelegate;
        public Reset resetFormControls;
        /// <summary>
        ///Delegates To call Functions from
        ///Receiver Thread///
        ///</summary>
        ///
        public delegate void ChatWindowPanel(MessageApplicationForm messageForm);
        public delegate void UpdateChatWindow(object[] objectItems);
        public delegate void HideChatWindowPanel(MessageApplicationForm messageForm);
        public delegate void Reset(MessageApplicationForm messageForm);


        public MessageApplicationForm()
        {
            InitializeComponent();
            try
            {
                hashBuddyList = new Hashtable();
                showChatWindowDelegate = new ChatWindowPanel(ChatWindowDisplay);
                updateChatWindowDelegate = new UpdateChatWindow(UpdateChatDisplay);
                hideChatWindowDelegate = new HideChatWindowPanel(HideChatWindow);
                resetFormControls = new Reset(ResetFormControls);
            }
            catch (Exception e)
            {
                MessageBox.Show("SmartDevice Conf File() Exception is occurred: " + e.Message);
            }
        }

        public static void HideChatWindow(MessageApplicationForm messageForm)
        {
            messageForm.ShowBuddyWindow();
        }

        public static void UpdateChatDisplay(object[] objectItems)
        {
            MessageApplicationForm messageForm = (MessageApplicationForm)objectItems[0];

            ListViewItem listViewItem = new ListViewItem(objectItems[1].ToString() + " says : "
                                                            + objectItems[2].ToString());
            messageForm.ChatList.Items.Add(listViewItem);
            messageForm.ShowChatWindow();
        }

        public static void ChatWindowDisplay(MessageApplicationForm messageForm)
        {
            messageForm.Show();
            messageForm.ShowChatWindow();
        }

        private static void ResetFormControls(MessageApplicationForm messageForm)
        {

        }


        private void MessageApplicationForm_Load(object sender, EventArgs e)
        {
            try
            {
                SetHashBuddyList();
                ShowBuddyList();
                LabelID.Text = "My Name: " + Node.name;
                ChatList.View = View.Details;
                ChatList.FullRowSelect = true;
                ChatList.Columns.Add("Chats", -2, HorizontalAlignment.Left);
        
            }
            catch (Exception Excep)
            {
                MessageBox.Show("SmartDevice Message Application Load Exception is occurred: " + Excep.Message);
            }
        }
         
        private void ChatMenuItem_Click(object sender, EventArgs e)
        {
            try
            {

                if (ChatMenuItem.Text == "CHAT")
                {
                    string buddyIdText = "NA";
                    buddyIdText = BuddyList.Items[(BuddyList.SelectedIndices[0])].SubItems[0].Text;

                    if (!buddyIdText.Equals("NA"))
                    {
                        AodvProtocolClass.chatInitiate = true;
                        AodvProtocolClass.buddyId = buddyIdText;
                        AodvProtocolClass.ProcessTextMessage("");
                        buddyIdText = "NA";
                        // this.Hide();
                    }
                    else
                    {
                        MessageBox.Show("Please select a buddy to chat!");
                    }

                }
                else if (ChatMenuItem.Text == "SEND")
                {
                    if (MessageTextBox.Text.Length > 0)
                    {
                        AodvProtocolClass.IsChating = true;
                        AodvProtocolClass.ProcessTextMessage(MessageTextBox.Text);
                        MessageTextBox.Text = "";
                    }
                    else
                    {
                        MessageBox.Show("Please enter a message first!");
                    }
                }
            }


            catch (Exception ex)
            {
                MessageBox.Show("Exception in btnChat_Click(): " + ex.Message);
            }
           
        }

        public void SetHashBuddyList()
        {
            try
            {
                Monitor.Enter(RouterTableClass.routeTableXml);

                XmlNode rootXmlNode = RouterTableClass.routeTableXml.DocumentElement;
                XmlNodeList childXmlNodes = rootXmlNode.ChildNodes;

                foreach (XmlNode childNode in childXmlNodes)
                {
                    string nodeId;
                    string nodeName;
                    XmlElement currentElement = (XmlElement)childNode;

                    if (!currentElement.GetAttribute("DestinationID").Equals(Node.id))
                    {
                        nodeId = currentElement.GetAttribute("DestinationID").ToString();
                        nodeName = currentElement.GetAttribute("NAME").ToString();
                        hashBuddyList.Add(nodeId, nodeName);
                    }
                }
                Monitor.Exit(RouterTableClass.routeTableXml);
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception in setHashBuddyList() : " + e.Message);
            }
        }

        public void ShowBuddyList()
        {
            try
            {
                ShowBuddyWindow();
                BuddyList.View = View.Details;
                BuddyList.FullRowSelect = true;
                BuddyList.Columns.Add("Node ID", -2, HorizontalAlignment.Left);
                BuddyList.Columns.Add("Name", -2, HorizontalAlignment.Left);

                IDictionaryEnumerator ide = hashBuddyList.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (!ide.Key.ToString().Equals(Node.id))
                    {
                        ListViewItem id = new ListViewItem(ide.Key.ToString());
                        id.SubItems.Add(ide.Value.ToString());
                        BuddyList.Items.Add(id);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("ShowBuddyList() Exception occurred: " + e.Message);
            }
        }

        private void ShowChatWindow()
        {
            LabelID.Visible = false;
            BuddyList.Visible = false;
            MessageTextBox.Visible = true;
            MessageTextBox.Focus();
            ChatList.View = View.Details;
            ChatList.FullRowSelect = true;
            ChatList.Visible = true;
            ExitMenuItem.Text = "CLOSE";
            ChatMenuItem.Text = "SEND";
        }

        private void ShowBuddyWindow()
        {
            LabelID.Visible = true;
            BuddyList.Visible = true;
            MessageTextBox.Visible = false;
            ChatList.Visible = false;
            ExitMenuItem.Text = "EXIT";
            ChatMenuItem.Text = "CHAT";
       
        }


        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            if (ExitMenuItem.Text == "CLOSE")
            {
                AodvProtocolClass.chatTerminate = true;
                AodvProtocolClass.ProcessTextMessage("");
                ShowBuddyWindow();
            }
            else if (ExitMenuItem.Text == "EXIT")
            {
                Application.Exit();
            }
        }
    }
}