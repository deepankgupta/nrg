using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SynchClient
{
    /// <summary>
    /// synchronise with the SynchServer by sending several packets with current time instance to server over udp
    /// assumption: transmission time in negligible
    /// </summary>
    class SynchClient
    {
        static void Main()
        {
            System.Net.IPAddress destinationIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            System.Net.Sockets.UdpClient udpClient = new System.Net.Sockets.UdpClient();
            int noOfPackets = 20; //no of packets used to synchronise two devices
            int interval = 2000; //interval(Ticks) between packets being sent
            udpClient.Connect(destinationIpAddress, 1234);
            Byte[] dataToBeSent = new Byte[1024];
            while (noOfPackets!=0)
            {
                dataToBeSent = System.Text.Encoding.ASCII.GetBytes(DateTime.Now.TimeOfDay.Ticks.ToString());
                udpClient.Send(dataToBeSent, dataToBeSent.Length);
                Thread.Sleep(interval);
                noOfPackets--;
                
            }
            udpClient.Close();

        }
    }
}
