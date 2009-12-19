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
        private static Hashtable receiverBufferWindow;
        private static PacketBuilder packetBuilder;


        public AodvProtocolClass()
        {
            SetBroadCastId();
            senderBufferWindow = new Hashtable();
            receiverBufferWindow = new Hashtable();
            packetBuilder = new PacketBuilder();
 
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

                //Route Request Packet Received
                if (receivedPacket.packetType == PacketConstants.ROUTE_REQUEST_PACKET)
                {
                    receivedPacketId = receivedPacket.sourceId + receivedPacket.broadcastId.ToString();

                    // Not Already Received
                    if (!receiverBufferWindow.Contains(receivedPacketId))
                    {
                        //Exist Path For Destination
                        if (!RouterTableClass.IsDestinationPathEmpty(receivedPacket.destinationId))
                        {
                            ArrayList DestinationInfoList; //contains destination Seq Num,HopCount,LifeTime in Order

                            DestinationInfoList = RouterTableClass.GetDestinationInfoFromRouteTable
                                                                    (receivedPacket.destinationId);
                            int currentDestinationSeqNum = Convert.ToInt32(DestinationInfoList[0]);

                            if (currentDestinationSeqNum > receivedPacket.destinationSeqNum)
                            {
                                //Send RouteReply
                                Packet routeReplyPacket =
                                                     packetBuilder.setPacketType(PacketConstants.ROUTE_REPLY_PACKET)
                                                    .setCurrentId(Node.id)
                                                    .setSourceId(Node.id)
                                                    .setDestinationId(buddyId)
                                                    .setSourceSeqNum(Convert.ToInt32(DestinationInfoList[0]))
                                                    .setDestinationSeqNum(Convert.ToInt32(DestinationInfoList[1]))
                                                    .setHopCount(Convert.ToInt32(DestinationInfoList[1]))
                                                    .setLifeTime(Convert.ToInt32(DestinationInfoList[2]))
                                                    .build();

                                SendPacket(routeReplyPacket, receivedPacket.currentId);

                            }
                            else  //Forward to Neighbours
                            {
                                receivedPacket.hopCount++;
                                ForwardPacketToNeighbours(receivedPacket);
                            }

                            //Store ReversePath in RouteTable
                            RouterTableClass.MakeReversePathEntryForNode(receivedPacket.currentId);

                        }
                        else //Path Does not Exist
                        {
                            //TODO
                        }
                    }
                    else //Already has Received Packet
                    {
                        //Do Nothing
                    }
                }
                else //Handle Other Packets
                {
                    //TODO
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
                    string neighbourIpAddress = RouterTableClass.GetIPAddressByIDInRouterTable(nodeId);
                    NetworkClass.sendMessageOverUdp(neighbourIpAddress, XmlMessageStream);
                    //    networkHelpingObject.sendMessageOverUdp(neighbourIpAddress, XmlMessageStream);
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
                SaveInSenderBuffer(sendPacket);
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
            ArrayList DestinationInfoList;
            
            destinationIpAddress = RouterTableClass.GetIPAddressByIDInRouterTable(buddyId);
            DestinationInfoList = RouterTableClass.GetDestinationInfoFromRouteTable(buddyId);
                                //contain Seq Num ,HopCount,LifeTime in Order           

            try
            {

                if (!RouterTableClass.IsDestinationPathEmpty(buddyId))
                {
   
                    //Start Chat Packet
                    Packet startChatPacket =
                                     packetBuilder.setPacketType(PacketConstants.START_CHAT_PACKET)
                                    .setBroadcastId(broadcastId)
                                    .setCurrentId(Node.id)
                                    .setSourceId(Node.id)
                                    .setDestinationId(buddyId)
                                    .setSourceSeqNum(Convert.ToInt32(DestinationInfoList[0]))
                                    .setDestinationSeqNum(Convert.ToInt32(DestinationInfoList[1]))
                                    .setPayLoadMessage(textMessage)
                                    .setHopCount(Convert.ToInt32(DestinationInfoList[1]))
                                    .setLifeTime(Convert.ToInt32(DestinationInfoList[2]))
                                    .build();

                    SendPacket(startChatPacket, destinationIpAddress);
                }
                else
                {
                    //Route Request Packet
                    Packet routeRequestPacket =
                                     packetBuilder.setPacketType(PacketConstants.ROUTE_REQUEST_PACKET)
                                    .setCurrentId(Node.id)
                                    .setSourceId(Node.id)
                                    .setDestinationId(buddyId)
                                    .setSourceSeqNum(Convert.ToInt32(DestinationInfoList[0]))
                                    .setDestinationSeqNum(Convert.ToInt32(DestinationInfoList[1]))
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
       
        private static void SaveInReceiverBuffer(string receivedPacketId,Packet receivedPacket)
        {
            try
            {
                receiverBufferWindow.Add(receivedPacketId, receivedPacket);
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception in SaveInReceiverBuffer() :" + e.Message);
            }
        }


    }
}