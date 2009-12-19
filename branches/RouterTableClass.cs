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
    /// <summary>
    /// Route Table Class for Finding and Managing
    /// Neibour Nodes Information From Route Table stored
    /// in an XML File.
    /// </summary>

    public class RouterTableClass
    {
        public static XmlDocument routeTableXml;
       
        public static void Initialize()
        {
            routeTableXml = LoadXmlFiles.FindXmlDoc(LoadXmlFiles.RouteFile);
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

                        if (currentElement.SelectSingleNode("HopCount").InnerText.Equals(PacketConstants.Infinity))
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

        public static Hashtable GetDestinationInfoFromRouteTable(string nodeId)
        {
            Hashtable destinationInfoList = new Hashtable();

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
                        destinationInfoList.Add("DestinationSequenceNum", currentElement.SelectSingleNode
                                                                        ("DestinationSequenceNum").InnerText);
                        destinationInfoList.Add("HopCount", currentElement.SelectSingleNode("HopCount").InnerText);
                        destinationInfoList.Add("LifeTime", currentElement.SelectSingleNode("LifeTime").InnerText);
                        destinationInfoList.Add("NextHop", currentElement.SelectSingleNode("NextHop").InnerText);
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

                    if (!childNode.SelectSingleNode("HopCount").InnerText.Equals(PacketConstants.Infinity))
                    {
                        neighbourNodeList.Add(currentElement.GetAttribute("DestinationID"));
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

        /// <summary>
        /// Set Path (or Reverse Path) for a Node from 
        /// which it received a Packet in Route Table
        /// </summary>
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