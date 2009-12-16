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
        private NetworkClass networkHelpingObject;
        private RouterTableClass routerTableObject;
        private Hashtable hashBuddyList;
        private string buddyId;
        private string buddyName;
        private string buddyIpAddress;
        private Thread ReceiverThread;

        //Member Functions
        public MessageApplicationForm()
        {
            InitializeComponent();
            try
            {
                networkHelpingObject = new NetworkClass();
                nodeDeviceObect = new Node();
                hashBuddyList = new Hashtable();
                buddyId = "NA";
                ReceiverThread =new Thread(new ThreadStart(networkHelpingObject
                                .ReceiverThreadFunction));
            }
            catch (Exception e)
            {
                MessageBox.Show("SmartDevice Conf File() Exception is occurred: " + e.Message);
            }
        }

        private void MessageApplicationForm_Load(object sender, EventArgs e)
        {
            LblID.Text = "My Name: " + nodeDeviceObect.Name;
            SetHashBuddyList();
            ShowBuddyList();
            ChatList.View = View.Details;
            ChatList.FullRowSelect = true;
            ChatList.Columns.Add("Chats", -2, HorizontalAlignment.Left);
        }
         
        private void ChatMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
              
              
                buddyId = BuddyList.Items[(BuddyList.SelectedIndices[0])].SubItems[0].Text;
                buddyName = BuddyList.Items[(BuddyList.SelectedIndices[0])].SubItems[1].Text;
                buddyIpAddress = UtilityConfFile.GetIPAddressByIDInConfFile(buddyId);
                
                if (!buddyId.Equals("NA"))
                {
                    HideBuddyWindow();
                    ShowChatWindow();
                    //SendBroadcastNTPacket();
                    //isSourceNode = true;
                }
                else
                {
                    MessageBox.Show("Please select a buddy to chat!");
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
                XmlNode rootNode = UtilityConfFile.xmlDoc.DocumentElement;
                XmlNodeList xmlNodes = rootNode.ChildNodes;
                foreach (XmlNode childNode in xmlNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;
                    string nodeId = currentElement.GetAttribute("ID").ToString();
                    string nodeName = currentElement.GetAttribute("NAME").ToString();
                    hashBuddyList.Add(nodeId, nodeName);
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
                    if (!ide.Key.ToString().Equals(nodeDeviceObect.Id))
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

        private void back_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

    }
}