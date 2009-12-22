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
    /// Transport Layer which support Routing
    /// Protocol to be followed to send and 
    /// receive data from the Presentation Layer 
    /// to the destination and viceversa
    /// </summary>
    public class TransportLayer
    {
        private Node node;
        private NetworkLayer networkLayer;
        private RouterTableClass routeTable;
        private PresentationLayer presentationLayer;
        private Hashtable senderBufferWindow;
        private Hashtable receivedRouteRequestWindow;
        private Hashtable receivedRouteReplyWindow;
        private Hashtable reversePathTable;
        private Hashtable forwardPathTable;
        private static volatile TransportLayer instance;
        private static object syncRoot = new Object();

        public static TransportLayer transportLayerInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new TransportLayer();
                    }
                }
                return instance;
            }
        }


        private TransportLayer()
        {
            senderBufferWindow = new Hashtable();
            receivedRouteRequestWindow = new Hashtable();
            receivedRouteReplyWindow = new Hashtable();
            reversePathTable = new Hashtable();
            forwardPathTable = new Hashtable();
            node = Node.nodeInstance;
            routeTable = RouterTableClass.routeTableInstance;
            networkLayer = NetworkLayer.networkLayerInstance;
        }

        public void HandleReceivePacket(string ReceivedXmlMessageString)
        {
            try
            {
                Packet receivedPacket = Packet.TransformXmlMessageIntoPacket(ReceivedXmlMessageString);
                string receivedPacketType = receivedPacket.packetType;
                string receivedPacketId = receivedPacket.sourceId + receivedPacket.
                                                            broadcastId.ToString();


                //1. Route Request
                if (receivedPacketType.Equals(PacketConstants.ROUTE_REQUEST_PACKET))
                {
                    // Not Already Received
                    if (!receivedRouteRequestWindow.Contains(receivedPacketId))
                    {
                        //Exist Path For Destination
                        if (!routeTable.IsDestinationPathEmpty(receivedPacket.destinationId))
                        {
                            Hashtable DestinationInfoList;

                            DestinationInfoList = routeTable.GetDestinationInfoFromRouteTable
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

                                string forwardIpAddress = routeTable.GetIPAddressByIDInRouterTable
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
                    routeTable.MakePathEntryForNode(receivedPacket.currentId, InitiatorInfoTable);
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
                        string forwardIpAddress = routeTable.GetIPAddressByIDInRouterTable
                                                              (receivedPacket.currentId);
                        ForwardToNextNeighbour(storedSentPacket, forwardIpAddress);

                    }
                    else if (reversePathTable.Contains(keyReversePath))  //SourceEntry Exist 
                    {
                        receivedPacket.currentId = Node.id;
                        string reverseIpAddress = routeTable.GetIPAddressByIDInRouterTable
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
                    routeTable.MakePathEntryForNode(receivedPacket.currentId, InitiatorInfoTable);

                }


                //3. Route Error 
                else if (receivedPacketType.Equals(PacketConstants.ROUTE_ERROR_PACKET))
                {
                    //TODO
                }
                else //All other Packets
                {
                    presentationLayer = PresentationLayer.presentationLayerInstance;
                    presentationLayer.HandleReceivedDataPackets(receivedPacket);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("HandleReceivePacket: An Exception has occured." + ex.ToString());
            }
        }

        public void ForwardToNextNeighbour(Packet forwardPacket, string destinationIpAddress)
        {
            try
            {
                string XmlMessageStream = forwardPacket.CreateMessageXmlstringFromPacket();
                networkLayer.sendMessageOverUdp(destinationIpAddress, XmlMessageStream);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ForwardToNextNeighbour: An Exception has occured." + ex.ToString());
            }
        }

        public void SendPacket(Packet sendPacket)
        {
            try
            {
                string XmlMessageStream = sendPacket.CreateMessageXmlstringFromPacket();
                string destinationIpAddress = routeTable.GetIPAddressByIDInRouterTable
                                                                (sendPacket.destinationId);
                if (!routeTable.IsDestinationPathEmpty(sendPacket.destinationId))
                {
                    networkLayer.sendMessageOverUdp(destinationIpAddress, XmlMessageStream);
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
       
        public void SendBroadCastPacket(Packet forwardPacket)
        {
            try
            {
                ArrayList neighbourNodesList = routeTable.GetNeighbourNodes(Node.id);
                string XmlMessageStream = forwardPacket.CreateMessageXmlstringFromPacket();

                foreach (string nodeId in neighbourNodesList)
                {
                    if (!nodeId.Equals(forwardPacket.sourceId))
                    {
                        string neighbourIpAddress = routeTable.GetIPAddressByIDInRouterTable(nodeId);
                        networkLayer.sendMessageOverUdp(neighbourIpAddress, XmlMessageStream);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("SendBroadCastPacket: An Exception has occured." + ex.ToString());
            }

        }
        
        public void SaveInSenderBuffer(Packet sentPacket)
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

        public Packet FindStoredPacketinSenderBuffer(string packetId)
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

        public Packet FindStoredPacketinRouteRequestBuffer(string packetId)
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

        public Packet FindStoredPacketinRouteReply(string packetId)
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

        public void SaveInReceivedRouteRequestBuffer(string routeRequestPacketId, Packet routeRequestPacket)
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

        public void SaveInReceivedRouteReplyBuffer(string routeReplyPacketId, Packet routeReplyPacket)
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
