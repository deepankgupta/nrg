using System;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Collections;
using System.Threading;
using System.Xml;


namespace SmartDeviceApplication
{

    /// <summary>
    /// Network Class for sending and 
    /// receiving Packets over the network
    /// </summary>
    public class NetworkLayer
    {

        private const int udpPort = 4586;
        private const int udpMessageSize = 1024;
        public static IPAddress IpAddress;
        private UdpClient udpClient;
        public Socket udpReceiverSocket;
        private bool IsUdpReceiverAlive;
        private object networkLock;
        private string routingProtocolName;
        private RoutingProtocol routingProtocol;
        private XmlElement networkHeaderElement;
        private static volatile NetworkLayer instance;
        private static object syncRoot = new Object();

        public static NetworkLayer networkLayerInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new NetworkLayer();
                    }
                }
                return instance;
            }
        }

        private NetworkLayer()
        {
            try
            {
                networkLock = new object();
                IPHostEntry localHostEntry = Dns.GetHostEntry(Dns.GetHostName());
                IpAddress = localHostEntry.AddressList[0];
                XmlNode networkNode = XmlFileUtility.configFileDocument.DocumentElement.
                                                        SelectSingleNode("NetworkLayer");
                networkHeaderElement = (XmlElement)networkNode;
                routingProtocolName = networkHeaderElement.GetAttribute("RoutingProtocol").ToString();
            }
            catch (SocketException e)
            {
                MessageBox.Show("SmartDevice () Exception is occurred: " + e.Message);
                //TODO
            }
        }

        /// <summary>
        /// Filter out proper Routing Protocol 
        /// </summary>
        public class RoutingProtocolFactory
        {
            public static RoutingProtocol GetInstance(string protocolName)
            {
                switch (protocolName)
                {
                    case "Aodv":
                        return AodvRoutingProtocol.aodvInstance;

                    //Similarly do for other protocols.
                }
                return RoutingProtocol.routingProtocolInstance;
            }
        }

        public void ReceiveMessageServerThread()
        {
            IsUdpReceiverAlive = true;
            udpReceiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint localIpEndPoint = new IPEndPoint(IpAddress, udpPort);
            EndPoint localEndEntry = (localIpEndPoint);
            udpReceiverSocket.Bind(localEndEntry);

            while (IsUdpReceiverAlive)
            {
                byte[] saveOverBuffer = new byte[udpMessageSize];
                udpReceiverSocket.ReceiveFrom(saveOverBuffer, ref localEndEntry);
                string receivedMessage = System.Text.Encoding.ASCII.GetString(saveOverBuffer,
                                                    0, saveOverBuffer.Length).ToString();

                routingProtocol = RoutingProtocolFactory.GetInstance(routingProtocolName);
                routingProtocol.HandleReceivedLowerLayerDataStream(receivedMessage.Substring
                                                        (0, receivedMessage.IndexOf('\0')));
            }
        }

        public void AddNetworkLayerStream(string upperLayerStream, string destinationId)
        {
            routingProtocol = RoutingProtocolFactory.GetInstance(routingProtocolName);
            routingProtocol.PrepareNetworkLayerStream(upperLayerStream, destinationId);
        }

        public bool sendMessageOverUdp(string destIpAddress, string textMessageStream)
        {
            bool isSent = false;

            lock (networkLock)
            {
                IPAddress destinationIpAddress = IPAddress.Parse(destIpAddress);
                udpClient = new UdpClient();
                udpClient.Connect(destinationIpAddress, udpPort);

                Byte[] inputToBeSent = new Byte[udpMessageSize];
                inputToBeSent = System.Text.Encoding.ASCII.GetBytes(textMessageStream.ToCharArray());
                int nBytesSent = udpClient.Send(inputToBeSent, inputToBeSent.Length);
                isSent = true;
                udpClient.Close();
            }

            return isSent;
        }

        public void ResetAll()
        {
            routingProtocol = RoutingProtocolFactory.GetInstance(routingProtocolName);
            routingProtocol.ResetAll();
            udpReceiverSocket.Close();
        }
    }
}