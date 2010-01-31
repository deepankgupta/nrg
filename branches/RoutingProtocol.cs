using System;
using System.ComponentModel;
using System.Data;
using System.Xml;
using OpenNETCF;
using OpenNETCF.Threading;
using System.Collections;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.CompilerServices;

namespace SmartDeviceApplication
{
    /// <summary>
    /// Base class for several Routing protocols and contains APIs 
    /// for handling and Forwarding NetworkLayer Stream.
    /// </summary>
    public class RoutingProtocol
    {
        public Node node;
        public RouteTable routeTable;
        public string upperLayerStream;
        public string networkLayerStream;
        public NetworkLayer networkLayer;
        public XmlElement networkHeader;
        public SessionLayer sessionLayer;
        public Semaphore semaphoreObject;
        public int broadcastId;
        private static volatile RoutingProtocol instance;
        private static object syncRoot = new Object();

        public static RoutingProtocol routingProtocolInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new RoutingProtocol();
                    }
                }
                return instance;
            }
        }

        public void SetBroadCastId()
        {
            Random r = new Random();
            broadcastId = r.Next(1, int.MaxValue);
        }

        protected RoutingProtocol()
        {
            node = Node.nodeInstance;
            semaphoreObject = new Semaphore(1, 1);
            networkHeader = (XmlElement)XmlFileUtility.configFileDocument.DocumentElement.
                                                            SelectSingleNode("NetworkLayer");
            routeTable = RouteTableFactory.GetInstance(networkHeader.GetAttribute
                                                  ("RoutingProtocol").ToString());
            networkLayer = NetworkLayer.networkLayerInstance;
            SetBroadCastId();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual void HandleReceivedLowerLayerDataStream(string receivedLowerLayerStream)
        {
            object[] dataStream = new object[2];
            XmlFileUtility.FilterStream(receivedLowerLayerStream, networkHeader, dataStream);
            networkLayerStream = (string)dataStream[0];
            upperLayerStream = (string)dataStream[1];
        }

        public virtual void PrepareNetworkLayerStream(string upperLayerStream, string nextNeighbourId)
        {
            string combinedDataStream = XmlFileUtility.CombineLayerStreams(networkHeader, networkLayerStream,
                                                                                   upperLayerStream);
            string nextNeighbourIpAddress = routeTable.GetIPAddressByIDInRouterTable(nextNeighbourId);
            networkLayer.sendMessageOverUdp(nextNeighbourIpAddress, combinedDataStream);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual void BroadcastStream(string streamId, string combinedDataStream) { }

        public virtual void ForwardReceivedStreamToDestination(string destinationId, string receivedLowerLayerStream) { }

        public virtual void ResetAll() { }

        public void WaitForNetworkStream()
        {
            semaphoreObject.WaitOne();
        }

        public void SignalNetworkStream()
        {
            semaphoreObject.Release();
        }
    }
}
