using System;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Net;
using System.Data;
using System.Collections;

namespace SmartDeviceApplication
{
    public class AodvProtocolClass
    {
        private int broadcastId;
        private string buddyId;
        private static Socket udpReceiverSocket;
        private static bool IsUdpReceiverAlive;
        private Hashtable senderBufferWindow;
        private static Hashtable receiverBufferWindow;


        public AodvProtocolClass(string buddyIdText)
        {
           
            buddyId = buddyIdText;
            SetBroadCastId();
            senderBufferWindow = new Hashtable();
            receiverBufferWindow = new Hashtable();
 
        }
    
 
        public void SetBroadCastId()
        {
            Random random = new Random();
            broadcastId = random.Next(1, 100000);
        }


        //Handle Received Packet
        public static void HandleReceivePacket(string ReceivedXmlMessageString)
        {
            try
            {
                string receivedPacketId;
                ArrayList DestinationInfoList; //contains destination Seq Num,HopCount,LifeTime in Order

                Packet receivedPacket = new Packet();
                receivedPacket = Packet.TransformXmlMessageIntoPacket(ReceivedXmlMessageString);
                MessageBox.Show(ReceivedXmlMessageString);

                //Route Request Packet
                if (receivedPacket.packetType == PacketConstants.ROUTE_REQUEST_PACKET)
                {
                    receivedPacketId = receivedPacket.sourceId + receivedPacket.broadcastId.ToString();

                    // Not Already Received
                    if (!receiverBufferWindow.Contains(receivedPacketId))
                    {
                        //Exist Path For Destination
                        if (!RouterTableClass.IsDestinationPathEmpty(receivedPacket.destinationId))
                        {
                            DestinationInfoList = RouterTableClass.GetDestinationInfoFromRouteTable
                                                                    (receivedPacket.destinationId);

                            int currentDestinationSeqNum = Convert.ToInt32(DestinationInfoList[0]);

                            if (currentDestinationSeqNum > receivedPacket.destinationSeqNum)
                            {

                            }
                            else  //Forward to Neighbours
                            {
                                //Increment Hop Count
                                receivedPacket.hopCount++;
                                ForwardPacketToNeighbours(receivedPacket);
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("SendMessage: An Exception has occured." + ex.ToString());
            }
        }

        //Receiver Thread
        public static void ReceiveMessageServer()
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
                    string receivedMessage = System.Text.Encoding.ASCII.GetString(saveOverBuffer, 0, saveOverBuffer.Length).ToString();
                    HandleReceivePacket(receivedMessage.Substring(0,receivedMessage.IndexOf('\0')));
                }

            }
            catch (SocketException SockExcep)
            {
                MessageBox.Show("Exception is occurred in ReceiveMessageServer() : " + SockExcep.Message);
                //TODO
            }

        }

        //Forward Packet to Neighbours
        public static void ForwardPacketToNeighbours(Packet forwardPacket)
        {
            try
            {
                ArrayList neighbourNodesList = RouterTableClass.GetNeighbourNodes(Node.Id);
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

        //Send ChatPacket
        public void SendStartChatPacket(Packet chatPacket,string destinationIpAddress)
        {
            try
            {
                string XmlMessageStream;

                //Save Packet In Sender Buffer
                SaveInSenderBuffer(chatPacket);

                //Convert Packet to Xml message stream
                XmlMessageStream = chatPacket.CreateMessageXmlstringFromPacket();

                //send Packet
                NetworkClass.sendMessageOverUdp(destinationIpAddress, XmlMessageStream);
                //    networkHelpingObject.sendMessageOverUdp(neighbourIpAddress, XmlMessageStream);
            }
            catch (Exception ex)
            {
                MessageBox.Show("SendStartChatPacket: An Exception has occured." + ex.ToString());
            }
        }

        //Send RouteRequest
        public void SendRouteRequestPacket(Packet routeRequestPacket, string destinationIpAddress)
        {
            try
            {
                //Save Packet In Sender Buffer
                SaveInSenderBuffer(routeRequestPacket);

                //Forward To Neighbours
                ForwardPacketToNeighbours(routeRequestPacket);
            }
            catch (Exception ex)
            {
                MessageBox.Show("SendRouteRequestPacket: An Exception has occured." + ex.ToString());
            }

        }

        // Send Message Packet
        public void SendMessage(string textMessage)
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
                    Packet startChatPacket = new Packet();

                    //Start Chat Packet 
                    startChatPacket.SetPacketInfo(PacketConstants.START_CHAT_PACKET, broadcastId,
                                            Node.Id, buddyId, Node.sequenceNumber, Convert.ToInt32
                                           (DestinationInfoList[0]), textMessage, Convert.ToInt32
                                           (DestinationInfoList[1]), Convert.ToInt32(DestinationInfoList[2]));

                    SendStartChatPacket(startChatPacket, destinationIpAddress);
                }
                else
                {
                    Packet routeRequestPacket = new Packet();

                    //Route Request Packet
                    routeRequestPacket.SetPacketInfo(PacketConstants.ROUTE_REQUEST_PACKET, broadcastId,
                                            Node.Id, buddyId, PacketConstants.EmptyInt, Convert.ToInt32
                                            (DestinationInfoList[0]), PacketConstants.EmptyString,
                                            PacketConstants.EmptyInt, PacketConstants.EmptyInt);

                    SendRouteRequestPacket(routeRequestPacket, destinationIpAddress);
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("SendMessage: An Exception has occured." + ex.ToString());
            }
        }

        //Save in Sender Buffer
        private void SaveInSenderBuffer(Packet sentPacket)
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
       
        //Save in Receiver Buffer
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