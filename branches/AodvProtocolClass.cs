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
    /// AODV Protocol Class which helps in finding
    /// Routes to Destination and managing Sending &
    /// Receiving of Packets from various Destinations
    /// </summary>

    public class AodvProtocolClass
    {
        public static string buddyId;
        public static int broadcastId;
        public static bool chatInitiate;
        public static bool chatTerminate;
        public static bool IsChating;
        private static Socket udpReceiverSocket;
        private static bool IsUdpReceiverAlive;
        private static Hashtable senderBufferWindow;
        private static Hashtable receivedRouteRequestWindow;
        private static Hashtable receivedRouteReplyWindow;
        private static Hashtable reversePathTable;
        private static Hashtable forwardPathTable;
        private static MessageApplicationForm messageForm;


        public static void Initialize()
        {
            chatInitiate = false;
            chatTerminate = false;
            IsChating = false;
            senderBufferWindow = new Hashtable();
            receivedRouteRequestWindow = new Hashtable();
            receivedRouteReplyWindow = new Hashtable();
            reversePathTable = new Hashtable();
            forwardPathTable = new Hashtable();
            messageForm = new MessageApplicationForm();
            Application.Run(messageForm);
        }

        public static void SetBroadCastId()
        {
            Random random = new Random();
            broadcastId = random.Next(1, 100000);

        }

        /// <summary>
        /// Handles Different Packets Like RouteReq,
        /// RouteReply,Data Packets etc. and updates 
        /// Route Table also if required.
        /// </summary>
        public static void HandleReceivePacket(string ReceivedXmlMessageString)
        {
            try
            {
                Packet receivedPacket = Packet.TransformXmlMessageIntoPacket(ReceivedXmlMessageString);
                string receivedPacketType = receivedPacket.packetType;
                string receivedPacketId = receivedPacket.sourceId + receivedPacket.broadcastId.ToString();


                //MessageBox.Show(ReceivedXmlMessageString);

                //1. Route Request
                if (receivedPacketType.Equals(PacketConstants.ROUTE_REQUEST_PACKET))
                {
                    // Not Already Received
                    if (!receivedRouteRequestWindow.Contains(receivedPacketId))
                    {
                        //Exist Path For Destination
                        if (!RouterTableClass.IsDestinationPathEmpty(receivedPacket.destinationId))
                        {
                            Hashtable DestinationInfoList;

                            DestinationInfoList = RouterTableClass.GetDestinationInfoFromRouteTable
                                                                    (receivedPacket.destinationId);
                            int savedDestinationSeqNum = Convert.ToInt32(DestinationInfoList
                                                                    ["DestinationSequenceNum"].ToString());


                            if (savedDestinationSeqNum >= receivedPacket.destinationSeqNum)
                            {
                                //Send RouteReply
                                PacketBuilder packetBuilder = new PacketBuilder();
                                Packet routeReplyPacket =
                                                     packetBuilder.setPacketType(PacketConstants.ROUTE_REPLY_PACKET)
                                                    .setBroadcastId(receivedPacket.broadcastId)
                                                    .setCurrentId(Node.id)
                                                    .setSourceId(receivedPacket.sourceId)
                                                    .setDestinationId(receivedPacket.destinationId)
                                                    .setSourceSeqNum(receivedPacket.sourceSeqNum)
                                                    .setDestinationSeqNum(Convert.ToInt32(DestinationInfoList
                                                                          ["DestinationSequenceNum"].ToString()))
                                                    .build();

                                string forwardIpAddress = RouterTableClass.GetIPAddressByIDInRouterTable
                                                                        (receivedPacket.currentId);
                                ForwardToNextNeighbour(routeReplyPacket, forwardIpAddress);

                            }
                            else  //BroadCast
                            {
                                receivedPacket.hopCount++;
                                receivedPacket.currentId = Node.id;
                                SaveInReceivedRouteRequestBuffer(receivedPacketId, receivedPacket);
                                SendBroadCastPacket(receivedPacket);
                            }
                        }
                        else //Path Does not Exist 
                        {
                            //ReBroadCast
                            receivedPacket.hopCount++;
                            receivedPacket.currentId = Node.id;
                            SaveInReceivedRouteRequestBuffer(receivedPacketId, receivedPacket);
                            SendBroadCastPacket(receivedPacket);
                        }

                        //StoreReversePath  & Update Route Table  
                        reversePathTable.Add(receivedPacket.sourceSeqNum, receivedPacket.currentId);

                    }
                    else //Already Received Request
                    {
                        Packet storedRouteRequestPacket = FindStoredPacketinRouteRequestBuffer(receivedPacketId);
                        if (storedRouteRequestPacket.hopCount > receivedPacket.hopCount)
                        {
                            receivedRouteRequestWindow.Remove(receivedPacketId);
                            SaveInReceivedRouteRequestBuffer(receivedPacketId, receivedPacket);
                            reversePathTable.Add(receivedPacket.sourceSeqNum, receivedPacket.currentId);
                        }
                    }


                    Hashtable InitiatorInfoTable = new Hashtable();
                    InitiatorInfoTable.Add("NextHop", receivedPacket.currentId);
                    InitiatorInfoTable.Add("HopCount", receivedPacket.hopCount + 1);
                    InitiatorInfoTable.Add("DestinationID", receivedPacket.sourceId);
                    InitiatorInfoTable.Add("DestinationSequenceNum", receivedPacket.sourceSeqNum);

                    //Update RouteTable
                    RouterTableClass.MakePathEntryForNode(receivedPacket.currentId, InitiatorInfoTable);
                }

                //2. Route Reply
                else if (receivedPacketType.Equals(PacketConstants.ROUTE_REPLY_PACKET))
                {

                    int keyReversePath = receivedPacket.sourceSeqNum;

                    //Source Node Receives
                    if (receivedPacket.sourceId.Equals(Node.id))
                    {
                        Packet storedSentPacket = FindStoredPacketinSenderBuffer(receivedPacketId);

                        //Forward stored Sent Messages
                        storedSentPacket.destinationSeqNum = receivedPacket.destinationSeqNum;
                        string forwardIpAddress = RouterTableClass.GetIPAddressByIDInRouterTable
                                                              (receivedPacket.currentId);
                        ForwardToNextNeighbour(storedSentPacket, forwardIpAddress);

                    }
                    else if (reversePathTable.Contains(keyReversePath))  //SourceEntry Exist 
                    {
                        receivedPacket.currentId = Node.id;
                        string reverseIpAddress = RouterTableClass.GetIPAddressByIDInRouterTable
                                                    (reversePathTable[keyReversePath].ToString());
                        receivedPacket.hopCount++;

                        //Hasnt Received RouteReply yet
                        if (!receivedRouteReplyWindow.Contains(receivedPacket.broadcastId))
                        {
                            receivedRouteReplyWindow.Remove(receivedPacketId);
                            SaveInReceivedRouteReplyBuffer(receivedPacketId, receivedPacket);
                            ForwardToNextNeighbour(receivedPacket, reverseIpAddress);
                        }
                        else //Has Already Received
                        {
                            Packet storedRouteReplyPacket = FindStoredPacketinRouteReply(receivedPacketId);

                            if (storedRouteReplyPacket.destinationSeqNum < receivedPacket.destinationSeqNum ||
                                                    storedRouteReplyPacket.hopCount > receivedPacket.hopCount)
                            {
                                receivedRouteReplyWindow.Remove(receivedPacketId);
                                SaveInReceivedRouteReplyBuffer(receivedPacketId, receivedPacket);
                                ForwardToNextNeighbour(receivedPacket, reverseIpAddress);
                            }
                        }
                    }
                    else
                    {
                        //DO Nothing    SourceEntry doesnot exist
                    }

                    Hashtable InitiatorInfoTable = new Hashtable();
                    InitiatorInfoTable.Add("NextHop", receivedPacket.currentId);
                    InitiatorInfoTable.Add("HopCount", receivedPacket.hopCount + 1);
                    InitiatorInfoTable.Add("DestinationID", receivedPacket.destinationId);
                    InitiatorInfoTable.Add("DestinationSequenceNum", receivedPacket.sourceSeqNum);

                    //Update RouteTable
                    RouterTableClass.MakePathEntryForNode(receivedPacket.currentId, InitiatorInfoTable);

                }

                //3. START CHAT 
                else if (receivedPacketType.Equals(PacketConstants.START_CHAT_PACKET))
                {

                    //Destination Node Receives
                    if (receivedPacket.destinationId.Equals(Node.id))
                    {
                        string buddyName = RouterTableClass.GetNameByIDInRouterTable(receivedPacket.sourceId);
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
                            SendPacket(acceptChatPacket);

                        }
                    }
                    else
                    {
                        //Forward it to destination
                        SendPacket(receivedPacket);
                    }
                }

                // 4.Accept START CHAT 
                else if (receivedPacketType.Equals(PacketConstants.ACCEPT_START_CHAT_PACKET))
                {
                    if (chatInitiate == true)
                        if (receivedPacket.destinationId.Equals(Node.id))
                        {
                            if (chatInitiate == true)
                            {
                                string buddyName = RouterTableClass.GetNameByIDInRouterTable(receivedPacket.sourceId);

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
                            //Forward it to destination
                            SendPacket(receivedPacket);
                        }
                }

                    //5. DATA PACKET
                else if (receivedPacketType.Equals(PacketConstants.DATA_PACKET))
                {
                    if (receivedPacket.destinationId.Equals(Node.id))
                    {
                        if (receivedPacket.sourceId.Equals(buddyId))
                        {
                            string buddyName = RouterTableClass.GetNameByIDInRouterTable
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
                        SendPacket(receivedPacket);
                    }
                }

                //6. TERMINATE CHAT
                else if (receivedPacketType.Equals(PacketConstants.TERMINATE_CHAT_PACKET))
                {
                    string buddyName = RouterTableClass.GetNameByIDInRouterTable
                                                        (receivedPacket.sourceId);

                    MessageBox.Show(buddyName + " has terminated the chat",
                    "Terminate", MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button1);

                    ResetChatInformation();
                    messageForm.Invoke(messageForm.resetFormControls, messageForm);
                    messageForm.Invoke(messageForm.hideChatWindowDelegate, messageForm);
                }

                //6. Route Error 
                else if (receivedPacketType.Equals(PacketConstants.ROUTE_ERROR_PACKET))
                {
                    //TODO
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("HandleReceivePacket: An Exception has occured." + ex.ToString());
            }
        }

        //Receiver Server Thread
        public static void ReceiveMessageServerThread()
        {
            try
            {
                IsUdpReceiverAlive = true;
                udpReceiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint localIpEndPoint = new IPEndPoint(NetworkClass.IpAddress, NetworkClass.udpPort);
                EndPoint localEndEntry = (localIpEndPoint);
                udpReceiverSocket.Bind(localEndEntry);

                while (IsUdpReceiverAlive)
                {
                    byte[] saveOverBuffer = new byte[NetworkClass.udpMessageSize];
                    udpReceiverSocket.ReceiveFrom(saveOverBuffer, ref localEndEntry);
                    string receivedMessage = System.Text.Encoding.ASCII.GetString(saveOverBuffer,
                                                        0, saveOverBuffer.Length).ToString();
                    HandleReceivePacket(receivedMessage.Substring(0, receivedMessage.IndexOf('\0')));
                }

            }
            catch (SocketException SockExcep)
            {
                MessageBox.Show("Exception is occurred in ReceiveMessageServer() : " + SockExcep.Message);
                //TODO
            }

        }

        public static void ForwardToNextNeighbour(Packet forwardPacket, string destinationIpAddress)
        {
            try
            {
                string XmlMessageStream = forwardPacket.CreateMessageXmlstringFromPacket();
                NetworkClass.sendMessageOverUdp(destinationIpAddress, XmlMessageStream);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ForwardToNextNeighbour: An Exception has occured." + ex.ToString());
            }
        }

        /// <summary>
        /// Handles Sending of Different Packets which may 
        /// be a Route Request,Chat Packet(Accept/Reject),
        /// Data Packet and several others
        /// </summary>
        public static void SendPacket(Packet sendPacket)
        {
            try
            {
                string XmlMessageStream = sendPacket.CreateMessageXmlstringFromPacket();
                string destinationIpAddress = RouterTableClass.GetIPAddressByIDInRouterTable
                                                                (sendPacket.destinationId);
                if (!RouterTableClass.IsDestinationPathEmpty(sendPacket.destinationId))
                {
                    NetworkClass.sendMessageOverUdp(destinationIpAddress, XmlMessageStream);
                }
                else
                {
                    //Route Request Packet
                    PacketBuilder packetBuilder = new PacketBuilder();
                    Packet routeRequestPacket =
                                     packetBuilder.setPacketType(PacketConstants.ROUTE_REQUEST_PACKET)
                                    .setBroadcastId(sendPacket.broadcastId)
                                    .setCurrentId(sendPacket.currentId)
                                    .setSourceId(sendPacket.sourceId)
                                    .setDestinationId(sendPacket.destinationId)
                                    .setSourceSeqNum(sendPacket.sourceSeqNum)
                                    .setDestinationSeqNum(sendPacket.destinationSeqNum)
                                    .build();


                    SendBroadCastPacket(routeRequestPacket);
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("SendPacket: An Exception has occured." + ex.ToString());
            }
        }

        public static void SendBroadCastPacket(Packet forwardPacket)
        {
            try
            {
                ArrayList neighbourNodesList = RouterTableClass.GetNeighbourNodes(Node.id);
                string XmlMessageStream = forwardPacket.CreateMessageXmlstringFromPacket();

                foreach (string nodeId in neighbourNodesList)
                {
                    if (!nodeId.Equals(forwardPacket.sourceId))
                    {
                        string neighbourIpAddress = RouterTableClass.GetIPAddressByIDInRouterTable(nodeId);
                        NetworkClass.sendMessageOverUdp(neighbourIpAddress, XmlMessageStream);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("SendBroadCastPacket: An Exception has occured." + ex.ToString());
            }

        }

        public static void ResetChatInformation()
        {
            chatInitiate = false;
            chatTerminate = false;
            IsChating = false;
        }


        public static void ProcessTextMessage(string textMessage)
        {

            try
            {
                Packet dataPacket = new Packet();
                SetBroadCastId();
                Hashtable DestinationInfoList = RouterTableClass.GetDestinationInfoFromRouteTable(buddyId);

                PacketBuilder packetBuilder = new PacketBuilder();

                packetBuilder = packetBuilder.setBroadcastId(broadcastId)
                                  .setCurrentId(Node.id)
                                  .setSourceId(Node.id)
                                  .setDestinationId(buddyId)
                                  .setSourceSeqNum(Node.sequenceNumber)
                                  .setDestinationSeqNum(Convert.ToInt32(DestinationInfoList
                                                            ["DestinationSequenceNum"].ToString()));



                //Start Chat Packet
                if (chatInitiate == true)
                {
                    dataPacket = packetBuilder.setPacketType(PacketConstants.START_CHAT_PACKET)
                                  .build();
                }
                if (chatTerminate == true)
                {
                    dataPacket = packetBuilder.setPacketType(PacketConstants.TERMINATE_CHAT_PACKET)
                                  .build();

                    messageForm.Invoke(messageForm.resetFormControls, messageForm);
                    ResetChatInformation();
                }
                if (IsChating == true)
                {
                    dataPacket = packetBuilder.setPacketType(PacketConstants.DATA_PACKET)
                                  .setPayLoadMessage(textMessage)
                                  .build();

                    object[] objectItems = new object[3];
                    objectItems[0] = messageForm;
                    objectItems[1] = Node.name;
                    objectItems[2] = textMessage;

                    messageForm.Invoke(messageForm.updateChatWindowDelegate, new object[] { objectItems });

                }

                SaveInSenderBuffer(dataPacket);
                SendPacket(dataPacket);

            }
            catch (Exception ex)
            {
                MessageBox.Show("SendMessage: An Exception has occured." + ex.ToString());
            }
        }

        private static void SaveInSenderBuffer(Packet sentPacket)
        {
            try
            {
                String packetId = sentPacket.sourceId + sentPacket.broadcastId.ToString();
                senderBufferWindow.Add(packetId, sentPacket);

            }
            catch (Exception e)
            {
                MessageBox.Show("Exception in SaveInSendBuffer() :" + e.Message);
            }
        }

        private static Packet FindStoredPacketinSenderBuffer(string packetId)
        {
            Packet packet = new Packet();
            try
            {
                IDictionaryEnumerator ide = senderBufferWindow.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (ide.Key.ToString().Equals(packetId))
                        packet = (Packet)ide.Value;

                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception in FindStoredPacketinSenderBuffer() :" + e.Message);
            }
            return packet;
        }

        private static Packet FindStoredPacketinRouteRequestBuffer(string packetId)
        {
            Packet packet = new Packet();
            try
            {
                IDictionaryEnumerator ide = senderBufferWindow.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (ide.Key.ToString().Equals(packetId))
                        packet = (Packet)ide.Value;

                }
                return packet;
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception in FindStoredPacketinRouteRequestBuffer() :" + e.Message);
            }
            return packet;
        }

        private static Packet FindStoredPacketinRouteReply(string packetId)
        {
            Packet packet = new Packet();
            try
            {
                IDictionaryEnumerator ide = senderBufferWindow.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (ide.Key.ToString().Equals(packetId))
                        packet = (Packet)ide.Value;

                }
                return packet;
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception in FindStoredPacketinRouteReply() :" + e.Message);
            }
            return packet;
        }

        private static void SaveInReceivedRouteRequestBuffer(string routeRequestPacketId, Packet routeRequestPacket)
        {
            try
            {
                receivedRouteRequestWindow.Add(routeRequestPacketId, routeRequestPacket);
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception in SaveInReceivedRouteRequestBuffer() :" + e.Message);
            }
        }

        private static void SaveInReceivedRouteReplyBuffer(string routeReplyPacketId, Packet routeReplyPacket)
        {
            try
            {
                receivedRouteReplyWindow.Add(routeReplyPacketId, routeReplyPacket);
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception in SaveInReceivedRouteReplyBuffer() :" + e.Message);
            }
        }
    }

}