using System;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Collections;


namespace SmartDeviceApplication
{
    public class NetworkClass
    {
        //Data Members
        private const int udpPort = 4568;
        private const int udpMessageSize = 512;
        public  static IPAddress IpAddress;
        public  UdpClient udpClient;
        public  bool sentSuccess;
        //private ArrayList receivedPacketMessageWindow;
        //private ArrayList sentPacketMessageWindow;
             

        //Member Functions
        public static void InitializeIpAddress()
        {
            try
            {
                IPHostEntry localHostEntry = Dns.GetHostEntry(Dns.GetHostName());
                IpAddress = localHostEntry.AddressList[0];
            }
            catch (SocketException e)
            {
                MessageBox.Show("SmartDevice () Exception is occurred: " + e.Message);
                //TODO
            }
        }

        public  bool sendMessageOverUdp(string destIpAddress, string textMessageStream)
        {
            
            try
            {
                sentSuccess = false; 
                IPAddress destinationIpAddress = IPAddress.Parse(destIpAddress);
                udpClient = new UdpClient();
                udpClient.Connect(destinationIpAddress, udpPort);

                Byte[] inputToBeSent = new Byte[udpMessageSize];
                inputToBeSent = System.Text.Encoding.ASCII.GetBytes(textMessageStream.ToCharArray());
                int nBytesSent = udpClient.Send(inputToBeSent, inputToBeSent.Length);
                sentSuccess = true;

            }
            catch (SocketException SockExcep)
            {
                MessageBox.Show("Exception is occurred in sendUDP() : " + SockExcep.Message);
                //TODO
            }
            udpClient.Close();
            return sentSuccess;
        }
    }
}