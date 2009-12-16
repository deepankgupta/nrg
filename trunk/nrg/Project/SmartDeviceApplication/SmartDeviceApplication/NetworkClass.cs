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
        private const int messageSize = 512;
        public  static IPAddress IpAddress;
        private ArrayList receivedPacketMessageWindow;
        private ArrayList sentPacketMessageWindow;
             

        //Member Functions
        public NetworkClass()
        {
            try
            {
                IPHostEntry localHostEntry = Dns.GetHostEntry(Dns.GetHostName());
                IpAddress = localHostEntry.AddressList[0];
            }
            catch (SocketException e)
            {
                MessageBox.Show("SmartDevice () Exception is occurred: " + e.Message);
            }
        }

        public void ReceiverThreadFunction()
        {

        }
    }
}