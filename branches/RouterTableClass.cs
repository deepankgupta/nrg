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
    public class RouterTableClass
    {
        public static XmlDocument routeTableXml;
        private static readonly string RouteFile = System.IO.Path.GetDirectoryName
                            (System.Reflection.Assembly.GetExecutingAssembly()
                            .GetName().CodeBase)+"\\RouteTable.xml";

       
        public static void Initialize()
        {
            routeTableXml = new XmlDocument();
            routeTableXml.Load(RouteFile);
        }

        // Check for Destination Path Existence
        public static bool IsDestinationPathEmpty(string nodeId)
        {
            bool flag = false;
            try
            {
                Monitor.Enter(routeTableXml);

                XmlNode rootXmlNode = routeTableXml.DocumentElement;
                XmlNodeList childXmlNodes = rootXmlNode.ChildNodes;

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;
                    if (currentElement.GetAttribute("DestinationID").Equals(nodeId))
                    {
                        if(currentElement.SelectSingleNode("HopCount").InnerText.Equals(PacketConstants.Infinity))
                        {
                            flag = true;
                        }
                        break;
                    }
                }
                Monitor.Exit(routeTableXml);
            }

            catch (Exception ex)
            {
                MessageBox.Show("IsDestinationPathEmpty: An Exception has occured." + ex.ToString());
            }
            return flag;
        }

        //Get Destination Path Info
        public static ArrayList GetDestinationInfoFromRouteTable(string nodeId)
        {
            ArrayList destinationInfoList = new ArrayList();

            try
            {
                Monitor.Enter(routeTableXml);

                XmlNode rootXmlNode = routeTableXml.DocumentElement;
                XmlNodeList childXmlNodes = rootXmlNode.ChildNodes;

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;
                    if (currentElement.GetAttribute("DestinationID").Equals(nodeId))
                    {
                        destinationInfoList.Add(currentElement.SelectSingleNode("DestinationSequenceNum").InnerText);
                        destinationInfoList.Add(currentElement.SelectSingleNode("HopCount").InnerText);
                        destinationInfoList.Add(currentElement.SelectSingleNode("ExpirationTime").InnerText);
                        break;
                    }
                }

                Monitor.Exit(routeTableXml);
            }

            catch (Exception ex)
            {
                MessageBox.Show("GetDestinationInfoFromRouteTable: An Exception has occured." + ex.ToString());
            }
            return destinationInfoList;
        }

        //Get Neighbour Nodes List
        public static ArrayList GetNeighbourNodes(string nodeId)
        {
            ArrayList neighbourNodeList = new ArrayList();

            try
            {
               Monitor.Enter(routeTableXml);

                XmlNode rootXmlNode = routeTableXml.DocumentElement;
                XmlNodeList childXmlNodes = rootXmlNode.ChildNodes;

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;
                    if (!currentElement.GetAttribute("DestinationID").Equals(nodeId))
                    {
                        if (!childNode.SelectSingleNode("HopCount").InnerText.Equals(PacketConstants.Infinity))
                        {
                            neighbourNodeList.Add(currentElement.GetAttribute("DestinationID"));
                        }
                    }
                }
                Monitor.Exit(routeTableXml);
            }

            catch (Exception ex)
            {
                MessageBox.Show("GetNeighbourNodes : An Exception has occured." + ex.ToString());
            }
            return neighbourNodeList;
        }

        //Find IpAddress from Id
        public static string GetIPAddressByIDInRouterTable(string nodeId)
        {
            string nodeIpAddress = "NA";
            try
            {
               Monitor.Enter(routeTableXml);

                XmlNode rootXmlNode = routeTableXml.DocumentElement;
                XmlNodeList childXmlNodes = rootXmlNode.ChildNodes;

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;
                    if (currentElement.GetAttribute("DestinationID").Equals(nodeId))
                    {
                       nodeIpAddress = currentElement.GetAttribute("IpAddress");
                    }

                }
                Monitor.Exit(routeTableXml);
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetIPAddressByID: An Exception has occured." + ex.ToString());
            }
            return nodeIpAddress;
        }

        //Make Reverse Path Entry 
        public static void MakeReversePathEntryForNode(string nodeId)
        {
            try
            {
                Monitor.Enter(routeTableXml);

                XmlNode rootXmlNode = routeTableXml.DocumentElement;
                XmlNodeList childXmlNodes = rootXmlNode.ChildNodes;

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;

                    if (currentElement.GetAttribute("DestinationID").Equals(nodeId))
                    {
                        currentElement.SelectSingleNode("HopCount").InnerText = "1";
                        currentElement.SelectSingleNode("NextHop").InnerText = nodeId;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("MakeReversePathEntryForNode: An Exception has occured." + ex.ToString());
            }                    
        }
    }
}