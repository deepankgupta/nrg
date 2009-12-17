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
        private int sourceSeqNum;
        private string requestId;
        private  string buddyId;
        private string sessionId;
        private static Socket udpReceiverSocket;
        public static bool IsUdpReceiverAlive;
        private Hashtable senderBufferWindow;
        NetworkClass networkHelpingObject;


        public AodvProtocolClass(string buddyIdText)
        {
           
            buddyId = buddyIdText;
            requestId = "";
            sourceSeqNum = 0;
            networkHelpingObject = new NetworkClass();
            senderBufferWindow = new Hashtable();
            SetSessionId();

        }
     
        public void SetSessionId()
        {
            Random random = new Random();
            int randomSessonNum = random.Next(0, 100000);
            sessionId = randomSessonNum + ":" + Node.Id + ":" + buddyId;
        }

        //RequestID for Packet
        public void SetRequestIdForPacket(string packetType)
        {

            if (packetType.Equals(PacketConstants.START_CHAT_PACKET))
            {
                requestId = sessionId + ":" + "0";
            }
            else if (packetType.Equals(PacketConstants.DATA_PACKET))
            {
                sourceSeqNum++;
                requestId = sessionId + ":" + sourceSeqNum;
            }
  
        }

        //Handle Received Packet
        public static void HandleReceivePacket(string ReceivedXmlMessageString)
        {
            Packet receivedPacket = new Packet();
            receivedPacket = Packet.TransformXmlMessageIntoPacket(ReceivedXmlMessageString);

            if (receivedPacket.packetType == PacketConstants.START_CHAT_PACKET)
            {

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
                    string receivedMessage = System.Text.Encoding.ASCII.GetString(saveOverBuffer, 0, saveOverBuffer.Length);
                    HandleReceivePacket(receivedMessage);
                    //MessageBox.Show(receivedMessage);
                }

            }
            catch (SocketException SockExcep)
            {
                MessageBox.Show("Exception is occurred in ReceiveMessageServer() : " + SockExcep.Message);
                //TODO
            }

        }

        // Send Message Packet
        public void SendMessage(string packetPayLoad)
        {
            try
            {
                int destinationSeqNum;
                int hopCount;
                int lifeTime;
                string payLoadXmlMessage;
                string destinationIpAddr;
                ArrayList neighbourNodesList;
                ArrayList destinationInfoList;

                if (!RouterTableClass.IsDestinationPathEmpty(buddyId))
                {
                    Packet sendPacket = new Packet();

                    SetRequestIdForPacket(PacketConstants.START_CHAT_PACKET);
                    destinationIpAddr = UtilityConfFile.GetIPAddressByIDInConfFile(buddyId);
                    destinationInfoList=RouterTableClass.GetDestinationInfoFromRouteTable(buddyId);
                    destinationSeqNum =Convert.ToInt32( destinationInfoList[0]);
                    hopCount = Convert.ToInt32(destinationInfoList[1]);
                    lifeTime = Convert.ToInt32(destinationInfoList[2]);

                    //set Packet Attributes
                    sendPacket.SetPacketInfo(PacketConstants.START_CHAT_PACKET, requestId,
                                            NetworkClass.IpAddress.ToString(), destinationIpAddr
                                            , sourceSeqNum, destinationSeqNum, packetPayLoad, hopCount
                                            , lifeTime);

                    //Save Packet In Sender Buffer
                    SaveInSenderBuffer(sendPacket);

                    //Convert Packet to Xml message stream
                    payLoadXmlMessage = sendPacket.CreateMessageXmlstringFromPacket();

                    //send Over Udp
                    networkHelpingObject.sendMessageOverUdp(destinationIpAddr, payLoadXmlMessage);

                }
                else
                {
                    neighbourNodesList = RouterTableClass.GetNeighbourNodes(Node.Id);
                 
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("FindIDbyIPAddress: An Exception has occured." + ex.ToString());
            }
        }

        //Save in Sender Buffer
        private void SaveInSenderBuffer(Packet packet)
        {
            try
            {
                if (!senderBufferWindow.ContainsKey(packet.packetId))
                {
                    senderBufferWindow.Add(packet.packetId, packet);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception in SaveInSendBuffer() :" + e.Message);
            }
        }
    }
}