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
        public RouteReplyTimer routeReplyTimer;
        public RouteRequestTimer routeRequestTimer;
        public ReversePathTimer reversePathTimer;
        public DataPacketTimer dataPacketTimer;
        private static volatile TransportLayer instance;
        private static object syncRoot = new Object();

        /*Timers*/
        public class Timer
        {
            protected int countTimer;
            protected Thread SetTimer_Click;
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

            public virtual void SetTimer()
            {

            }

            public virtual void ReleaseTimer()
            {
                enabledTimer = false;
                SetTimer_Click.Abort();
            }
        }

        public class RouteTimer : Timer
        {
            private int broadcastId;
            public string helloMessageStatus;
            private Packet helloMessage;
            private PacketBuilder packetBuilder;

            public RouteTimer(int count)
                : base(count)
            {
                packetBuilder = new PacketBuilder();
                Random rand = new Random();
                broadcastId = rand.Next(1, 100000);
                SetTimer_Click = new Thread(new ThreadStart(RouteTimerClick));
            }

            public override void SetTimer()
            {
                enabledTimer = true;
                packetBuilder = new PacketBuilder();
                packetBuilder = packetBuilder.setPacketType(PacketConstants.HELLO_MESSAGE)
                                .setCurrentId(node.id)
                                .setSourceSeqNum(node.sequenceNumber)
                                .setSourceId(node.id);
                SetTimer_Click = new Thread(new ThreadStart(RouteTimerClick));
                SetTimer_Click.Start();
            }

            public override void ReleaseTimer()
            {
                base.ReleaseTimer();
            }

            public void RouteTimerClick()
            {
                //Send Periodic Hello Messages To Neighbours

                string XmlMessageStream;
                string neighbourIpAddress;
                ArrayList neighbourNodesList;

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
            public int deltaIncrement;
            public int keyReversePath;
            public object reversePathTableLock;
            private Dictionary<int, string> reversePathTable;

            public ReversePathTimer(int count)
                : base(count)
            {
                keyReversePath = PacketConstants.EmptyInt;
                reversePathTableLock = new object();
                reversePathTable = new Dictionary<int, string>();
                SetTimer_Click = new Thread(new ThreadStart(ReversePathTimerClick));
            }

            public override void SetTimer()
            {
                if (enabledTimer == true)
                    return;
                SetTimer_Click = new Thread(new ThreadStart(ReversePathTimerClick));
                SetTimer_Click.Start();
            }

            public void ReversePathTimerClick()
            {
                deltaIncrement = 0;
                enabledTimer = true;

                while (enabledTimer && deltaIncrement <= countTimer)
                {
                    Thread.Sleep(countTimer);
                    deltaIncrement += countTimer;
                }

                if (deltaIncrement > countTimer)
                {
                    ReleaseTimer();
                }
            }

            public override void ReleaseTimer()
            {
                UpdateReversePathTable(keyReversePath);
                base.ReleaseTimer();
            }

            public void UpdateReversePathTable(int keyReversePath)
            {
                try
                {
                    lock (reversePathTableLock)
                    {
                        if (reversePathTable.ContainsKey(keyReversePath))
                        {
                            reversePathTable.Remove(keyReversePath);
                        }
                    }
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
                    lock (reversePathTableLock)
                    {
                        IDictionaryEnumerator ide = reversePathTable.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            if (ide.Key.ToString().Equals(keyReversePath))
                                neigbourId = (string)ide.Value;

                        }
                    }
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
                    lock (reversePathTableLock)
                    {
                        reversePathTable.Add(keyReversePath, neighbourId);
                    }
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
                    lock (reversePathTableLock)
                    {
                        if (reversePathTable.ContainsKey(keyReversePath))
                            flag = true;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in IsPresentInReversePathTable() :" + e.Message);
                }
                return flag;
            }

        }

        public class RouteRequestTimer : Timer
        {
            public int deltaIncrement;
            public string packetId;
            public object routeRequestWindowLock;
            public Dictionary<string, Packet> receivedRouteRequestWindow;

            public RouteRequestTimer(int count)
                : base(count)
            {
                packetId = "NA";
                routeRequestWindowLock = new object();
                receivedRouteRequestWindow = new Dictionary<string, Packet>();
                SetTimer_Click = new Thread(new ThreadStart(RouteRequestTimerClick));
            }

            public override void SetTimer()
            {
                SetTimer_Click = new Thread(new ThreadStart(RouteRequestTimerClick));
                SetTimer_Click.Start();
            }

            public void RouteRequestTimerClick()
            {
                deltaIncrement = 0;
                enabledTimer = true;

                while (enabledTimer && deltaIncrement <= countTimer)
                {
                    Thread.Sleep(countTimer);
                    deltaIncrement += countTimer;
                }
                if (deltaIncrement > countTimer)
                {
                    ReleaseTimer();
                }
            }

            public override void ReleaseTimer()
            {
                UpdateRouteRequestBuffer(packetId);
                base.ReleaseTimer();
            }

            public void UpdateRouteRequestBuffer(string packetId)
            {
                try
                {
                    lock (routeRequestWindowLock)
                    {
                        if (receivedRouteRequestWindow.ContainsKey(packetId))
                        {
                            receivedRouteRequestWindow.Remove(packetId);
                        }
                    }
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
                    lock (routeRequestWindowLock)
                    {
                        IDictionaryEnumerator ide = receivedRouteRequestWindow.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            if (ide.Key.ToString().Equals(packetId))
                                packet = (Packet)ide.Value;

                        }

                    }
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
                    lock (routeRequestWindowLock)
                    {
                        if (receivedRouteRequestWindow.ContainsKey(packetId))
                            flag = true;
                    }
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
                    lock (routeRequestWindowLock)
                    {
                        receivedRouteRequestWindow.Add(routeRequestPacketId, routeRequestPacket);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in SaveInReceivedRouteRequestBuffer() :" + e.Message);
                }
            }

        }

        public class RouteReplyTimer : Timer
        {
            public int deltaIncrement;
            public string packetId;
            public object routeReplyWindowLock;
            public Dictionary<string, Packet> receivedRouteReplyWindow;

            public RouteReplyTimer(int count)
                : base(count)
            {
                packetId = "NA";
                routeReplyWindowLock = new object();
                receivedRouteReplyWindow = new Dictionary<string, Packet>();
                SetTimer_Click = new Thread(new ThreadStart(RouteReplyTimerClick));
            }

            public override void SetTimer()
            {
                SetTimer_Click = new Thread(new ThreadStart(RouteReplyTimerClick));
                SetTimer_Click.Start();
            }

            public void RouteReplyTimerClick()
            {
                deltaIncrement = 0;
                enabledTimer = true;

                while (enabledTimer && deltaIncrement <= countTimer)
                {
                    Thread.Sleep(countTimer);
                    deltaIncrement += countTimer;
                }
                if (deltaIncrement > countTimer)
                {
                    ReleaseTimer();
                }
            }

            public override void ReleaseTimer()
            {
                UpdateRouteReplyBuffer(packetId);
                base.ReleaseTimer();
            }

            public void SaveInReceivedRouteReplyBuffer(string routeReplyPacketId, Packet routeReplyPacket)
            {
                try
                {
                    lock (routeReplyWindowLock)
                    {
                        receivedRouteReplyWindow.Add(routeReplyPacketId, routeReplyPacket);
                    }
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
                    lock (routeReplyWindowLock)
                    {
                        if (receivedRouteReplyWindow.ContainsKey(packetId))
                            flag = true;
                    }
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
                    lock (routeReplyWindowLock)
                    {
                        IDictionaryEnumerator ide = receivedRouteReplyWindow.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            if (ide.Key.ToString().Equals(packetId))
                                packet = (Packet)ide.Value;

                        }
                    }
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
                    lock (routeReplyWindowLock)
                    {
                        if (receivedRouteReplyWindow.ContainsKey(packetId))
                        {
                            receivedRouteReplyWindow.Remove(packetId);
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in SaveInSendBuffer() :" + e.Message);
                }

            }

        }

        public class DataPacketTimer : Timer
        {
            public int deltaIncrement;
            public string dataPacketId;
            public object senderWindowLock;
            public Dictionary<string, Packet> senderBufferWindow;

            public DataPacketTimer(int count)
                : base(count)
            {
                senderWindowLock = new object();
                senderBufferWindow = new Dictionary<string, Packet>();
                SetTimer_Click = new Thread(new ThreadStart(DataPacketTimerClick));
            }

            public override void SetTimer()
            {
                SetTimer_Click = new Thread(new ThreadStart(DataPacketTimerClick));
                SetTimer_Click.Start();
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public void DataPacketTimerClick()
            {
                deltaIncrement = 0;
                enabledTimer = true;
                while (enabledTimer && deltaIncrement <= countTimer)
                {
                    Thread.Sleep(countTimer);
                    deltaIncrement += countTimer;
                }

                if (deltaIncrement > countTimer)
                {
                    Packet storedPacket = FindStoredPacketInSenderBuffer(dataPacketId);
                    storedPacket.packetType = PacketConstants.ROUTE_ERROR_PACKET;
                    storedPacket.payloadMessage = PacketConstants.TIMER_EXPIRED;
                    string XmlPacketString = storedPacket.CreateMessageXmlstringFromPacket();
                    transportLayerInstance.HandleReceivePacket(XmlPacketString);
                    ReleaseTimer();
                }
            }

            public override void ReleaseTimer()
            {

                if (dataPacketId != null)
                {
                    dataPacketId = "NA";
                    senderBufferWindow.Clear();
                }
                base.ReleaseTimer();
            }

            public void SaveInSenderBuffer(Packet sentPacket)
            {

                lock (senderWindowLock)
                {
                    dataPacketId = sentPacket.sourceId + sentPacket.broadcastId.ToString();
                    senderBufferWindow.Add(dataPacketId, sentPacket);
                }
            }

            public Packet FindStoredPacketInSenderBuffer(string packetId)
            {
                Packet packet = new Packet();

                lock (senderWindowLock)
                {
                    IDictionaryEnumerator ide = senderBufferWindow.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (ide.Key.ToString().Equals(packetId))
                            packet = (Packet)ide.Value;

                    }
                }

                return packet;
            }
        }

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

                        }
                        else  //BroadCast
                        {
                            receivedPacket.hopCount++;
                            receivedPacket.currentId = node.id;

                            //SET ROUTE REQUEST TIMER
                            routeRequestTimer.SetTimer();
                            routeRequestTimer.SaveInReceivedRouteRequestBuffer(receivedPacketId, receivedPacket);
                            SendBroadCastPacket(receivedPacket);
                        }
                    }
                    else //Path Does not Exist 
                    {
                        //ReBroadCast
                        receivedPacket.hopCount++;
                        receivedPacket.currentId = node.id;

                        //SET ROUTE REQUEST TIMER
                        routeRequestTimer.SetTimer();
                        routeRequestTimer.SaveInReceivedRouteRequestBuffer(receivedPacketId, receivedPacket);
                        SendBroadCastPacket(receivedPacket);
                    }


                    //RELEASE PREVIOUS REVERSE PATH TIMER
                    reversePathTimer.keyReversePath = receivedPacket.sourceSeqNum;
                    reversePathTimer.ReleaseTimer();

                    //SET REVERSE PATH TIMER
                    reversePathTimer.SetTimer();
                    reversePathTimer.SaveInReversePathTable(receivedPacket.sourceSeqNum, receivedPacket.currentId);

                }
                else //Already Received Request
                {
                    Packet storedRouteRequestPacket = routeRequestTimer.FindStoredPacketInRouteRequestBuffer(receivedPacketId);
                    if (storedRouteRequestPacket.hopCount > receivedPacket.hopCount)
                    {
                        //RELEASE PREVIOUS ROUTE REQUEST TIMER
                        routeRequestTimer.packetId = receivedPacketId;
                        routeRequestTimer.ReleaseTimer();

                        //SET NEW ROUTE REQUEST TIMER
                        routeRequestTimer.SaveInReceivedRouteRequestBuffer(receivedPacketId, receivedPacket);
                        routeRequestTimer.SetTimer();

                        //RELEASE PREVIOUS REVERSE PATH TIMER
                        reversePathTimer.keyReversePath = receivedPacket.sourceSeqNum;
                        reversePathTimer.ReleaseTimer();

                        // SET NEW REVERSE PATH TIMERS
                        reversePathTimer.SaveInReversePathTable(receivedPacket.sourceSeqNum, receivedPacket.currentId);
                        reversePathTimer.SetTimer();
                    }
                }
            }

            //2. Route Reply
            else if (receivedPacketType.Equals(PacketConstants.ROUTE_REPLY_PACKET))
            {
                InitiatorInfoTable.Clear();
                AddItemsToHashTable(InitiatorInfoTable, receivedPacket.currentId, receivedPacket.hopCount + 1,
                                          receivedPacket.destinationId, receivedPacket.destinationSeqNum);

                if (!routeReplyTimer.IsPresentInRouteReplyBuffer(receivedPacketId))
                {
                    if (routeRequestTimer.IsPresentInRouteRequestBuffer(receivedPacketId))
                    {
                        if (receivedPacket.sourceId.Equals(node.id))
                        {
                            Packet storedSentPacket = dataPacketTimer.FindStoredPacketInSenderBuffer(receivedPacketId);
                            storedSentPacket.destinationSeqNum = receivedPacket.destinationSeqNum;
                            string forwardIpAddress = routeTable.GetIPAddressByIDInRouterTable
                                                                  (receivedPacket.currentId);
                            ForwardToNextNeighbour(storedSentPacket, forwardIpAddress);

                            //RELEASE ROUTE REQUEST
                            routeRequestTimer.packetId = receivedPacketId;
                            routeRequestTimer.ReleaseTimer();

                            //SET ROUTE REPLY TIMER
                            routeReplyTimer.SaveInReceivedRouteReplyBuffer(receivedPacketId, receivedPacket);
                            routeRequestTimer.SetTimer();
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
                                routeRequestTimer.packetId = receivedPacketId;
                                routeRequestTimer.ReleaseTimer();

                                //RELEASE REVERSE PATH TIMER
                                reversePathTimer.keyReversePath = keyReversePath; ;
                                reversePathTimer.ReleaseTimer();

                                //SET ROUTE REPLY TIMER
                                routeReplyTimer.SaveInReceivedRouteReplyBuffer(receivedPacketId, receivedPacket);
                                routeReplyTimer.SetTimer();
                            }
                        }
                    }
                }
                else //Already Received ROUTE REPLY
                {
                    Packet storedRouteReplyPacket = routeReplyTimer.FindStoredPacketInRouteReplyBuffer(receivedPacketId);
                    if (storedRouteReplyPacket.hopCount > receivedPacket.hopCount)
                    {
                        //RELEASE PERVIOUS ROUTE REPLY TIMER
                        routeReplyTimer.packetId = receivedPacketId;
                        routeReplyTimer.ReleaseTimer();

                        //SET NEW ROUTE REPLY TIMER
                        routeReplyTimer.SaveInReceivedRouteReplyBuffer(receivedPacketId, receivedPacket);
                        routeReplyTimer.SetTimer();
                    }
                }
            }

            //3. Route Error 
            else if (receivedPacketType.Equals(PacketConstants.ROUTE_ERROR_PACKET))
            {
                if (receivedPacket.sourceId.Equals(node.id))
                {
                    dataPacketTimer.dataPacketId = receivedPacketId;
                    Packet storedPacket = dataPacketTimer.FindStoredPacketInSenderBuffer(receivedPacketId);
                    presentationLayer = PresentationLayer.presentationLayerInstance;
                    MessageBox.Show(receivedPacket.payloadMessage, "Terminate", MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    presentationLayer.ResetAll();
                    routeTable.DeleteRouteEntryForNode(receivedPacket.destinationId);
                    dataPacketTimer.ReleaseTimer();
                }
                else
                {
                    routeTable.DeleteRouteEntryForNode(receivedPacket.destinationId);
                    if (!routeTable.IsDestinationPathEmpty(receivedPacket.destinationId))
                    {
                        Hashtable DestinationInfoList = routeTable.GetDestinationInfoFromRouteTable
                                                                (receivedPacket.destinationId);
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
            }

             //5. REPLY HELLO MESSAGE
            else if (receivedPacketType.Equals(PacketConstants.REPLY_HELLO_MESSAGE))
            {
                if (receivedPacket.destinationId.Equals(node.id))
                {
                    receivedHelloMessageStatus = receivedPacket.sourceId + receivedPacket.broadcastId.ToString();
                }
            }

            else //All other Packets
            {

                presentationLayer = PresentationLayer.presentationLayerInstance;
                presentationLayer.HandleReceivedDataPackets(receivedPacket);

                //TODO  RELEASE DATA TIMERS
            }

            //Update RouteTable

            routeTable.MakePathEntryForNode(receivedPacket.currentId, InitiatorInfoTable);
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
                    // TODO     SET ROUTE REQUEST TIMER
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
    }
}

