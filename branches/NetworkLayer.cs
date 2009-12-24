using System;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Collections;
using System.Threading;


namespace SmartDeviceApplication
{

    /// <summary>
    /// Network Class for sending and 
    /// receiving Packets over the network
    /// </summary>
    public class NetworkLayer
    {

        private const int udpPort = 4586;
        private const int udpMessageSize = 512;
        public  static IPAddress IpAddress;
        private UdpClient udpClient;
        public  Socket udpReceiverSocket;
        private bool IsUdpReceiverAlive;
        private TransportLayer transportLayer;
        private object networkLock;
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

        public  NetworkLayer()
        {
            try
            {
                networkLock=new object();
                IPHostEntry localHostEntry = Dns.GetHostEntry(Dns.GetHostName());
                IpAddress = localHostEntry.AddressList[0];
            }
            catch (SocketException e)
            {
                MessageBox.Show("SmartDevice () Exception is occurred: " + e.Message);
                //TODO
            }
        }

        public void ReceiveMessageServerThread()
        {

            IsUdpReceiverAlive = true;
            udpReceiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint localIpEndPoint = new IPEndPoint(IpAddress, udpPort);
            EndPoint localEndEntry = (localIpEndPoint);
            udpReceiverSocket.Bind(localEndEntry);
            transportLayer = TransportLayer.transportLayerInstance;

            while (IsUdpReceiverAlive)
            {
                byte[] saveOverBuffer = new byte[udpMessageSize];
                udpReceiverSocket.ReceiveFrom(saveOverBuffer, ref localEndEntry);
                string receivedMessage = System.Text.Encoding.ASCII.GetString(saveOverBuffer,
                                                    0, saveOverBuffer.Length).ToString();
                transportLayer.HandleReceivePacket(receivedMessage.Substring(0, receivedMessage.IndexOf('\0')));
            }

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

    }
}