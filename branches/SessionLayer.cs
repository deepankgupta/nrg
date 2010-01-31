using System;
using System.Xml;
using System.Threading;
using OpenNETCF;
using OpenNETCF.Threading;
using System.ComponentModel;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Net;
using System.Data;
using System.Collections;
using System.Runtime.CompilerServices;

namespace SmartDeviceApplication
{
    /// <summary>
    /// This class act as medium b/w Application
    /// Layer and Transport layer . Interprets
    /// Commands from the Application Layer
    /// and generates appropriate Messages to be
    /// sent and vice versa for Receiving
    /// </summary>
    public class SessionLayer
    {
        public string buddyId;
        private Node node;
        private int broadcastId;
        private bool chatInitiate;
        private string sessionId;
        private bool hasReceivedAck;
        private bool hasSentData;
        private string sessionLayerStream;
        private string upperLayerStream;
        private RouteTable routeTable;
        private NetworkLayer networkLayer;
        private DataPacketTimer dataPacketTimer;
        private XmlElement applicationHeaderElement;
        private MessageApplicationForm messageForm;
        private static volatile SessionLayer instance;
        private static object syncRoot = new Object();

        private SessionLayer()
        {
            node = Node.nodeInstance;
            chatInitiate = false;
            sessionId = "";
            hasReceivedAck = false;
            hasSentData = false;
            SetBroadCastId();
            applicationHeaderElement = (XmlElement)XmlFileUtility.configFileDocument.DocumentElement.
                                                SelectSingleNode("ApplicationLayer");
            dataPacketTimer = new DataPacketTimer(TimerConstants.DATA_TIMER);
            routeTable = RouteTable.routeTableInstance;
        }

