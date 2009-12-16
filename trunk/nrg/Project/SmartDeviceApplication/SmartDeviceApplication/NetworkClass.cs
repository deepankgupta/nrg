using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Xml;
using System.IO;

namespace SmartDeviceApplication
{
    public class NetworkClass
    {
        //Data Members
        private const int udpPort = 4568;
        private const int messageSize = 512;
        public static IPAddress myIpAddress;
        private ArrayList receivedPacketMessageWindow;
        private ArrayList sentPacketMessageWindow;
             

        //Member Functions
        public NetworkClass()
        {
            try
            {
                IPHostEntry localHostEntry = Dns.GetHostEntry(Dns.GetHostName());
                myIpAddress = localHostEntry.AddressList[0];
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