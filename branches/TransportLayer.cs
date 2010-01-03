using System;
using System.Threading;
using System.ComponentModel;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Net;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


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
        public Node node;
        public NetworkLayer networkLayer;
        public RouterTableClass routeTable;
        private PresentationLayer presentationLayer;
        private string receivedHelloMessageStatus;
        public RouteTimer routeTimer;
        private RouteReplyTimer routeReplyTimer;
        private RouteRequestTimer routeRequestTimer;
        private ReversePathTimer reversePathTimer;
        public DataPacketTimer dataPacketTimer;
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

            node = Node.nodeInstance;
            routeRequestTimer = new RouteRequestTimer(TimerConstants.ROUTE_REQUEST_TIMER);
            routeReplyTimer = new RouteReplyTimer(TimerConstants.ROUTE_REPLY_TIMER);
            dataPacketTimer = new DataPacketTimer(TimerConstants.DATA_TIMER);
            reversePathTimer = new ReversePathTimer(TimerConstants.REVERSE_PATH_TIMER);
            routeTable = RouterTableClass.routeTableInstance;
            networkLayer = NetworkLayer.networkLayerInstance;


        }

        /*Timers*/
        public abstract class Timer
        {
            protected int countTimer;
            protected Thread SetTimerClick;
            protected volatile bool enabledTimer;
            protected Node node;
            protected NetworkLayer networkLayer;
            protected RouterTableClass routeTable;

            public Timer(int count)
            {
                countTimer = count;
                enabledTimer = false;
                node = Node.nodeInstance;
                routeTable = RouterTableClass.routeTableInstance;
                networkLayer = NetworkLayer.networkLayerInstance;

            }

            public virtual void SetTimer() { }

            public virtual void SetTimer(string packetId) { }

            public virtual void ReleaseTimer(string packetID) { }

            public virtual void ReleaseTimer() { }

        }

        public class RouteTimer : Timer
        {
            private int broadcastId;
            public string helloMessageStatus;
            private Packet helloMessage;
            private PacketBuilder packetBuilder;

            public RouteTimer(int count): base(count)
            {
                packetBuilder = new PacketBuilder();
                Random rand = new Random();
                broadcastId = rand.Next(1, 100000);
                SetTimerClick = new Thread(new ThreadStart(RouteTimerClick));
            }

            public override void SetTimer()
            {
                enabledTimer = true;
                packetBuilder = packetBuilder.setPacketType(PacketConstants.HELLO_MESSAGE)
                                .setCurrentId(node.id)
                                .setSourceSeqNum(node.sequenceNumber)
                                .setSourceId(node.id);
                SetTimerClick = new Thread(new ThreadStart(RouteTimerClick));
                SetTimerClick.Start();
            }

            public override void ReleaseTimer()
            {
                SetTimerClick.Abort();
            }

            public void RouteTimerClick()
            {
                //Send Periodic Hello Messages To Neighbours

                string XmlMessageStream;
                string neighbourIpAddress;
                ArrayList neighbourNodesList;
                Thread.Sleep(2*countTimer);

                while (enabledTimer)
                {      
                    neighbourNodesList = routeTable.GetNeighbourNodes(node.id);
                    foreach (string nodeId in neighbourNodesList)
                    {
                        if (!nodeId.Equals(node.id))
                        {

                            broadcastId++;
                            helloMessageStatus = nodeId + broadcastId.ToString();
                            helloMessage = packetBuilder.setBroadcastId(broadcastId)
                                                        .setDestinationId(nodeId)
                                                        .build();

                            Hashtable DestinationInfoList = routeTable.GetDestinationInfoFromRouteTable(nodeId);
                            neighbourIpAddress = routeTable.GetIPAddressByIDInRouterTable
                                                         (DestinationInfoList["NextHop"].ToString());
                            XmlMessageStream = helloMessage.CreateMessageXmlstringFromPacket();
                            networkLayer.sendMessageOverUdp(neighbourIpAddress, XmlMessageStream);

                            Thread.Sleep(countTimer);
                            if (!helloMessageStatus.Equals(transportLayerInstance.receivedHelloMessageStatus))
                            {
                                routeTable.DeleteRouteEntryForNode(nodeId);
                            }
                        }
                    }
                }
            }
        }

        public class ReversePathTimer : Timer
        {
            private object reversePathTableLock;
            private Semaphore reversePathSemaphore;
            private Dictionary<int, string> reversePathTable;
            private Dictionary<int, Thread> reversePathThreadWindow;

            public ReversePathTimer(int count): base(count)
            {
                reversePathTableLock = new object();
                reversePathSemaphore = new Semaphore(1, 1);
                reversePathTable = new Dictionary<int, string>();
                reversePathThreadWindow = new Dictionary<int, Thread>();
            }

            public override void SetTimer(string packetId)
            {
                lock (reversePathTableLock)
                {
                    Thread SetTimer = new Thread(new ThreadStart(ReversePathTimerClick));
                    reversePathThreadWindow.Add(Convert.ToInt32(packetId), SetTimer);
                    SetTimer.Start();
                }
            }

            public void ReversePathTimerClick()
            {

                int deltaIncrement = 0;
                while (deltaIncrement <= countTimer)
                {
                    Thread.Sleep(countTimer);
                    deltaIncrement += countTimer;
                }

                lock (reversePathTableLock)
                {
                    Thread currentThread = Thread.CurrentThread;
                    IDictionaryEnumerator ide = reversePathThreadWindow.GetEnumerator();
                    while (ide.MoveNext())
                    {

                        if (((Thread)ide.Value).Equals(currentThread))
                            ReleaseTimer(ide.Key.ToString());
                    }
                }
            }

            public override void ReleaseTimer(string packetId)
            {
                lock (reversePathTableLock)
                {
                    UpdateReversePathTable(Convert.ToInt32(packetId));
                    UpdateReversePathThreadWindow(packetId);
                }
            }

            public void UpdateReversePathThreadWindow(string keyReversePath)
            {
                IDictionaryEnumerator ide = reversePathThreadWindow.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (ide.Key.ToString().Equals(keyReversePath))
                    {
                        Thread storedThread = (Thread)ide.Value;
                        reversePathThreadWindow.Remove(Convert.ToInt32(keyReversePath));
                        storedThread.Abort();
                        break;
                    }
                }
            }


            public void UpdateReversePathTable(int keyReversePath)
            {
                try
                {
                    WaitReversePathTable();
                    if (reversePathTable.ContainsKey(keyReversePath))
                    {
                        reversePathTable.Remove(keyReversePath);
                    }
                    SignalReversePathTable();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in UpdateReversePathTable() :" + e.Message);
                }
            }

            public string FindReverseEntryInReversePathTable(int keyReversePath)
            {
                string neigbourId = "NA";
                try
                {
                    WaitReversePathTable();
                    IDictionaryEnumerator ide = reversePathTable.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (ide.Key.ToString().Equals(keyReversePath.ToString()))
                        {
                            neigbourId = (string)ide.Value;
                            break;
                        }
                    }
                    SignalReversePathTable();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in FindStoredPacketinRouteReply() :" + e.Message);
                }
                return neigbourId;
            }

            public void SaveInReversePathTable(int keyReversePath, string neighbourId)
            {
                try
                {
                    WaitReversePathTable();
                    reversePathTable.Add(keyReversePath, neighbourId);
                    SignalReversePathTable();
                }

                catch (Exception e)
                {
                    MessageBox.Show("Exception in SaveInReversePathTable() :" + e.Message);
                }
            }

            public bool IsPresentInReversePathTable(int keyReversePath)
            {
                bool flag = false;
                try
                {
                    WaitReversePathTable();
                    if (reversePathTable.ContainsKey(keyReversePath))
                        flag = true;
                    SignalReversePathTable();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in IsPresentInReversePathTable() :" + e.Message);
                }
                return flag;
            }

            private void WaitReversePathTable()
            {
                reversePathSemaphore.WaitOne();
            }

            private void SignalReversePathTable()
            {
                reversePathSemaphore.Release();
            }


        }

        public class RouteRequestTimer : Timer
        {
            private object routeRequestWindowLock;
            private Semaphore routeRequestSemaphore;
            private Dictionary<string, Packet> receivedRouteRequestWindow;
            private Dictionary<string, Thread> routeRequestThreadWindow;

            public RouteRequestTimer(int count): base(count)
            {
                routeRequestWindowLock = new object();
                routeRequestSemaphore = new Semaphore(1, 1);
                receivedRouteRequestWindow = new Dictionary<string, Packet>();
                routeRequestThreadWindow = new Dictionary<string, Thread>();
            }

            public override void SetTimer(string packetId)
            {
                lock (routeRequestWindowLock)
                {
                    Thread SetTimer = new Thread(new ThreadStart(RouteRequestTimerClick));
                    routeRequestThreadWindow.Add(packetId, SetTimer);
                    SetTimer.Start();
                }
            }

            public void RouteRequestTimerClick()
            {
                int deltaIncrement = 0;
                while (deltaIncrement <= countTimer)
                {
                    Thread.Sleep(countTimer);
                    deltaIncrement += countTimer;
                }

                lock (routeRequestWindowLock)
                {
                    Thread currentThread = Thread.CurrentThread;
                    IDictionaryEnumerator ide = routeRequestThreadWindow.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (((Thread)ide.Value).Equals(currentThread))
                            ReleaseTimer(ide.Key.ToString());
                    }
                }
            }

            public override void ReleaseTimer(string packetId)
            {
                lock (routeRequestWindowLock)
                {
                    UpdateRouteRequestBuffer(packetId);
                    UpdateRouteRequestThreadWindow(packetId);
                }
            }

            public void UpdateRouteRequestThreadWindow(string packetId)
            {
                IDictionaryEnumerator ide = routeRequestThreadWindow.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (ide.Key.ToString().Equals(packetId))
                    {
                        Thread storedThread = (Thread)ide.Value;
                        routeRequestThreadWindow.Remove(packetId);
                        storedThread.Abort();
                        break;
                    }
                }
            }

            public void UpdateRouteRequestBuffer(string packetId)
            {
                try
                {
                    WaitRouteRequestWindow();
                    if (receivedRouteRequestWindow.ContainsKey(packetId))
                    {
                        receivedRouteRequestWindow.Remove(packetId);
                    }
                    SignalRouteRequestWindow();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in SaveInSendBuffer() :" + e.Message);
                }
            }

            public Packet FindStoredPacketInRouteRequestBuffer(string packetId)
            {
                Packet packet = new Packet();
                try
                {
                    WaitRouteRequestWindow();
                    IDictionaryEnumerator ide = receivedRouteRequestWindow.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (ide.Key.ToString().Equals(packetId))
                        {
                            packet = (Packet)ide.Value;
                            break;
                        }
                    }
                    SignalRouteRequestWindow();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in FindStoredPacketinRouteRequestBuffer() :" + e.Message);
                }
                return packet;
            }

            public bool IsPresentInRouteRequestBuffer(string packetId)
            {
                bool flag = false;
                try
                {
                    WaitRouteRequestWindow();
                    if (receivedRouteRequestWindow.ContainsKey(packetId))
                        flag = true;
                    SignalRouteRequestWindow();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in IsPresentInRouteRequestBuffer() :" + e.Message);
                }
                return flag;
            }

            public void SaveInReceivedRouteRequestBuffer(string routeRequestPacketId, Packet routeRequestPacket)
            {
                try
                {
                    WaitRouteRequestWindow();
                    receivedRouteRequestWindow.Add(routeRequestPacketId, routeRequestPacket);
                    SignalRouteRequestWindow();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in SaveInReceivedRouteRequestBuffer() :" + e.Message);
                }
            }

            private void WaitRouteRequestWindow()
            {
                routeRequestSemaphore.WaitOne();
            }

            private void SignalRouteRequestWindow()
            {
                routeRequestSemaphore.Release();
            }
        }

        public class RouteReplyTimer : Timer
        {
            private object routeReplyWindowLock;
            private Semaphore routeReplySemaphore;
            private Dictionary<string, Packet> receivedRouteReplyWindow;
            private Dictionary<string, Thread> routeReplyThreadWindow;


            public RouteReplyTimer(int count): base(count)
            {
                routeReplySemaphore = new Semaphore(1, 1);
                routeReplyWindowLock = new object();
                receivedRouteReplyWindow = new Dictionary<string, Packet>();
                routeReplyThreadWindow = new Dictionary<string, Thread>();
            }

            public override void SetTimer(string packetId)
            {
                lock (routeReplyWindowLock)
                {
                    Thread SetTimer = new Thread(new ThreadStart(RouteReplyTimerClick));
                    routeReplyThreadWindow.Add(packetId, SetTimer);
                    SetTimer.Start();
                }
            }

            public void RouteReplyTimerClick()
            {
                int deltaIncrement = 0;
                while (deltaIncrement <= countTimer)
                {
                    Thread.Sleep(countTimer);
                    deltaIncrement += countTimer;
                }

                lock (routeReplyWindowLock)
                {
                    Thread currentThread = Thread.CurrentThread;
                    IDictionaryEnumerator ide = routeReplyThreadWindow.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (((Thread)ide.Value).Equals(currentThread))
                            ReleaseTimer(ide.Key.ToString());
                    }
                }
            }

            public override void ReleaseTimer(string packetId)
            {
                lock (routeReplyWindowLock)
                {
                    UpdateRouteReplyBuffer(packetId);
                    UpdateRouteReplyThreadWindow(packetId);
                }
            }

            public void UpdateRouteReplyThreadWindow(string packetId)
            {
                IDictionaryEnumerator ide = routeReplyThreadWindow.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (ide.Key.ToString().Equals(packetId))
                    {
                        Thread storedThread = (Thread)ide.Value;
                        routeReplyThreadWindow.Remove(packetId);
                        storedThread.Abort();
                        break;
                    }
                }
            }

            public void SaveInReceivedRouteReplyBuffer(string routeReplyPacketId, Packet routeReplyPacket)
            {
                try
                {
                    WaitRouteReplyWindow();
                    receivedRouteReplyWindow.Add(routeReplyPacketId, routeReplyPacket);
                    SignalRouteReplyWindow();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in SaveInReceivedRouteReplyBuffer() :" + e.Message);
                }
            }

            public bool IsPresentInRouteReplyBuffer(string packetId)
            {
                bool flag = false;
                try
                {

                    WaitRouteReplyWindow();
                    if (receivedRouteReplyWindow.ContainsKey(packetId))
                        flag = true;
                    SignalRouteReplyWindow();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in IsPresentInRouteReplyBuffer() :" + e.Message);
                }
                return flag;
            }

            public Packet FindStoredPacketInRouteReplyBuffer(string packetId)
            {
                Packet packet = new Packet();
                try
                {
                    WaitRouteReplyWindow();
                    IDictionaryEnumerator ide = receivedRouteReplyWindow.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (ide.Key.ToString().Equals(packetId))
                        {
                            packet = (Packet)ide.Value;
                            break;
                        }
                    }
                    SignalRouteReplyWindow();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in FindStoredPacketinRouteReply() :" + e.Message);
                }
                return packet;
            }

            public void UpdateRouteReplyBuffer(string packetId)
            {
                try
                {
                    WaitRouteReplyWindow();
                    if (receivedRouteReplyWindow.ContainsKey(packetId))
                    {
                        receivedRouteReplyWindow.Remove(packetId);
                    }
                    SignalRouteReplyWindow();
                }

                catch (Exception e)
                {
                    MessageBox.Show("Exception in SaveInSendBuffer() :" + e.Message);
                }

            }
            private void WaitRouteReplyWindow()
            {
                routeReplySemaphore.WaitOne();
            }

            private void SignalRouteReplyWindow()
            {
                routeReplySemaphore.Release();
            }
        }

        public class DataPacketTimer : Timer
        {
            public string dataPacketId;
            private object senderWindowLock;
            private Semaphore senderWindowSemaphore;
            private Dictionary<string, Packet> senderBufferWindow;

            public DataPacketTimer(int count): base(count)
            {
                dataPacketId = "NA";
                senderWindowSemaphore = new Semaphore(1, 1);
                senderWindowLock = new object();
                senderBufferWindow = new Dictionary<string, Packet>();
                SetTimerClick = new Thread(new ThreadStart(DataPacketTimerClick));
            }

            public override void SetTimer()
            {
                WaitSenderBufferWindow();
                if (senderBufferWindow.Count == 1)
                {
                    SetTimerClick = new Thread(new ThreadStart(DataPacketTimerClick));
                    SetTimerClick.Start();
                }
                SignalSenderBufferWindow();
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public void DataPacketTimerClick()
            {
                int deltaIncrement = 0;
                enabledTimer = true;
                while (enabledTimer && deltaIncrement <= countTimer)
                {
                    Thread.Sleep(countTimer);
                    deltaIncrement += countTimer;
                }

                if (deltaIncrement > countTimer)
                {
                    Packet storedPacket = FindStoredPacketInSenderBuffer(dataPacketId);
                    if (!storedPacket.packetType.Equals(PacketConstants.DATA_PACKET))
                    {
                        storedPacket.packetType = PacketConstants.ROUTE_ERROR_PACKET;
                        storedPacket.payloadMessage = PacketConstants.LINK_BREAK;
                        string XmlPacketString = storedPacket.CreateMessageXmlstringFromPacket();
                        transportLayerInstance.HandleReceivePacket(XmlPacketString);
                    }
                }
            }

            public override void ReleaseTimer()
            {
                WaitSenderBufferWindow();
                dataPacketId = "NA";
                enabledTimer = false;
                senderBufferWindow.Clear();
                SignalSenderBufferWindow();
                SetTimerClick.Abort();
            }

            public void SaveInSenderBuffer(Packet sentPacket)
            {
                WaitSenderBufferWindow();
                dataPacketId = sentPacket.sourceId + sentPacket.broadcastId.ToString();
                senderBufferWindow.Add(dataPacketId, sentPacket);
                SignalSenderBufferWindow();
            }

            public Packet FindStoredPacketInSenderBuffer(string packetId)
            {
                Packet packet = new Packet();

                WaitSenderBufferWindow();
                {
                    IDictionaryEnumerator ide = senderBufferWindow.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (ide.Key.ToString().Equals(packetId))
                            packet = (Packet)ide.Value;

                    }
                }
                SignalSenderBufferWindow();
                return packet;
            }

            private void WaitSenderBufferWindow()
            {
                senderWindowSemaphore.WaitOne();
            }

            private void SignalSenderBufferWindow()
            {
                senderWindowSemaphore.Release();
            }
        }

        public void AddItemsToHashTable(Hashtable InitiatorInfoTable, string currentId,
                                         int hopCount, string sourceId, int seqNum)
        {
            InitiatorInfoTable.Add("NextHop", currentId);
            InitiatorInfoTable.Add("HopCount", hopCount + 1);
            InitiatorInfoTable.Add("DestinationID", sourceId);
            InitiatorInfoTable.Add("DestinationSequenceNum", seqNum);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void HandleReceivePacket(string ReceivedXmlMessageString)
        {
            Packet receivedPacket = Packet.TransformXmlMessageIntoPacket(ReceivedXmlMessageString);
            string receivedPacketType = receivedPacket.packetType;
            string receivedPacketId = receivedPacket.sourceId + receivedPacket.
                                                        broadcastId.ToString();

            Hashtable InitiatorInfoTable = new Hashtable();
            AddItemsToHashTable(InitiatorInfoTable, receivedPacket.currentId, receivedPacket.hopCount + 1,
                                            receivedPacket.sourceId, receivedPacket.sourceSeqNum);

            if (!receivedPacket.packetType.Equals(PacketConstants.ROUTE_REPLY_PACKET))
            {
                routeTable.MakePathEntryForNode(receivedPacket.currentId, InitiatorInfoTable);
            }

            //1. Route Request
            if (receivedPacketType.Equals(PacketConstants.ROUTE_REQUEST_PACKET))
            {
                // Not Already Received
                if (!routeRequestTimer.IsPresentInRouteRequestBuffer(receivedPacketId))
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
                            PacketBuilder packetBuilder = new PacketBuilder();
                            Packet routeReplyPacket =
                                                 packetBuilder.setPacketType(PacketConstants.ROUTE_REPLY_PACKET)
                                                .setBroadcastId(receivedPacket.broadcastId)
                                                .setCurrentId(node.id)
                                                .setSourceId(receivedPacket.sourceId)
                                                .setDestinationId(receivedPacket.destinationId)
                                                .setSourceSeqNum(receivedPacket.sourceSeqNum)
                                                .setDestinationSeqNum(Convert.ToInt32(DestinationInfoList
                                                                        ["DestinationSequenceNum"].ToString()))
                                                .setHopCount(Convert.ToInt32(DestinationInfoList
                                                                        ["HopCount"].ToString()))
                                                .build();

                            string forwardIpAddress = routeTable.GetIPAddressByIDInRouterTable
                                                                    (receivedPacket.currentId);

                            ForwardToNextNeighbour(routeReplyPacket, forwardIpAddress);
                            
                            //SET NEW ROUTE REPLY TIMER
                            routeReplyTimer.SaveInReceivedRouteReplyBuffer(receivedPacketId, receivedPacket);
                            routeReplyTimer.SetTimer(receivedPacketId);
                 
                        }
                        else  //BroadCast
                        {
                            receivedPacket.hopCount++;
                            receivedPacket.currentId = node.id;
                            SendBroadCastPacket(receivedPacket);
                        }
                    }
                    else //Path Does not Exist
                    {
                        //ReBroadCast
                        receivedPacket.hopCount++;
                        receivedPacket.currentId = node.id;
                        SendBroadCastPacket(receivedPacket);
                    }
               
                    //SET NEW ROUTE REQUEST TIMER
                    routeRequestTimer.SaveInReceivedRouteRequestBuffer(receivedPacketId, receivedPacket);
                    routeRequestTimer.SetTimer(receivedPacketId);


                    //RELEASE PREVIOUS REVERSE PATH TIMER
                    reversePathTimer.ReleaseTimer(receivedPacket.sourceSeqNum.ToString());

                    //SET REVERSE PATH TIMER
                    reversePathTimer.SaveInReversePathTable(receivedPacket.sourceSeqNum, receivedPacket.currentId);
                    reversePathTimer.SetTimer(receivedPacket.sourceSeqNum.ToString());
                }
                else //Already Received Request
                {
                    Packet storedRouteRequestPacket = routeRequestTimer.FindStoredPacketInRouteRequestBuffer(receivedPacketId);
                    if (storedRouteRequestPacket.hopCount > receivedPacket.hopCount)
                    {
                        //RELEASE PREVIOUS ROUTE REQUEST TIMER
                        routeRequestTimer.ReleaseTimer(receivedPacketId);

                        //SET NEW ROUTE REQUEST TIMER
                        routeRequestTimer.SaveInReceivedRouteRequestBuffer(receivedPacketId, receivedPacket);
                        routeRequestTimer.SetTimer(receivedPacketId);

                        //RELEASE PREVIOUS REVERSE PATH TIMER
                        reversePathTimer.ReleaseTimer(receivedPacket.sourceSeqNum.ToString());

                        // SET NEW REVERSE PATH TIMERS
                        reversePathTimer.SaveInReversePathTable(receivedPacket.sourceSeqNum, receivedPacket.currentId);
                        reversePathTimer.SetTimer(receivedPacket.sourceSeqNum.ToString());
                    }
                }
            }

            //2. Route Reply
            else if (receivedPacketType.Equals(PacketConstants.ROUTE_REPLY_PACKET))
            {
                InitiatorInfoTable.Clear();
                AddItemsToHashTable(InitiatorInfoTable, receivedPacket.currentId, receivedPacket.hopCount + 1,
                                          receivedPacket.destinationId, receivedPacket.destinationSeqNum);
                routeTable.MakePathEntryForNode(receivedPacket.currentId, InitiatorInfoTable);

                if (!routeReplyTimer.IsPresentInRouteReplyBuffer(receivedPacketId))
                {
                    if (routeRequestTimer.IsPresentInRouteRequestBuffer(receivedPacketId))
                    {
                        if (receivedPacket.sourceId.Equals(node.id))
                        {
                            receivedPacket.hopCount++;
                            Packet storedSentPacket = dataPacketTimer.FindStoredPacketInSenderBuffer(receivedPacketId);
                            storedSentPacket.destinationSeqNum = receivedPacket.destinationSeqNum;
                            storedSentPacket.hopCount = receivedPacket.hopCount;
                            string forwardIpAddress = routeTable.GetIPAddressByIDInRouterTable
                                                                  (receivedPacket.currentId);
                            ForwardToNextNeighbour(storedSentPacket, forwardIpAddress);

                            //RELEASE ROUTE REQUEST
                            routeRequestTimer.ReleaseTimer(receivedPacketId);

                            //SET ROUTE REPLY TIMER
                            routeReplyTimer.SaveInReceivedRouteReplyBuffer(receivedPacketId, receivedPacket);
                            routeReplyTimer.SetTimer(receivedPacketId);
                        }
                        else
                        {
                            receivedPacket.currentId = node.id;
                            receivedPacket.hopCount++;
                            int keyReversePath = receivedPacket.sourceSeqNum;
                            string reverseIpAddress = routeTable.GetIPAddressByIDInRouterTable(reversePathTimer.
                                                          FindReverseEntryInReversePathTable(keyReversePath));

                            if (reversePathTimer.IsPresentInReversePathTable(keyReversePath))
                            {
                                routeReplyTimer.SaveInReceivedRouteReplyBuffer(receivedPacketId, receivedPacket);

                                //RELEASE ROUTE REQUEST TIMER
                                routeRequestTimer.ReleaseTimer(receivedPacketId);

                                //SET ROUTE REPLY TIMER
                                routeReplyTimer.SaveInReceivedRouteReplyBuffer(receivedPacketId, receivedPacket);
                                routeReplyTimer.SetTimer(receivedPacketId);
                                ForwardToNextNeighbour(receivedPacket,reverseIpAddress);
                            }
                        }
                    }
                }
                else //Already Received ROUTE REPLY
                {
                    Packet storedRouteReplyPacket = routeReplyTimer.FindStoredPacketInRouteReplyBuffer(receivedPacketId);
                    if (storedRouteReplyPacket.hopCount > receivedPacket.hopCount)
                    {
                        receivedPacket.currentId = node.id;
                        receivedPacket.hopCount++;
                        int keyReversePath = receivedPacket.sourceSeqNum;
                        string reverseIpAddress = routeTable.GetIPAddressByIDInRouterTable(reversePathTimer.
                                                      FindReverseEntryInReversePathTable(keyReversePath));

                        if (reversePathTimer.IsPresentInReversePathTable(keyReversePath))
                        {
                            routeReplyTimer.SaveInReceivedRouteReplyBuffer(receivedPacketId, receivedPacket);

                            //RELEASE ROUTE REQUEST TIMER
                            routeRequestTimer.ReleaseTimer(receivedPacketId);

                            //SET ROUTE REPLY TIMER
                            routeReplyTimer.SaveInReceivedRouteReplyBuffer(receivedPacketId, receivedPacket);
                            routeReplyTimer.SetTimer(receivedPacketId);
                            ForwardToNextNeighbour(receivedPacket, reverseIpAddress);
                        }
                    }
                }
            }

            //3. Route Error
            else if (receivedPacketType.Equals(PacketConstants.ROUTE_ERROR_PACKET))
            {
                if (receivedPacket.sourceId.Equals(node.id))
                {
                    presentationLayer = PresentationLayer.presentationLayerInstance;
                    MessageBox.Show(PacketConstants.LINK_BREAK, "Terminate", MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    presentationLayer.ResetAll();
                    routeTable.DeleteRouteEntryForNode(receivedPacket.destinationId);
                    dataPacketTimer.ReleaseTimer();
                }
                else
                {
                    routeTable.DeleteRouteEntryForNode(receivedPacket.destinationId);
                    if (!routeTable.IsDestinationPathEmpty(receivedPacket.sourceId))
                    {
                        Hashtable DestinationInfoList = routeTable.GetDestinationInfoFromRouteTable
                                                                (receivedPacket.sourceId);
                        string destinationIpAddress = routeTable.GetIPAddressByIDInRouterTable
                                                     (DestinationInfoList["NextHop"].ToString());
                        ForwardToNextNeighbour(receivedPacket, destinationIpAddress);
                    }
                }

            }
            else if (receivedPacketType.Equals(PacketConstants.HELLO_MESSAGE))
            {
                if (receivedPacket.destinationId.Equals(node.id))
                {
                    PacketBuilder packetBuilder = new PacketBuilder();
                    Packet replyHelloMessage =
                                   packetBuilder.setPacketType(PacketConstants.REPLY_HELLO_MESSAGE)
                                  .setBroadcastId(receivedPacket.broadcastId)
                                  .setCurrentId(node.id)
                                  .setSourceId(receivedPacket.destinationId)
                                  .setDestinationId(receivedPacket.sourceId)
                                  .build();

                    string destinationIpAddress = routeTable.GetIPAddressByIDInRouterTable
                                                                (receivedPacket.currentId);
                    ForwardToNextNeighbour(replyHelloMessage, destinationIpAddress);
                }
                else
                {
                    ForwardToDestination(receivedPacket);
                }
            }

             //5. REPLY HELLO MESSAGE
            else if (receivedPacketType.Equals(PacketConstants.REPLY_HELLO_MESSAGE))
            {
                if (receivedPacket.destinationId.Equals(node.id))
                {
                    receivedHelloMessageStatus = receivedPacket.sourceId + receivedPacket.broadcastId.ToString();
                }
                else
                {
                    ForwardToDestination(receivedPacket);
                }
            }

            else //All other Packets
            {

                if (!receivedPacket.destinationId.Equals(node.id))
                {
                    ForwardToDestination(receivedPacket);
                }
                else
                {
                    presentationLayer = PresentationLayer.presentationLayerInstance;
                    presentationLayer.HandleReceivedDataPackets(receivedPacket);
                }
                //TODO  RELEASE DATA TIMERS
            }

            //Update RouteTable

            InitiatorInfoTable.Clear();
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

                if (!(sendPacket.packetType.Equals(PacketConstants.TERMINATE_CHAT_PACKET) ||
                    sendPacket.packetType.Equals(PacketConstants.REJECT_START_CHAT_PACKET)))
                {
                    dataPacketTimer.SaveInSenderBuffer(sendPacket);
                    dataPacketTimer.SetTimer();
                }

                if (!routeTable.IsDestinationPathEmpty(sendPacket.destinationId))
                {
                    Hashtable DestinationInfoList = routeTable.GetDestinationInfoFromRouteTable
                                                            (sendPacket.destinationId);
                    string destinationIpAddress = routeTable.GetIPAddressByIDInRouterTable
                                                 (DestinationInfoList["NextHop"].ToString());

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

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SendBroadCastPacket(Packet forwardPacket)
        {
            try
            {
                string packetId = forwardPacket.sourceId + forwardPacket.broadcastId.ToString();

                //RELEAST PREVIOUS ROUTE REQUEST TIMER
                routeRequestTimer.ReleaseTimer(packetId);

                //SET ROUTE REQUEST TIMER
                routeRequestTimer.SaveInReceivedRouteRequestBuffer(packetId, forwardPacket);
                routeRequestTimer.SetTimer(packetId);

                ArrayList neighbourNodesList = routeTable.GetNeighbourNodes(node.id);
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

        public void ForwardToDestination(Packet receivedPacket)
        {

            if (!routeTable.IsDestinationPathEmpty(receivedPacket.destinationId))
            {
                receivedPacket.currentId = node.id;
                Hashtable DestinationInfoList = routeTable.GetDestinationInfoFromRouteTable
                                                        (receivedPacket.destinationId);
                string destinationIpAddress = routeTable.GetIPAddressByIDInRouterTable
                                             (DestinationInfoList["NextHop"].ToString());
                ForwardToNextNeighbour(receivedPacket, destinationIpAddress);

            }
            else
            {
                receivedPacket.payloadMessage = "LINK BREAK";
                receivedPacket.packetType = PacketConstants.ROUTE_ERROR_PACKET;
                string forwardIpAddress = routeTable.GetIPAddressByIDInRouterTable(receivedPacket.currentId);
                ForwardToNextNeighbour(receivedPacket, forwardIpAddress);
            }
        }
    }
}
