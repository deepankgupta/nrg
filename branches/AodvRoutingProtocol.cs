using System;
using System.ComponentModel;
using System.Data;
using System.Xml;
using System.Collections;
using OpenNETCF;
using OpenNETCF.Threading;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.CompilerServices;

namespace SmartDeviceApplication
{
    /// <summary>
    /// Contains Aodv RoutingProtocol specific APIs for handling
    /// and Forwarding NetworkLayer Stream.
    /// </summary>
    
    public class AodvRoutingProtocol : RoutingProtocol
    {
        public RouteTimer routeTimer;
        private RouteReplyTimer routeReplyTimer;
        private RouteRequestTimer routeRequestTimer;
        private ReversePathTimer reversePathTimer;
        private Thread routeTimerThread;
        private static volatile AodvRoutingProtocol instance;
        private static object syncRoot = new Object();

        public static AodvRoutingProtocol aodvInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new AodvRoutingProtocol();
                    }
                }
                return instance;
            }
        }

        private AodvRoutingProtocol() : base()
        {
            routeRequestTimer = new RouteRequestTimer(TimerConstants.ROUTE_REQUEST_TIMER);
            routeReplyTimer = new RouteReplyTimer(TimerConstants.ROUTE_REPLY_TIMER);
            reversePathTimer = new ReversePathTimer(TimerConstants.REVERSE_PATH_TIMER);
            routeTimer = new RouteTimer(TimerConstants.ROUTE_TIMER);
            routeTimerThread = new Thread(new ThreadStart(routeTimer.SetTimer));
            //routeTimerThread.Start();
        }

        public class RouteTimer : Timer
        {
            public RouteTimer(int count)
                : base(count)
            {
                routeTable = RouteTableFactory.GetInstance("Aodv");
            }

            public override void SetTimer()
            {
                int broadCastId;
                AodvPacket helloMessage;
                string helloMessageStream;
                string neighbourIpAddress;
                ArrayList neighbourNodesList;

                AodvPacket.PacketBuilder packetBuilder = new AodvPacket.PacketBuilder();
                packetBuilder = packetBuilder.SetPacketType(PacketConstants.HELLO_MESSAGE)
                                .SetPreviousId(node.id)
                                .SetSourceSeqNum(node.sequenceNumber)
                                .SetSourceId(node.id);

                while (true)
                {
                    Thread.Sleep(2 * countTimer);
                    neighbourNodesList = routeTable.GetNeighbourNodes(node.id);
                    foreach (string nodeId in neighbourNodesList)
                    {
                        if (!nodeId.Equals(node.id))
                        {
                            broadCastId = aodvInstance.broadcastId++;
                            helloMessage = packetBuilder.SetPacketId(node.id + broadCastId.ToString())
                                                        .SetDestinationId(nodeId)
                                                        .BuildAll();

                            Hashtable DestinationInfoList = routeTable.GetDestinationInfoFromRouteTable(nodeId);
                            neighbourIpAddress = routeTable.GetIPAddressByIDInRouterTable
                                                         (DestinationInfoList["NextHop"].ToString());
                            helloMessageStream = helloMessage.CreateStreamFromPacket();
                            string combinedMessageStream = XmlFileUtility.CombineLayerStreams(aodvInstance.networkHeader,
                                                                        helloMessageStream, PacketConstants.EmptyString);


                            SetTimerThread = new Thread(new ThreadStart(SetTimerStart));
                            threadWindowForStreamSent.Add(helloMessage.packetId, SetTimerThread);
                            bufferWindowForStreamSent.Add(helloMessage.packetId, nodeId);
                            aodvInstance.networkLayer.sendMessageOverUdp(neighbourIpAddress, combinedMessageStream);
                            SetTimerThread.Start();
                        }
                    }
                    Thread.Sleep(countTimer);
                }
            }

            public override void SetTimerStart()
            {
                int deltaIncrement = 0;
                while (deltaIncrement <= countTimer)
                {
                    Thread.Sleep(countTimer);
                    deltaIncrement += countTimer;
                }

                lock (synchronizedLockOnThreads)
                {
                    Thread currentThread = Thread.CurrentThread;
                    IDictionaryEnumerator ide = threadWindowForStreamSent.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (((Thread)ide.Value).Equals(currentThread))
                        {
                            string storedNodeId = FindStoredStreamInBufferWindow(ide.Key.ToString());
                            routeTable.DeleteRouteEntryForNode(storedNodeId);
                            ReleaseTimer(ide.Key.ToString());
                        }
                    }
                }
            }
        }

        public class ReversePathTimer : Timer
        {
            public ReversePathTimer(int count) : base(count) { }

            public override void SetTimer(string packetId)
            {
                lock (synchronizedLockOnThreads)
                {
                    SetTimerThread = new Thread(new ThreadStart(SetTimerStart));
                    threadWindowForStreamSent.Add(packetId, SetTimerThread);
                    SetTimerThread.Start();
                }
            }

            public override void SetTimerStart()
            {
                base.SetTimerStart();
            }
        }

        public class RouteRequestTimer : Timer
        {
            public RouteRequestTimer(int count) : base(count) { }

            public override void SetTimer(string packetId)
            {
                lock (synchronizedLockOnThreads)
                {
                    SetTimerThread = new Thread(new ThreadStart(SetTimerStart));
                    threadWindowForStreamSent.Add(packetId, SetTimerThread);
                    SetTimerThread.Start();
                }
            }

            public override void SetTimerStart()
            {
                base.SetTimerStart();
            }
        }

        public class RouteReplyTimer : Timer
        {
            public RouteReplyTimer(int count) : base(count) { }

            public override void SetTimer(string packetId)
            {
                lock (synchronizedLockOnThreads)
                {
                    SetTimerThread = new Thread(new ThreadStart(SetTimerStart));
                    threadWindowForStreamSent.Add(packetId, SetTimerThread);
                    SetTimerThread.Start();
                }
            }

            public override void SetTimerStart()
            {
                base.SetTimerStart();
            }
        }

        public void AddItemsToHashTable(Hashtable DestinationInfoTable, string previousId,
                                         int hopCount, string sourceId, int seqNum)
        {
            DestinationInfoTable.Add("NextHop", previousId);
            DestinationInfoTable.Add("HopCount", hopCount + 1);
            DestinationInfoTable.Add("DestinationID", sourceId);
            DestinationInfoTable.Add("DestinationSequenceNum", seqNum);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void HandleReceivedLowerLayerDataStream(string receivedLowerLayerStream)
        {
            WaitForNetworkStream();
            base.HandleReceivedLowerLayerDataStream(receivedLowerLayerStream);
            AodvPacket receivedPacket = AodvPacket.TransformStreamToPacket(networkLayerStream);
            string receivedPacketType = receivedPacket.packetType;
            string receivedPacketId = receivedPacket.packetId;
            string combinedDataStream = string.Empty;

            Hashtable DestinationInfoTable = new Hashtable();
            AddItemsToHashTable(DestinationInfoTable, receivedPacket.previousId, receivedPacket.hopCount + 1,
                                            receivedPacket.sourceId, receivedPacket.sourceSeqNum);

            if (!receivedPacket.packetType.Equals(PacketConstants.ROUTE_REPLY_PACKET))
            {
                routeTable.MakePathEntryForNode(DestinationInfoTable);
            }

            //1. Route Request
            if (receivedPacketType.Equals(PacketConstants.ROUTE_REQUEST_PACKET))
            {

                if (!receivedPacket.sourceId.Equals(node.id))
                {
                    // Not Already Received
                    if (!routeRequestTimer.IsPresentInBufferWindow(receivedPacketId))
                    {
                        //Exist Path For Destination
                        if (!routeTable.IsDestinationPathEmpty(receivedPacket.destinationId))
                        {
                            Hashtable DestinationInfoList = routeTable.GetDestinationInfoFromRouteTable
                                                                    (receivedPacket.destinationId);
                            int savedDestinationSeqNum = Convert.ToInt32(DestinationInfoList
                                                                    ["DestinationSequenceNum"].ToString());

                            if (savedDestinationSeqNum >= receivedPacket.destinationSeqNum)
                            {
                                //Send RouteReply
                                AodvPacket.PacketBuilder packetBuilder = new AodvPacket.PacketBuilder();
                                AodvPacket routeReplyPacket =
                                                     packetBuilder.SetPacketType(PacketConstants.ROUTE_REPLY_PACKET)
                                                    .SetPacketId(receivedPacket.packetId)
                                                    .SetPreviousId(node.id)
                                                    .SetSourceId(receivedPacket.sourceId)
                                                    .SetDestinationId(receivedPacket.destinationId)
                                                    .SetSourceSeqNum(receivedPacket.sourceSeqNum)
                                                    .SetDestinationSeqNum(Convert.ToInt32(DestinationInfoList
                                                                            ["DestinationSequenceNum"].ToString()))
                                                    .SetHopCount(Convert.ToInt32(DestinationInfoList
                                                                            ["HopCount"].ToString()))
                                                    .BuildAll();

                                networkLayerStream = routeReplyPacket.CreateStreamFromPacket();
                                base.PrepareNetworkLayerStream(upperLayerStream, receivedPacket.previousId);

                                //SET NEW ROUTE REPLY TIMER
                                routeReplyTimer.SaveInBufferWindow(receivedPacketId, combinedDataStream);
                                routeReplyTimer.SetTimer(receivedPacketId);

                            }
                            else  //BroadCast
                            {
                                receivedPacket.hopCount++;
                                receivedPacket.previousId = node.id;
                                networkLayerStream = receivedPacket.CreateStreamFromPacket();
                                combinedDataStream = XmlFileUtility.CombineLayerStreams(networkHeader,
                                                                networkLayerStream, upperLayerStream);
                                BroadcastStream(receivedPacketId, combinedDataStream);
                            }
                        }
                        else //Path Does not Exist
                        {
                            //ReBroadCast
                            receivedPacket.hopCount++;
                            receivedPacket.previousId = node.id;
                            networkLayerStream = receivedPacket.CreateStreamFromPacket();
                            combinedDataStream = XmlFileUtility.CombineLayerStreams(networkHeader,
                                                                networkLayerStream, upperLayerStream);
                            BroadcastStream(receivedPacketId, combinedDataStream);
                        }

                        //RELEASE PREVIOUS ROUTE REQUEST TIMER
                        routeRequestTimer.ReleaseTimer(receivedPacketId);

                        //SET NEW ROUTE REQUEST TIMER
                        networkLayerStream = receivedPacket.CreateStreamFromPacket();
                        combinedDataStream = XmlFileUtility.CombineLayerStreams(networkHeader, networkLayerStream,
                                                                        upperLayerStream);
                        routeRequestTimer.SaveInBufferWindow(receivedPacketId, combinedDataStream);
                        routeRequestTimer.SetTimer(receivedPacketId);

                        //RELEASE PREVIOUS REVERSE PATH TIMER
                        reversePathTimer.ReleaseTimer(receivedPacket.sourceSeqNum.ToString());

                        //SET REVERSE PATH TIMER
                        reversePathTimer.SaveInBufferWindow(receivedPacket.sourceSeqNum.ToString(),
                                                                            receivedPacket.previousId);
                        reversePathTimer.SetTimer(receivedPacket.sourceSeqNum.ToString());
                    }
                    else //Already Received Request
                    {
                        string storedStream = routeRequestTimer.FindStoredStreamInBufferWindow(receivedPacketId);
                        base.HandleReceivedLowerLayerDataStream(storedStream);
                        AodvPacket storedRouteRequestPacket = AodvPacket.TransformStreamToPacket(networkLayerStream);

                        if (storedRouteRequestPacket.hopCount > receivedPacket.hopCount)
                        {
                            //RELEASE PREVIOUS ROUTE REQUEST TIMER
                            routeRequestTimer.ReleaseTimer(receivedPacketId);

                            //SET NEW ROUTE REQUEST TIMER
                            routeRequestTimer.SaveInBufferWindow(receivedPacketId, receivedLowerLayerStream);
                            routeRequestTimer.SetTimer(receivedPacketId);

                            //RELEASE PREVIOUS REVERSE PATH TIMER
                            reversePathTimer.ReleaseTimer(receivedPacket.sourceSeqNum.ToString());

                            // SET NEW REVERSE PATH TIMERS
                            reversePathTimer.SaveInBufferWindow(receivedPacket.sourceSeqNum.ToString(),
                                                                                receivedPacket.previousId);
                            reversePathTimer.SetTimer(receivedPacket.sourceSeqNum.ToString());
                        }
                    }
                }
            }

                //2. Route Reply
            else if (receivedPacketType.Equals(PacketConstants.ROUTE_REPLY_PACKET))
            {
                DestinationInfoTable.Clear();
                AddItemsToHashTable(DestinationInfoTable, receivedPacket.previousId, receivedPacket.hopCount + 1,
                                          receivedPacket.destinationId, receivedPacket.destinationSeqNum);
                routeTable.MakePathEntryForNode(DestinationInfoTable);

                if (!routeReplyTimer.IsPresentInBufferWindow(receivedPacketId))
                {
                    if (routeRequestTimer.IsPresentInBufferWindow(receivedPacketId))
                    {
                        if (receivedPacket.sourceId.Equals(node.id))
                        {
                            receivedPacket.hopCount++;
                            string storedStream = routeRequestTimer.FindStoredStreamInBufferWindow(receivedPacketId);
                            base.HandleReceivedLowerLayerDataStream(storedStream);

                            AodvPacket storedDRouteRequestPacket = AodvPacket.TransformStreamToPacket(networkLayerStream);
                            storedDRouteRequestPacket.destinationSeqNum = receivedPacket.destinationSeqNum;
                            storedDRouteRequestPacket.hopCount = receivedPacket.hopCount;
                            networkLayerStream = storedDRouteRequestPacket.CreateStreamFromPacket();
                            base.PrepareNetworkLayerStream(upperLayerStream, receivedPacket.previousId);

                            //RELEASE ROUTE REQUEST
                            routeRequestTimer.ReleaseTimer(receivedPacketId);

                            //SET ROUTE REPLY TIMER
                            routeReplyTimer.SaveInBufferWindow(receivedPacketId, combinedDataStream);
                            routeReplyTimer.SetTimer(receivedPacketId);
                        }
                        else
                        {
                            receivedPacket.previousId = node.id;
                            receivedPacket.hopCount++;
                            int keyReversePath = receivedPacket.sourceSeqNum;
                            string reversePathId = reversePathTimer.FindStoredStreamInBufferWindow(keyReversePath.ToString());

                            if (reversePathTimer.IsPresentInBufferWindow(keyReversePath.ToString()))
                            {
                                networkLayerStream = receivedPacket.CreateStreamFromPacket();
                                base.PrepareNetworkLayerStream(upperLayerStream, reversePathId);

                                //RELEASE ROUTE REQUEST TIMER
                                routeRequestTimer.ReleaseTimer(receivedPacketId);

                                //SET ROUTE REPLY TIMER
                                routeReplyTimer.SaveInBufferWindow(receivedPacketId, combinedDataStream);
                                routeReplyTimer.SetTimer(receivedPacketId);
                            }
                        }
                    }
                }
                else //Already Received ROUTE REPLY
                {
                    string storedStream = routeReplyTimer.FindStoredStreamInBufferWindow(receivedPacketId);
                    base.HandleReceivedLowerLayerDataStream(storedStream);
                    AodvPacket storedRouteReplyPacket = AodvPacket.TransformStreamToPacket(networkLayerStream);

                    if (storedRouteReplyPacket.hopCount > receivedPacket.hopCount)
                    {
                        receivedPacket.previousId = node.id;
                        receivedPacket.hopCount++;
                        int keyReversePath = receivedPacket.sourceSeqNum;
                        string reversePathId = reversePathTimer.FindStoredStreamInBufferWindow(keyReversePath.ToString());

                        if (reversePathTimer.IsPresentInBufferWindow(keyReversePath.ToString()))
                        {
                            networkLayerStream = receivedPacket.CreateStreamFromPacket();
                            base.PrepareNetworkLayerStream(upperLayerStream, reversePathId);

                            //RELEASE ROUTE REQUEST TIMER
                            routeRequestTimer.ReleaseTimer(receivedPacketId);

                            //RELEASE PREVIOUS ROUTE REPLY TIMER
                            routeReplyTimer.ReleaseTimer(receivedPacketId);

                            //SET ROUTE REPLY TIMER
                            routeReplyTimer.SaveInBufferWindow(receivedPacketId, combinedDataStream);
                            routeReplyTimer.SetTimer(receivedPacketId);
                        }
                    }
                }
            }

            //3. Route Error
            else if (receivedPacketType.Equals(PacketConstants.ROUTE_ERROR_PACKET))
            {
                if (receivedPacket.sourceId.Equals(node.id))
                {
                    sessionLayer = SessionLayer.sessionLayerInstance;
                    MessageBox.Show(PacketConstants.LINK_BREAK, "Terminate", MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    sessionLayer.ResetAll();
                    routeTable.DeleteRouteEntryForNode(receivedPacket.destinationId);
                }
                else
                {
                    routeTable.DeleteRouteEntryForNode(receivedPacket.destinationId);
                    if (!routeTable.IsDestinationPathEmpty(receivedPacket.sourceId))
                    {
                        Hashtable DestinationInfoList = routeTable.GetDestinationInfoFromRouteTable
                                                                (receivedPacket.sourceId);
                        string nextNeighbourId = DestinationInfoList["NextHop"].ToString();
                        base.PrepareNetworkLayerStream(upperLayerStream, nextNeighbourId);
                    }
                }
            }
            else if (receivedPacketType.Equals(PacketConstants.HELLO_MESSAGE))
            {
                if (receivedPacket.destinationId.Equals(node.id))
                {
                    AodvPacket.PacketBuilder packetBuilder = new AodvPacket.PacketBuilder();
                    AodvPacket replyHelloMessage =
                                            packetBuilder.SetPacketType(PacketConstants.REPLY_HELLO_MESSAGE)
                                            .SetPacketId(receivedPacket.packetId)
                                            .SetPreviousId(node.id)
                                            .SetSourceId(receivedPacket.destinationId)
                                            .SetDestinationId(receivedPacket.sourceId)
                                            .BuildAll();

                    networkLayerStream = replyHelloMessage.CreateStreamFromPacket();
                    base.PrepareNetworkLayerStream(upperLayerStream, receivedPacket.previousId);
                }
                else
                {
                    ForwardReceivedStreamToDestination(receivedPacket.destinationId, receivedLowerLayerStream);
                }
            }

             //5. REPLY HELLO MESSAGE
            else if (receivedPacketType.Equals(PacketConstants.REPLY_HELLO_MESSAGE))
            {
                if (receivedPacket.destinationId.Equals(node.id))
                {
                    routeTimer.ReleaseTimer(receivedPacket.packetId);
                }
                else
                {
                    ForwardReceivedStreamToDestination(receivedPacket.destinationId, receivedLowerLayerStream);
                }
            }

            else //All other Packets
            {
                if (!receivedPacket.destinationId.Equals(node.id))
                {
                    ForwardReceivedStreamToDestination(receivedPacket.destinationId, receivedLowerLayerStream);
                }
                else
                {
                    string sessionLayerStream = upperLayerStream;
                    SignalNetworkStream();
                    sessionLayer = SessionLayer.sessionLayerInstance;
                    sessionLayer.HandleReceivedLowerLayerStream(sessionLayerStream);
                    return;
                }
                //TODO  RELEASE DATA TIMERS
            }

            //Update RouteTable
            DestinationInfoTable.Clear();
            SignalNetworkStream();
        }

        public override void PrepareNetworkLayerStream(string upperLayerStream, string destinationId)
        {
            try
            {
                WaitForNetworkStream();
                broadcastId++;
                Hashtable destinationInfoList = routeTable.GetDestinationInfoFromRouteTable(destinationId);
                AodvPacket.PacketBuilder packetBuilder = new AodvPacket.PacketBuilder();
                AodvPacket routePacket =
                                 packetBuilder.SetPacketType(PacketConstants.DATA_PACKET)
                                .SetPacketId(node.id + broadcastId.ToString())
                                .SetPreviousId(node.id)
                                .SetSourceId(node.id)
                                .SetDestinationId(destinationId)
                                .SetSourceSeqNum(node.sequenceNumber)
                                .SetHopCount(Convert.ToInt32(destinationInfoList
                                                        ["HopCount"].ToString()))
                                .SetDestinationSeqNum(Convert.ToInt32(destinationInfoList
                                                        ["DestinationSequenceNum"].ToString()))
                                .BuildAll();

                networkLayerStream = routePacket.CreateStreamFromPacket();

                if (!routeTable.IsDestinationPathEmpty(destinationId))
                {
                    string neighbourId = destinationInfoList["NextHop"].ToString();
                    base.PrepareNetworkLayerStream(upperLayerStream, neighbourId);
                }
                else
                {
                    //Route Request Packet
                    AodvPacket routeRequestPacket = routePacket;
                    routeRequestPacket.packetType = PacketConstants.ROUTE_REQUEST_PACKET;
                    networkLayerStream = routeRequestPacket.CreateStreamFromPacket();
                    string combinedDataStream = XmlFileUtility.CombineLayerStreams(networkHeader,
                                                            networkLayerStream, upperLayerStream);
                    BroadcastStream(routeRequestPacket.packetId, combinedDataStream);

                }
                SignalNetworkStream();
            }

            catch (Exception ex)
            {
                MessageBox.Show("SendPacket: An Exception has occured." + ex.ToString());
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void BroadcastStream(string streamId, string combinedDataStream)
        {
            try
            {
                //RELEAST PREVIOUS ROUTE REQUEST TIMER
                routeRequestTimer.ReleaseTimer(streamId);

                //SET ROUTE REQUEST TIMER
                routeRequestTimer.SaveInBufferWindow(streamId, combinedDataStream);
                routeRequestTimer.SetTimer(streamId);

                ArrayList neighbourNodesList = routeTable.GetNeighbourNodes(node.id);

                foreach (string nodeId in neighbourNodesList)
                {
                    string neighbourIpAddress = routeTable.GetIPAddressByIDInRouterTable(nodeId);
                    networkLayer.sendMessageOverUdp(neighbourIpAddress, combinedDataStream);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("BroadcastStream: An Exception has occured." + ex.ToString());
            }

        }

        public override void ForwardReceivedStreamToDestination(string destinationId, string receivedLowerLayerStream)
        {
            base.HandleReceivedLowerLayerDataStream(receivedLowerLayerStream);
            AodvPacket receivedPacket = AodvPacket.TransformStreamToPacket(networkLayerStream);
            string nextNeighbourId = string.Empty;

            if (!routeTable.IsDestinationPathEmpty(destinationId))
            {
                receivedPacket.previousId = node.id;
                Hashtable DestinationInfoList = routeTable.GetDestinationInfoFromRouteTable
                                                        (receivedPacket.destinationId);
                nextNeighbourId = DestinationInfoList["NextHop"].ToString();
            }
            else
            {
                receivedPacket.packetType = PacketConstants.ROUTE_ERROR_PACKET;
                nextNeighbourId = receivedPacket.previousId;
            }

            networkLayerStream = receivedPacket.CreateStreamFromPacket();
            base.PrepareNetworkLayerStream(upperLayerStream, nextNeighbourId);

        }

        public override void ResetAll()
        {
            routeTimerThread.Abort();
            routeTimer.ReleaseAllTimerThread();
            routeRequestTimer.ReleaseAllTimerThread();
            routeReplyTimer.ReleaseAllTimerThread();
            reversePathTimer.ReleaseAllTimerThread();
        }
    }
}
