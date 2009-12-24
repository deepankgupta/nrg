using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Net;
using System.Data;
using System.Collections;

namespace SmartDeviceApplication
{
    /// <summary>
    /// This class act as medium b/w Application
    /// Layer and Transport layer . Interprets
    /// Commands from the Application Layer
    /// and generates appropriate Messages to be
    /// sent and vice versa for Receiving
    /// </summary>
    public class PresentationLayer
    {
        public string buddyId;
        private Node node;
        private bool chatInitiate;
        private int broadcastId;
        private bool hasReceivedAck;
        private bool hasSentData;
        private RouterTableClass routeTable;
        private TransportLayer transportLayer;
        private MessageApplicationForm messageForm;
        private static volatile PresentationLayer instance;
        private static object syncRoot = new Object();

        private PresentationLayer()
        {
            node = Node.nodeInstance;
            chatInitiate = false;
            hasReceivedAck = false;
            hasSentData = false;
            SetBroadCastId();
            routeTable = RouterTableClass.routeTableInstance;
        }

        public static PresentationLayer presentationLayerInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new PresentationLayer();
                    }
                }
                return instance;
            }
        }

        public void SetBroadCastId()
        {
            Random random = new Random();
            broadcastId = random.Next(1, 100000);
        }

        public void ResetAll()
        {
            hasReceivedAck = false;
            hasSentData = false;
            chatInitiate = false;
            messageForm = MessageApplicationForm.messageFormInstance;
            messageForm.Invoke(messageForm.hideChatWindowDelegate, messageForm);
            messageForm.Invoke(messageForm.resetFormControls, messageForm);

        }

        public void PerformSenderAction(string optionSelected)
        {
            broadcastId++;
            Packet dataPacket = new Packet();
            Hashtable destinationInfoList = routeTable.GetDestinationInfoFromRouteTable(buddyId);
            PacketBuilder packetBuilder = new PacketBuilder();
            transportLayer = TransportLayer.transportLayerInstance;
            messageForm = MessageApplicationForm.messageFormInstance;

            packetBuilder = packetBuilder.setBroadcastId(broadcastId)
                              .setCurrentId(node.id)
                              .setSourceId(node.id)
                              .setDestinationId(buddyId)
                              .setSourceSeqNum(node.sequenceNumber)
                              .setHopCount(Convert.ToInt32(destinationInfoList
                                                        ["HopCount"].ToString()))
                              .setDestinationSeqNum(Convert.ToInt32(destinationInfoList
                                                        ["DestinationSequenceNum"].ToString()));


            if (optionSelected.Equals("CHAT"))
            {
                if (chatInitiate == false)
                {
                    dataPacket = packetBuilder.setPacketType(PacketConstants.START_CHAT_PACKET)
                              .build();
                    chatInitiate = true;
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
                dataPacket = packetBuilder.setPacketType(PacketConstants.TERMINATE_CHAT_PACKET)
                             .build();

                transportLayer.dataPacketTimer.ReleaseTimer();
                this.ResetAll();
            }

            if (optionSelected.Equals("SEND"))
            {
                hasSentData = true;
                dataPacket = packetBuilder.setPacketType(PacketConstants.DATA_PACKET)
                               .setPayLoadMessage(messageForm.MessageTextBox.Text)
                               .build();
                object[] objectItems = new object[3];
                objectItems[0] = messageForm;
                objectItems[1] = node.name;
                objectItems[2] = messageForm.MessageTextBox.Text;

                messageForm.Invoke(messageForm.updateChatWindowDelegate, new object[] { objectItems });
            }

            transportLayer.SendPacket(dataPacket);
        }

        public void HandleReceivedDataPackets(Packet receivedPacket)
        {
            string receivedPacketType = receivedPacket.packetType;
            string receivedPacketId = receivedPacket.sourceId + receivedPacket.broadcastId.ToString();

            //1. START CHAT PACKET
            if (receivedPacketType.Equals(PacketConstants.START_CHAT_PACKET))
            {
                transportLayer = TransportLayer.transportLayerInstance;
                messageForm = MessageApplicationForm.messageFormInstance;

                //Destination Node Receives
                if (receivedPacket.destinationId.Equals(node.id))
                {
                    string buddyName;
                    string acceptChat = "No";
                    if (chatInitiate == false)
                    {
                        buddyName = routeTable.GetNameByIDInRouterTable(receivedPacket.sourceId);
                        acceptChat = MessageBox.Show(buddyName + " wants to chat with you! ",
                                                    "Start Chat Packet", MessageBoxButtons.YesNo,
                                                    MessageBoxIcon.Question, MessageBoxDefaultButton.Button1).ToString();
                        if (acceptChat.Equals("Yes"))
                        {
                            SetBroadCastId();
                            buddyId = receivedPacket.sourceId;
                            hasReceivedAck = true;

                            PacketBuilder packetBuilder = new PacketBuilder();
                            Packet acceptChatPacket =
                                           packetBuilder.setPacketType(PacketConstants.ACCEPT_START_CHAT_PACKET)
                                          .setBroadcastId(broadcastId)
                                          .setCurrentId(node.id)
                                          .setSourceId(receivedPacket.destinationId)
                                          .setDestinationId(receivedPacket.sourceId)
                                          .setSourceSeqNum(receivedPacket.destinationSeqNum)
                                          .setDestinationSeqNum(receivedPacket.sourceSeqNum)
                                          .build();

                            transportLayer.SendPacket(acceptChatPacket);
                            messageForm.Invoke(messageForm.showChatWindowDelegate, messageForm);

                        }
                    }
                    if (acceptChat.Equals("No"))
                    {
                        //TODO Send Reject Chat Message
                    }
                }
                else
                {
                    if (!routeTable.IsDestinationPathEmpty(receivedPacket.destinationId))
                    {
                        Hashtable DestinationInfoList = routeTable.GetDestinationInfoFromRouteTable
                                                                (receivedPacket.destinationId);
                        string destinationIpAddress = routeTable.GetIPAddressByIDInRouterTable
                                                     (DestinationInfoList["NextHop"].ToString());
                        transportLayer.ForwardToNextNeighbour(receivedPacket, destinationIpAddress);

                    }
                    else
                    {
                        receivedPacket.payloadMessage = "LINK BREAK";
                        receivedPacket.packetType = PacketConstants.ROUTE_ERROR_PACKET;
                        string XmlMessageStream = receivedPacket.CreateMessageXmlstringFromPacket();
                        transportLayer.HandleReceivePacket(XmlMessageStream);
                    }
                }
            }

            // 2.Accept START CHAT 
            else if (receivedPacketType.Equals(PacketConstants.ACCEPT_START_CHAT_PACKET))
            {

                if (receivedPacket.destinationId.Equals(node.id))
                {
                    string storedPacketId = transportLayer.dataPacketTimer.dataPacketId;
                    if (!storedPacketId.Equals("NA"))
                    {
                        hasReceivedAck = false;
                        transportLayer.dataPacketTimer.ReleaseTimer();
                        string buddyName = routeTable.GetNameByIDInRouterTable(receivedPacket.sourceId);

                        MessageBox.Show(buddyName + " agrees to talk with you",
                        "Confirmation Packet", MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
                        messageForm.Invoke(messageForm.showChatWindowDelegate, messageForm);
                    }
                }
                else
                {
                    if (!routeTable.IsDestinationPathEmpty(receivedPacket.destinationId))
                    {
                        Hashtable DestinationInfoList = routeTable.GetDestinationInfoFromRouteTable
                                                                (receivedPacket.destinationId);
                        string destinationIpAddress = routeTable.GetIPAddressByIDInRouterTable
                                                     (DestinationInfoList["NextHop"].ToString());
                        transportLayer.ForwardToNextNeighbour(receivedPacket, destinationIpAddress);

                    }
                    else
                    {
                        receivedPacket.payloadMessage = "LINK BREAK";
                        receivedPacket.packetType = PacketConstants.ROUTE_ERROR_PACKET;
                        string XmlMessageStream = receivedPacket.CreateMessageXmlstringFromPacket();
                        transportLayer.HandleReceivePacket(XmlMessageStream);
                    }
                }
            }

            //3. DATA PACKET
            else if (receivedPacketType.Equals(PacketConstants.DATA_PACKET))
            {
                if (receivedPacket.destinationId.Equals(node.id))
                {
                    string storedPacketId = transportLayer.dataPacketTimer.dataPacketId;

                    if (hasReceivedAck == true)
                    {
                        hasReceivedAck = false;
                        transportLayer.dataPacketTimer.ReleaseTimer();
                    }
                    if (hasSentData == true)
                    {
                        hasSentData = false;
                        transportLayer.dataPacketTimer.ReleaseTimer();
                    }
                    string buddyName = routeTable.GetNameByIDInRouterTable
                                                    (receivedPacket.sourceId);
                    object[] objectItems = new object[3];
                    objectItems[0] = messageForm;
                    objectItems[1] = buddyName;
                    objectItems[2] = receivedPacket.payloadMessage;

                    messageForm.Invoke(messageForm.updateChatWindowDelegate, new object[] { objectItems });

                }
                else
                {
                    //Forward it to destination
                    transportLayer.SendPacket(receivedPacket);
                }
            }

             //4. TERMINATE CHAT
            else if (receivedPacketType.Equals(PacketConstants.TERMINATE_CHAT_PACKET))
            {
                string buddyName = routeTable.GetNameByIDInRouterTable
                                                    (receivedPacket.sourceId);
                if (!transportLayer.dataPacketTimer.dataPacketId.Equals("NA"))
                {
                    transportLayer.dataPacketTimer.ReleaseTimer();
                }
                MessageBox.Show(buddyName + " has terminated the chat",
                "Terminate", MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button1);

                this.ResetAll();
            }
        }
    }
}