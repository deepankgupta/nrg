using System;
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
        private static int broadcastId;
        public  static string buddyId;
        private static Socket udpReceiverSocket;
        private static bool IsUdpReceiverAlive;
        private static Hashtable senderBufferWindow;
        private static Hashtable receivedRouteRequestWindow;
        private static Hashtable receivedRouteReplyWindow;
        private static Hashtable reversePathTable;
        private static Hashtable forwardPathTable;
  
        public AodvProtocolClass()
        {
            SetBroadCastId();
            senderBufferWindow = new Hashtable();
            receivedRouteRequestWindow = new Hashtable();
            reversePathTable = new Hashtable();
            forwardPathTable = new Hashtable();
 
        }
    
 
        public void SetBroadCastId()
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
                string receivedPacketId;

                Packet receivedPacket = Packet.TransformXmlMessageIntoPacket(ReceivedXmlMessageString);
                MessageBox.Show(ReceivedXmlMessageString);

                //Route Request Packet
                if (receivedPacket.packetType == PacketConstants.ROUTE_REQUEST_PACKET)
                {
                    receivedPacketId = receivedPacket.sourceId + receivedPacket.broadcastId.ToString();

                    // Not Already Received
                    if (!receivedRouteRequestWindow.Contains(receivedPacketId))
                    {
                        //Exist Path For Destination
                        if (!RouterTableClass.IsDestinationPathEmpty(receivedPacket.destinationId))
                        {
                            Hashtable DestinationInfoList; 

                            DestinationInfoList = RouterTableClass.GetDestinationInfoFromRouteTable
                                                                    (receivedPacket.destinationId);
                            int currentDestinationSeqNum = Convert.ToInt32(DestinationInfoList[0]);
         
                            if (currentDestinationSeqNum > receivedPacket.destinationSeqNum)
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
                                                    .setHopCount(Convert.ToInt32(DestinationInfoList
                                                                          ["HopCount"].ToString()))
                                                    .setLifeTime(Convert.ToInt32(DestinationInfoList
                                                                          ["LifeTime"].ToString()))
                                                    .build();

                                
                                SendPacket(routeReplyPacket, receivedPacket.currentId);

                            }
                            else  //BroadCast
                            {
                                receivedPacket.hopCount++;
                                reversePathTable.Add(receivedPacket.sourceSeqNum, receivedPacket.currentId);
                                SaveInReceivedRequestBuffer(receivedPacketId,receivedPacket);
                                ForwardPacketToNeighbours(receivedPacket);
                            }

                            //Store ReversePath in RouteTable
                            RouterTableClass.MakeReversePathEntryForNode(receivedPacket.currentId);

                        }
                        else //Path Does not Exist    --->Commented due to LocalHost Testing
                        { 
                        //    //ReBroadCast
                        //    receivedPacket.hopCount++;
                        //    reversePathTable.Add(receivedPacket.sourceSeqNum, receivedPacket.currentId);
                        //    ForwardPacketToNeighbours(receivedPacket);
                        }
                    }
                }

                // Route Request Packet
                else if(receivedPacket.packetType == PacketConstants.ROUTE_REQUEST_PACKET)
                {
                    //SourceEntry Exist 
                    if (reversePathTable.Contains(receivedPacket.sourceSeqNum))
                    {
                        //Hasnt Received RouteReply yet
                        if (!receivedRouteRequestWindow.Contains(receivedPacket.broadcastId))
                        {
                            SaveInReceivedRouteReplyBuffer(receivedPacket.broadcastId, receivedPacket);
                            
                            //Forward RouteReply to Source
                            SendPacket(routeReplyPacket, reversePathTable[receivedPacket.sourceSeqNum].ToString());
                        }
                        else
                        {
                            //TODO
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("SendMessage: An Exception has occured." + ex.ToString());
            }
        }
     

        //Receiver Server Thread
        public static void ReceiveMessageServerThread()
        {
            try
            {
                IsUdpReceiverAlive = true;
                udpReceiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint localIpEndPoint = new IPEndPoint(NetworkClass.IpAddress,NetworkClass.udpPort);
                EndPoint  localEndEntry = (localIpEndPoint);
                udpReceiverSocket.Bind(localEndEntry);
                byte[] saveOverBuffer = new byte[NetworkClass.udpMessageSize];
              
                while (IsUdpReceiverAlive)
                {
                    udpReceiverSocket.ReceiveFrom(saveOverBuffer, ref localEndEntry);
                    string receivedMessage = System.Text.Encoding.ASCII.GetString(saveOverBuffer, 
                                                        0, saveOverBuffer.Length).ToString();
                    HandleReceivePacket(receivedMessage.Substring(0,receivedMessage.IndexOf('\0')));
                }

            }
            catch (SocketException SockExcep)
            {
                MessageBox.Show("Exception is occurred in ReceiveMessageServer() : " + SockExcep.Message);
                //TODO
            }

        }

        public static void ForwardPacketToNeighbours(Packet forwardPacket)
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
                MessageBox.Show("ForwardPacketToNeighbours: An Exception has occured." + ex.ToString());
            }
        }

        /// <summary>
        /// Handles Sending of Different Packets which may 
        /// be a Route Request,Chat Packet(Accept/Reject),
        /// Data Packet and several others
        /// </summary>
        public static void SendPacket(Packet sendPacket,string destinationIpAddress)
        {
            try
            {
                string XmlMessageStream;
                XmlMessageStream = sendPacket.CreateMessageXmlstringFromPacket();
                NetworkClass.sendMessageOverUdp(destinationIpAddress, XmlMessageStream);
             
            }
            catch (Exception ex)
            {
                MessageBox.Show("SendStartChatPacket: An Exception has occured." + ex.ToString());
            }
        }

        //Send BroadCast
        public void SendBroadCastPacket(Packet routeRequestPacket)
        {
            try
            {
                SaveInSenderBuffer(routeRequestPacket);
                ForwardPacketToNeighbours(routeRequestPacket);
            }
            catch (Exception ex)
            {
                MessageBox.Show("SendRouteRequestPacket: An Exception has occured." + ex.ToString());
            }

        }
      
        public void ProcessTextMessage(string textMessage)
        {
            string destinationIpAddress;
            Hashtable DestinationInfoList;
            
            destinationIpAddress = RouterTableClass.GetIPAddressByIDInRouterTable(buddyId);
            DestinationInfoList = RouterTableClass.GetDestinationInfoFromRouteTable(buddyId);
                                //contain Seq Num ,HopCount,LifeTime in Order           
            try
            {

                if (!RouterTableClass.IsDestinationPathEmpty(buddyId))
                {

                    //Start Chat Packet
                    PacketBuilder packetBuilder = new PacketBuilder();
                    Packet startChatPacket =
                                     packetBuilder.setPacketType(PacketConstants.START_CHAT_PACKET)
                                    .setBroadcastId(broadcastId)
                                    .setCurrentId(Node.id)
                                    .setSourceId(Node.id)
                                    .setDestinationId(DestinationInfoList["NextHop"].ToString())
                                    .setSourceSeqNum(Node.sequenceNumber)
                                    .setDestinationSeqNum(Convert.ToInt32(DestinationInfoList
                                                            ["DestinationSequenceNum"].ToString()))
                                    .setPayLoadMessage(textMessage)
                                    .setHopCount(Convert.ToInt32(DestinationInfoList["HopCount"].ToString()))
                                    .setLifeTime(Convert.ToInt32(DestinationInfoList["LifeTime"].ToString()))
                                    .build();

                    SaveInSenderBuffer(startChatPacket);
                    SendPacket(startChatPacket, destinationIpAddress);
                }
                else
                {
                    //Route Request Packet
                    PacketBuilder packetBuilder = new PacketBuilder();
                    Packet routeRequestPacket =
                                     packetBuilder.setPacketType(PacketConstants.ROUTE_REQUEST_PACKET)
                                    .setCurrentId(Node.id)
                                    .setSourceId(Node.id)
                                    .setDestinationId(buddyId)
                                    .setDestinationSeqNum(Convert.ToInt32(DestinationInfoList
                                                            ["DestinationSequenceNum"].ToString()))
                                    .setHopCount(Convert.ToInt32(DestinationInfoList["HopCount"].ToString()))
                                    .setLifeTime(Convert.ToInt32(DestinationInfoList["LifeTime"].ToString()))
                                    .build();


                    SendBroadCastPacket(routeRequestPacket);
                }
                
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
                String packetId = sentPacket.sourceId + broadcastId.ToString();
                if (!senderBufferWindow.ContainsKey(packetId))
                {
                    senderBufferWindow.Add(packetId, sentPacket);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception in SaveInSendBuffer() :" + e.Message);
            }
        }
       
        private static void SaveInReceivedRouteRequestBuffer(string routeRequestPacketId,Packet routeRequestPacket)
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
           
        private static void SaveInReceivedRouteReplyBuffer(string routeReplyPacketId,Packet routeReplyPacket)
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