        public static SessionLayer sessionLayerInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new SessionLayer();
                    }
                }
                return instance;
            }
        }

        public void ResetAll()
        {
            hasReceivedAck = false;
            hasSentData = false;
            chatInitiate = false;
            sessionId = "";
            messageForm = MessageApplicationForm.messageFormInstance;
            messageForm.Invoke(messageForm.hideChatWindowDelegate, messageForm);
            messageForm.Invoke(messageForm.resetFormControls, messageForm);
            dataPacketTimer.ReleaseAllTimerThread();
        }

        public class DataPacketTimer : Timer
        {
            public DataPacketTimer(int count) : base(count) { }

            public override void SetTimer(string sentStreamId)
            {
                WaitBufferWindow();
                if (bufferWindowForStreamSent.Count == 1)
                {
                    SetTimerThread = new Thread(new ThreadStart(SetTimerStart));
                    threadWindowForStreamSent.Add(sentStreamId, SetTimerThread);
                    SetTimerThread.Start();
                }
                SignalBufferWindow();
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void SetTimerStart()
            {
                int deltaIncrement = 0;
                while (deltaIncrement <= countTimer)
                {
                    Thread.Sleep(countTimer);
                    deltaIncrement += countTimer;
                }

                if (deltaIncrement > countTimer)
                {
                    Thread currentThread = Thread.CurrentThread;
                    IDictionaryEnumerator ide = threadWindowForStreamSent.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (((Thread)ide.Value).Equals(currentThread))
                        {
                            string storedStream = FindStoredStreamInBufferWindow(ide.Key.ToString());
                            object[] dataStream = new object[2];
                            XmlFileUtility.FilterStream(storedStream, sessionLayerInstance.applicationHeaderElement, dataStream);
                            storedStream = (string)dataStream[0];
                            SessionLayerPacket storedPacket = SessionLayerPacket.TransformStreamToPacket(storedStream);

                            if (!storedPacket.packetType.Equals(PacketConstants.DATA_PACKET))
                            {
                                MessageBox.Show(PacketConstants.LINK_BREAK, "Terminate", MessageBoxButtons.OK,
                                                MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                                sessionLayerInstance.ResetAll();
                                routeTable.DeleteRouteEntryForNode(sessionLayerInstance.buddyId);
                            }

                            ReleaseTimer(storedStreamId);
                        }
                    }
                }
            }
        }

        public void SetBroadCastId()
        {
            Random r = new Random();
            broadcastId = r.Next(1, int.MaxValue);
        }

        public void PerformSenderAction(string optionSelected)
        {
            broadcastId++;
            string combinedDataStream = string.Empty;
            SessionLayerPacket.PacketBuilder packetBuilder = new SessionLayerPacket.PacketBuilder();
            packetBuilder.SetPacketId(node.id + broadcastId.ToString()).SetSourceId(node.id);
            SessionLayerPacket packet = new SessionLayerPacket(packetBuilder);

            if (optionSelected.Equals("CHAT"))
            {
                if (chatInitiate == false)
                {
                    packet = packetBuilder.SetPacketType(PacketConstants.START_CHAT_PACKET).BuildAll();
                    sessionId = node.id + buddyId;
                    chatInitiate = true;
                    PrepareSessionLayerStream(packet, PacketConstants.EmptyString, buddyId);
                }
                else
                {
                    MessageBox.Show("Already A Chat In Progress",
                    "Hold On", MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                     MessageBoxDefaultButton.Button1);
                    return;
                }
            }

            if (optionSelected.Equals("CLOSE"))
            {
                packet = packetBuilder.SetPacketType(PacketConstants.TERMINATE_CHAT_PACKET).BuildAll();
                dataPacketTimer.ReleaseTimer(packet.packetId);
                this.ResetAll();
                PrepareSessionLayerStream(packet, PacketConstants.EmptyString, buddyId);
            }

            if (optionSelected.Equals("SEND"))
            {
                hasSentData = true;
                packet = packetBuilder.SetPacketType(PacketConstants.DATA_PACKET).BuildAll();

                object[] objectItems = new object[3];
                objectItems[0] = messageForm;
                objectItems[1] = node.name;
                objectItems[2] = messageForm.MessageTextBox.Text;

                messageForm.Invoke(messageForm.updateChatWindowDelegate, new object[] { objectItems });
                PrepareSessionLayerStream(packet, messageForm.MessageTextBox.Text, buddyId);
            }
        }

        public void HandleReceivedLowerLayerStream(string receivedLowerLayerStream)
        {
            object[] dataStream = new object[2];
            XmlFileUtility.FilterStream(receivedLowerLayerStream, applicationHeaderElement, dataStream);
            sessionLayerStream = (string)dataStream[0];
            upperLayerStream = (string)dataStream[1];
            networkLayer = NetworkLayer.networkLayerInstance;
            messageForm = MessageApplicationForm.messageFormInstance;

            SessionLayerPacket receivedPacket = SessionLayerPacket.TransformStreamToPacket(sessionLayerStream);
            string receivedPacketType = receivedPacket.packetType;

            //1. START CHAT PACKET
            if (receivedPacketType.Equals(PacketConstants.START_CHAT_PACKET))
            {
                string buddyName;
                string acceptChat = "No";
                SetBroadCastId();
                SessionLayerPacket.PacketBuilder packetBuilder = new SessionLayerPacket.PacketBuilder();

                packetBuilder = packetBuilder.SetPacketId(node.id + broadcastId.ToString())
                              .SetSourceId(node.id);

                if (chatInitiate == false)
                {
                    buddyName = routeTable.GetNameByIDInRouterTable(receivedPacket.sourceId);
                    acceptChat = MessageBox.Show(buddyName + " wants to chat with you! ",
                                                "Start Chat Packet", MessageBoxButtons.YesNo,
                                                MessageBoxIcon.Question, MessageBoxDefaultButton.Button1).ToString();
                    if (acceptChat.Equals("Yes"))
                    {
                        buddyId = receivedPacket.sourceId;
                        sessionId = node.id + buddyId;
                        hasReceivedAck = true;
                        chatInitiate = true;
                        SessionLayerPacket acceptChatPacket = packetBuilder.SetPacketType(PacketConstants.
                                                                ACCEPT_START_CHAT_PACKET).BuildAll();
                        messageForm.Invoke(messageForm.showChatWindowDelegate, messageForm);
                        PrepareSessionLayerStream(acceptChatPacket, upperLayerStream, buddyId);
                    }
                }

                if (acceptChat.Equals("No"))
                {
                    SessionLayerPacket rejectChatPacket = packetBuilder.SetPacketType(PacketConstants.
                                                                    REJECT_START_CHAT_PACKET).BuildAll();
                    PrepareSessionLayerStream(rejectChatPacket, upperLayerStream, receivedPacket.sourceId);
                }
            }

            // 2.Accept START CHAT 
            else if (receivedPacketType.Equals(PacketConstants.ACCEPT_START_CHAT_PACKET))
            {
                string currentSessionId = node.id + receivedPacket.sourceId;

                if (currentSessionId.Equals(sessionId))
                {
                    hasReceivedAck = false;
                    sessionId = node.id + buddyId;
                    dataPacketTimer.ReleaseAllTimerThread();
                    string buddyName = routeTable.GetNameByIDInRouterTable(receivedPacket.sourceId);

                    MessageBox.Show(buddyName + " agrees to talk with you",
                    "Confirmation Packet", MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button1);
                    messageForm.Invoke(messageForm.showChatWindowDelegate, messageForm);
                }
            }

            //3. DATA PACKET
            else if (receivedPacketType.Equals(PacketConstants.DATA_PACKET))
            {
                string currentSessionId = node.id + receivedPacket.sourceId;

                if (currentSessionId.Equals(sessionId))
                {
                    if (hasReceivedAck == true)
                    {
                        hasReceivedAck = false;
                        dataPacketTimer.ReleaseAllTimerThread();
                    }
                    if (hasSentData == true)
                    {
                        hasSentData = false;
                        dataPacketTimer.ReleaseAllTimerThread();
                    }
                    string buddyName = routeTable.GetNameByIDInRouterTable
                                                    (receivedPacket.sourceId);
                    object[] objectItems = new object[3];
                    objectItems[0] = messageForm;
                    objectItems[1] = buddyName;
                    objectItems[2] = upperLayerStream;

                    messageForm.Invoke(messageForm.updateChatWindowDelegate, new object[] { objectItems });
                }
            }

         //4. TERMINATE CHAT
            else if (receivedPacketType.Equals(PacketConstants.TERMINATE_CHAT_PACKET))
            {
                string currentSessionId = node.id + receivedPacket.sourceId;

                if (currentSessionId.Equals(sessionId))
                {
                    string buddyName = routeTable.GetNameByIDInRouterTable(receivedPacket.sourceId);
                    MessageBox.Show(buddyName + " has terminated the chat",
                    "Terminate", MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button1);

                    this.ResetAll();
                }
            }

            else if (receivedPacket.packetType.Equals(PacketConstants.REJECT_START_CHAT_PACKET))
            {
                string currentSessionId = node.id + receivedPacket.sourceId;

                if (currentSessionId.Equals(sessionId))
                {
                    string buddyName = routeTable.GetNameByIDInRouterTable
                                                       (receivedPacket.sourceId);
                    MessageBox.Show(buddyName + " Rejected the Chat Offer",
                    "Terminate", MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button1);

                    this.ResetAll();
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void PrepareSessionLayerStream(SessionLayerPacket packet, string upperLayerStream,
                                                                        string destinationId)
        {
            sessionLayerStream = packet.CreateStreamFromPacket();
            string combinedDataStream = XmlFileUtility.CombineLayerStreams(applicationHeaderElement,
                                                   sessionLayerStream, upperLayerStream);
            if (!(packet.packetType.Equals(PacketConstants.TERMINATE_CHAT_PACKET) ||
                packet.packetType.Equals(PacketConstants.REJECT_START_CHAT_PACKET)))
            {
                dataPacketTimer.SaveInBufferWindow(packet.packetId, combinedDataStream);
                dataPacketTimer.SetTimer(packet.packetId);
            }

            SendPacketToLowerLayer(combinedDataStream, destinationId);
        }
        
        public void SendPacketToLowerLayer(string combinedDataStream, string destinationId)
        {
            networkLayer = NetworkLayer.networkLayerInstance;
            networkLayer.AddNetworkLayerStream(combinedDataStream, destinationId);
        }
    }
}
