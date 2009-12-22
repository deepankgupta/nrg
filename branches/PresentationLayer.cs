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
        public bool chatInitiate;
        public bool chatTerminate;
        public bool isChating;
        private int broadcastId;
        private Node node;
        private RouterTableClass routeTable;
        private TransportLayer transportLayer;
        private MessageApplicationForm messageForm;
        private static volatile PresentationLayer instance;
        private static object syncRoot = new Object();

        private PresentationLayer()
        {
            chatInitiate = false;
            chatTerminate = false;
            isChating = false;
            node = Node.nodeInstance;
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
            chatInitiate = false;
            chatTerminate = false;
            isChating = false;
        }

        public void PerformSenderAction(string optionSelected)
        {
            Packet dataPacket = new Packet();
            SetBroadCastId();
            Hashtable destinationInfoList = routeTable.GetDestinationInfoFromRouteTable(buddyId);
            PacketBuilder packetBuilder = new PacketBuilder();
            transportLayer = TransportLayer.transportLayerInstance;
            messageForm = MessageApplicationForm.messageFormInstance;

            packetBuilder = packetBuilder.setBroadcastId(broadcastId)
                              .setCurrentId(Node.id)
                              .setSourceId(Node.id)
                              .setDestinationId(buddyId)
                              .setSourceSeqNum(Node.sequenceNumber)
                              .setDestinationSeqNum(Convert.ToInt32(destinationInfoList
                                                        ["DestinationSequenceNum"].ToString()));


            if (optionSelected.Equals("CHAT"))
            {
                chatInitiate = true;
                dataPacket = packetBuilder.setPacketType(PacketConstants.START_CHAT_PACKET)
                              .build();
            }

            if (optionSelected.Equals("CLOSE"))
            {
                dataPacket = packetBuilder.setPacketType(PacketConstants.TERMINATE_CHAT_PACKET)
                             .build();
                this.ResetAll();
                messageForm.Invoke(messageForm.hideChatWindowDelegate, messageForm);
                messageForm.Invoke(messageForm.resetFormControls, messageForm);

            }

            if (optionSelected.Equals("SEND"))
            {
                isChating = true;
                dataPacket = packetBuilder.setPacketType(PacketConstants.DATA_PACKET)
                               .setPayLoadMessage(messageForm.MessageTextBox.Text)
                               .build();

                object[] objectItems = new object[3];
                objectItems[0] = messageForm;
                objectItems[1] = Node.name;
                objectItems[2] = messageForm.MessageTextBox.Text;

                messageForm.Invoke(messageForm.updateChatWindowDelegate, new object[] { objectItems });


            }

            transportLayer.SendPacket(dataPacket);
        }

        public void HandleReceivedDataPackets(Packet receivedPacket)
        {
            string receivedPacketType = receivedPacket.packetType;
            string receivedPacketId = receivedPacket.sourceId + receivedPacket.broadcastId.ToString();
          
            //3. START MESSAGE
            if (receivedPacketType.Equals(PacketConstants.START_CHAT_PACKET))
            {
                transportLayer = TransportLayer.transportLayerInstance;
                messageForm = MessageApplicationForm.messageFormInstance;

                //Destination Node Receives
                if (receivedPacket.destinationId.Equals(Node.id))
                {
                    string buddyName = routeTable.GetNameByIDInRouterTable(receivedPacket.sourceId);
                    string acceptChat = MessageBox.Show(buddyName + " wants to chat with you! ",
                                                "Start Chat Packet", MessageBoxButtons.YesNo,
                                                MessageBoxIcon.Question, MessageBoxDefaultButton.Button1).ToString();
                    if (acceptChat.Equals("Yes"))
                    {

                        chatInitiate = false;
                        SetBroadCastId();
                        buddyId = receivedPacket.sourceId;

                        PacketBuilder packetBuilder = new PacketBuilder();
                        Packet acceptChatPacket =
                                       packetBuilder.setPacketType(PacketConstants.ACCEPT_START_CHAT_PACKET)
                                      .setBroadcastId(broadcastId)
                                      .setCurrentId(Node.id)
                                      .setSourceId(receivedPacket.destinationId)
                                      .setDestinationId(receivedPacket.sourceId)
                                      .setSourceSeqNum(receivedPacket.destinationSeqNum)
                                      .setDestinationSeqNum(receivedPacket.sourceSeqNum)
                                      .build();
                        transportLayer.SendPacket(acceptChatPacket);

                        messageForm.Invoke(messageForm.showChatWindowDelegate, messageForm);

                    }
                }
                else
                {
                    transportLayer.SendPacket(receivedPacket);
                }
            }

            // 4.Accept START CHAT 
            else if (receivedPacketType.Equals(PacketConstants.ACCEPT_START_CHAT_PACKET))
            {

                if (receivedPacket.destinationId.Equals(Node.id))
                {
                    if (chatInitiate == true)
                    {
                        string buddyName = routeTable.GetNameByIDInRouterTable(receivedPacket.sourceId);

                        MessageBox.Show(buddyName + " agrees to talk with you",
                        "Confirmation Packet", MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);

                        chatInitiate = false;
                        messageForm.Invoke(messageForm.showChatWindowDelegate, messageForm);


                    }
                    else
                    {
                        //TODO  ERROR 
                    }
                }
                else
                {
                    transportLayer.SendPacket(receivedPacket);
                }
            }

            //5. DATA PACKET
            else if (receivedPacketType.Equals(PacketConstants.DATA_PACKET))
            {
                if (receivedPacket.destinationId.Equals(Node.id))
                {
                    if (receivedPacket.sourceId.Equals(buddyId))
                    {
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
                        //TODO ERROR

                    }
                }
                else
                {
                    //Forward it to destination
                    transportLayer.SendPacket(receivedPacket);
                }
            }

             //6. TERMINATE CHAT
            else if (receivedPacketType.Equals(PacketConstants.TERMINATE_CHAT_PACKET))
            {
                string buddyName = routeTable.GetNameByIDInRouterTable
                                                    (receivedPacket.sourceId);

                MessageBox.Show(buddyName + " has terminated the chat",
                "Terminate", MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button1);

                this.ResetAll();
                messageForm.Invoke(messageForm.hideChatWindowDelegate, messageForm);
                messageForm.Invoke(messageForm.resetFormControls, messageForm);
            }
        }
    }
}