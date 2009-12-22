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
    /// Main Form Class controlling GUI and Initializing 
    /// other Objects like RouteTable,Node,Presentation
    /// Layer and some delegates handling as well.
    /// </summary>

    public partial class MessageApplicationForm : Form
    {
        private Node node;
        private Hashtable hashBuddyList;
        private RouterTableClass routeTable;
        private PresentationLayer presentationLayer;
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

        public static void HideChatWindow(MessageApplicationForm messageForm)
        {
            messageForm.ShowBuddyWindow();
        }

        public MessageApplicationForm()
        {
            InitializeComponent();
            try
            {
                //DELEGATES
                resetFormControls = new Reset(ResetAll);
                showChatWindowDelegate = new ChatWindowPanel(ChatWindowDisplay);
                updateChatWindowDelegate = new UpdateChatWindow(UpdateChatDisplay);
                hideChatWindowDelegate = new HideChatWindowPanel(HideChatWindow);

                hashBuddyList = new Hashtable();
                node = Node.nodeInstance;
                routeTable = RouterTableClass.routeTableInstance;   
            }
            catch (Exception e)
            {
                MessageBox.Show("SmartDevice Conf File() Exception is occurred: " + e.Message);
            }
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

        private static void ResetAll(MessageApplicationForm messageForm)
        {
            messageForm.ChatList.Items.Clear();
        }

        public void ResetAll()
        {
            ChatList.Items.Clear();
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

        public void SetHashBuddyList()
        {
            try
            {
                Monitor.Enter(routeTable.routeTableXml);

                XmlNode rootXmlNode = routeTable.routeTableXml.DocumentElement;
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
                Monitor.Exit(routeTable.routeTableXml);
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

        public void ShowChatWindow()
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

        public void ShowBuddyWindow()
        {
            LabelID.Visible = true;
            BuddyList.Visible = true;
            MessageTextBox.Visible = false;
            ChatList.Visible = false;
            ExitMenuItem.Text = "EXIT";
            ChatMenuItem.Text = "CHAT";

        }




        private void ChatMenuItem_Click(object sender, EventArgs e)
        {
            try
            {

                if (ChatMenuItem.Text == "CHAT")
                {
                    string buddyIdText = "NA";
                    buddyIdText = BuddyList.Items[(BuddyList.SelectedIndices[0])].SubItems[0].Text;
                    presentationLayer = PresentationLayer.presentationLayerInstance;

                    if (!buddyIdText.Equals("NA"))
                    {
                        presentationLayer.buddyId = buddyIdText;
                        presentationLayer.PerformSenderAction("CHAT");
                        buddyIdText = "NA";
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
                        presentationLayer.PerformSenderAction("SEND");
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

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            if (ExitMenuItem.Text == "CLOSE")
            {
                presentationLayer.chatTerminate = true;
                presentationLayer.PerformSenderAction("CLOSE");
            }
            else if (ExitMenuItem.Text == "EXIT")
            {
                Program.TerminateAllThreads();
            }
        }
    }
}