using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Xml;

namespace SynchServer
{
    /// <summary>
    /// synchronise with the SynchClient, receives packets with time instance of client and synchronise own time with clients's clock
    /// </summary>
    class SynchServer
    {
       
        static void Main()
        {
            IPHostEntry localHostEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = localHostEntry.AddressList[0];
            Socket udpReceiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint localIpEndPoint = new System.Net.IPEndPoint(ipAddress, 1234);
            EndPoint localEndEntry = (localIpEndPoint);
            udpReceiverSocket.Bind(localEndEntry);
            XmlDocument xmlDocument = new XmlDocument(); //used to save the packets info like received Ticks value, current Tick's value and difference
            int noOfPackets = 20; //no of packets used to synchronise two devices
            xmlDocument.LoadXml("<synch/>");

            byte[] buffer = new byte[32];
            while (noOfPackets != 0)
            {
                udpReceiverSocket.ReceiveFrom(buffer, ref localEndEntry);
                
                string receivedMessage = System.Text.Encoding.ASCII.GetString(buffer,
                                                        0, buffer.Length).ToString();
                string currTicks = DateTime.Now.TimeOfDay.Ticks.ToString();
                XmlElement xmlElement = xmlDocument.CreateElement("packet");
                XmlAttribute xmlAttribute = xmlDocument.CreateAttribute("receivedTicks");
                xmlAttribute.Value = receivedMessage;
                xmlElement.Attributes.Append(xmlAttribute);
                xmlAttribute = xmlDocument.CreateAttribute("ownTicks");
                xmlAttribute.Value = currTicks;
                xmlElement.Attributes.Append(xmlAttribute);
                xmlAttribute = xmlDocument.CreateAttribute("diff");
                xmlAttribute.Value = ((Int64)(Convert.ToInt64(currTicks)-Convert.ToInt64(receivedMessage))).ToString();
                xmlElement.Attributes.Append(xmlAttribute);
                xmlDocument.DocumentElement.AppendChild(xmlElement);
                noOfPackets--;

            }
            xmlDocument.Save("sych.xml");
            SynchroniseTime(xmlDocument);
           
        }

        static void SynchroniseTime(XmlDocument xmlDocument)
        {
            long referenceTimeGap = Convert.ToInt64(xmlDocument.DocumentElement.FirstChild.SelectSingleNode("@diff").Value);
            long avgTimeGap;
            long netDifference = 0;
            long avgDifference;
            foreach (XmlElement xmlElement in xmlDocument.DocumentElement.ChildNodes)
            {
                netDifference += Convert.ToInt64(xmlElement.SelectSingleNode("@diff").Value) - referenceTimeGap;
            }
            avgDifference = netDifference / xmlDocument.DocumentElement.ChildNodes.Count;
            avgTimeGap = referenceTimeGap + avgDifference;

            DateTime.Now.Subtract(TimeSpan.FromTicks(avgTimeGap));
        }
    }
}
