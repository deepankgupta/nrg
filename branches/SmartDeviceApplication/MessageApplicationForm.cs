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
    public partial class MessageApplicationForm : Form
    {
        // Data Members
        private Node nodeDeviceObect;
        private Thread ReceiverThread;
        private AodvProtocolClass aodvProtocolObject;
        private static Hashtable hashBuddyList;

       
        public MessageApplicationForm()
        {
            InitializeComponent();
            try
            {
                NetworkClass.InitializeIpAddress();
                nodeDeviceObect = new Node();
                RouterTableClass.Initialize();
                hashBuddyList = new Hashtable();
                ReceiverThread = new Thread(new ThreadStart
                                (AodvProtocolClass.ReceiveMessageServer));
                ReceiverThread.Start();
                               
            }
            catch (Exception e)
            {
                MessageBox.Show("SmartDevice Conf File() Exception is occurred: " + e.Message);
            }
        }

        private void MessageApplicationForm_Load(object sender, EventArgs e)
        {
            try
            {
                SetHashBuddyList();
                ShowBuddyList();
                LblID.Text = "My Name: " + Node.Name;
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
                    buddyIdText= BuddyList.Items[(BuddyList.SelectedIndices[0])].SubItems[0].Text;
                                       
                    if (!buddyIdText.Equals("NA"))
                    {
                        aodvProtocolObject = new AodvProtocolClass(buddyIdText);
                        HideBuddyWindow();
                        ShowChatWindow();
                    }
                    else
                    {
                        MessageBox.Show("Please select a buddy to chat!");
                    }
                }
                else if (ChatMenuItem.Text == "SEND")
                {
                    if (MessageTextBox.Text.ToString().Length > 0)
                    {
                        aodvProtocolObject.SendMessage(MessageTextBox.Text);
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

        public static void SetHashBuddyList()
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

                    if (!currentElement.GetAttribute("DestinationID").Equals(Node.Id))
                    {
                        nodeId = currentElement.GetAttribute("DestinationID").ToString();
                        nodeName = currentElement.GetAttribute("NAME").ToString();
                        hashBuddyList.Add(nodeId, nodeName);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception in setHashBuddyList() : " + e.Message);
            }
        }

        private void ShowBuddyList()
        {
            try
            {
                HideChatWindow();
                ShowBuddyWindow();
                BuddyList.View = View.Details;
                BuddyList.FullRowSelect = true;
                BuddyList.Columns.Add("Node ID", -2, HorizontalAlignment.Left);
                BuddyList.Columns.Add("Name", -2, HorizontalAlignment.Left);

                IDictionaryEnumerator ide = hashBuddyList.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (!ide.Key.ToString().Equals(Node.Id))
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

        private void HideChatWindow()
        {
            MessageTextBox.Visible = false;
            ChatList.Visible = false;
            ExitMenuItem.Text = "EXIT";
            ChatMenuItem.Text = "CHAT";
        }

        private void ShowChatWindow()
        {
            MessageTextBox.Visible = true;
            MessageTextBox.Focus();
            ChatList.View = View.Details;
            ChatList.FullRowSelect = true;
            ChatList.Visible = true;
            ExitMenuItem.Text = "CLOSE";
            ChatMenuItem.Text = "SEND";
        }

        private void HideBuddyWindow()
        {
            LblID.Visible = false;
            BuddyList.Visible = false;
        }

        private void ShowBuddyWindow()
        {
            LblID.Visible = true;
            BuddyList.Visible = true;
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            if (ExitMenuItem.Text == "CLOSE")
            {
                HideChatWindow();
                ShowBuddyWindow();
   
            }
            else if (ExitMenuItem.Text == "EXIT")
            {
                Application.Exit();
            }
        }

    }
}