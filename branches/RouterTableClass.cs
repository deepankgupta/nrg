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
        public  XmlDocument routeTableXml;
        private Semaphore routeTableSynchronize;
        private static volatile RouterTableClass instance;
        private static object syncRoot = new Object();

        public RouterTableClass()
        {
            routeTableXml = XmlFileUtility.FindXmlDoc(XmlFileUtility.RouteFile);
            routeTableSynchronize=new Semaphore(1,1);
        }

        public static RouterTableClass routeTableInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new RouterTableClass();
                    }
                }
                return instance;
            }
        }

        public bool IsDestinationPathEmpty(string nodeId)
        {
            bool flag = false;

            try
            {
                LockRouteTable();
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
                ReleaseRouteTable();
            }

            catch (Exception ex)
            {
                MessageBox.Show("IsDestinationPathEmpty: An Exception has occured." + ex.ToString());
            }
            return flag;
        }

        public Hashtable GetDestinationInfoFromRouteTable(string nodeId)
        {
            Hashtable destinationInfoList = new Hashtable();

            try
            {
                LockRouteTable();

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

                ReleaseRouteTable();
            }

            catch (Exception ex)
            {
                MessageBox.Show("GetDestinationInfoFromRouteTable: An Exception has occured." + ex.ToString());
            }
            return destinationInfoList;
        }

        public ArrayList GetNeighbourNodes(string nodeId)
        {
            ArrayList neighbourNodeList = new ArrayList();

            try
            {
                LockRouteTable();

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
                ReleaseRouteTable();
            }

            catch (Exception ex)
            {
                //MessageBox.Show("GetNeighbourNodes : An Exception has occured." + ex.ToString());
            }
            return neighbourNodeList;
        }

        public string GetIPAddressByIDInRouterTable(string nodeId)
        {
            string nodeIpAddress = "NA";
            try
            {
                LockRouteTable();

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
                ReleaseRouteTable();
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetIPAddressByID: An Exception has occured." + ex.ToString());
            }
            return nodeIpAddress;
        }

        public string GetNameByIDInRouterTable(string nodeId)
        {
            string nodeIpAddress = "NA";
            try
            {
                LockRouteTable();

                XmlNode rootXmlNode = routeTableXml.DocumentElement;
                XmlNodeList childXmlNodes = rootXmlNode.ChildNodes;

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;
                    if (currentElement.GetAttribute("DestinationID").Equals(nodeId))
                    {
                        nodeIpAddress = currentElement.GetAttribute("NAME");
                    }

                }
                ReleaseRouteTable();
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetNameByIDInRouterTable: An Exception has occured." + ex.ToString());
            }
            return nodeIpAddress;
        }

        /// <summary>
        /// Set Path (Reverse or Forward Path) for Intiator Node 
        /// (Source or Destination) and NeighbourNode from which
        /// it received Packet
        /// </summary>
        public void MakePathEntryForNode(string neighbourNodeId, Hashtable InitiatorInfoTable)
        {
            try
            {
                LockRouteTable();

                XmlNode rootXmlNode = routeTableXml.DocumentElement;
                XmlNodeList childXmlNodes = rootXmlNode.ChildNodes;
                IDictionaryEnumerator ide = InitiatorInfoTable.GetEnumerator();
                bool updateTable=false;

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;

                    if (currentElement.GetAttribute("DestinationID").Equals(neighbourNodeId))
                    {
                        currentElement.SelectSingleNode("HopCount").InnerText = "1";
                        currentElement.SelectSingleNode("NextHop").InnerText = neighbourNodeId;
                    }
                    if (currentElement.GetAttribute("DestinationID").Equals(InitiatorInfoTable["DestinationID"].ToString()))
                    {
                        int storedHopCount = Convert.ToInt32(currentElement.SelectSingleNode("HopCount").InnerText);
                        int receivedHopCount = Convert.ToInt32(InitiatorInfoTable["HopCount"].ToString());
                        int storedDestinationSeqNum = Convert.ToInt32(currentElement.SelectSingleNode
                                                                     ("DestinationSequenceNum").InnerText);
                        int receivedDestinationSeqNum = Convert.ToInt32(InitiatorInfoTable
                                                                     ["DestinationSequenceNum"].ToString());

                        if (storedDestinationSeqNum <= receivedDestinationSeqNum)
                            updateTable = true;

                        if (storedHopCount > receivedHopCount)
                            updateTable = true;

                        if (updateTable)
                        {
                            while (ide.MoveNext())
                            {
                                if (ide.Key.ToString().Equals("DestinationSequenceNum"))
                                    currentElement.SelectSingleNode("DestinationSequenceNum").InnerText = ide.Value.ToString();

                                if (ide.Key.ToString().Equals("HopCount"))
                                    currentElement.SelectSingleNode("HopCount").InnerText = ide.Value.ToString();

                                if (ide.Key.ToString().Equals("LifeTime"))
                                    currentElement.SelectSingleNode("LifeTime").InnerText = ide.Value.ToString();

                                if (ide.Key.ToString().Equals("NextHop"))
                                    currentElement.SelectSingleNode("NextHop").InnerText = neighbourNodeId;
                            }
                        }
                    }

                }
                ReleaseRouteTable();    
            }
            catch (Exception ex)
            {
                MessageBox.Show("MakePathEntryForNode: An Exception has occured." + ex.ToString());
            }
        }

        public void DeleteRouteEntryForNode(string nodeId)
        {
            LockRouteTable();

            XmlNode rootXmlNode = routeTableXml.DocumentElement;
            XmlNodeList childXmlNodes = rootXmlNode.ChildNodes;


            foreach (XmlNode childNode in childXmlNodes)
            {
                XmlElement currentElement = (XmlElement)childNode;

                if (currentElement.GetAttribute("DestinationID").Equals(nodeId))
                {
                    currentElement.SelectSingleNode("HopCount").InnerText = PacketConstants.Infinity;
                    currentElement.SelectSingleNode("NextHop").InnerText = "EMPTY";
                    currentElement.SelectSingleNode("LifeTime").InnerText = PacketConstants.EmptyInt.ToString();
                }
            }
            ReleaseRouteTable();
        }


        public void LockRouteTable()
        {
            routeTableSynchronize.WaitOne();
        }

        public void ReleaseRouteTable()
        {
            routeTableSynchronize.Release();
        }

    }
}