using System;
using System.Net.Sockets;
using System.Net;
using OpenNETCF.Threading;
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
    public class RouteTable
    {
        public  XmlDocument routeTableDocument;
        public Semaphore routeTableSynchronize;
        private static volatile RouteTable instance;
        private static object syncRoot = new Object();

        public static RouteTable routeTableInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new RouteTable();
                    }
                }
                return instance;
            }
        }

        protected RouteTable()
        {
            routeTableDocument = XmlFileUtility.routeTableDocument;
            routeTableSynchronize = new Semaphore(1, 1);
        }

        public XmlNodeList GetNodesInRouteXmlFile()
        {
            XmlNode rootXmlNode = routeTableDocument.DocumentElement;
            return rootXmlNode.ChildNodes;
        }

        public bool IsDestinationPathEmpty(string nodeId)
        {

            LockRouteTable();
            XmlNodeList childXmlNodes = GetNodesInRouteXmlFile();

            foreach (XmlNode childNode in childXmlNodes)
            {
                XmlElement currentElement = (XmlElement)childNode;
                if (currentElement.GetAttribute("DestinationID").Equals(nodeId))
                {
                    if (currentElement.SelectSingleNode("HopCount").InnerText.Equals(PacketConstants.Infinity))
                    {
                        ReleaseRouteTable();
                        return true;
                    }
                }
            }
            ReleaseRouteTable();

            return false;
        }

        public virtual Hashtable GetDestinationInfoFromRouteTable(string nodeId) 
        {
            return new Hashtable();
        }

        public ArrayList GetNeighbourNodes(string nodeId)
        {
            ArrayList neighbourNodeList = new ArrayList();

            LockRouteTable();
            XmlNodeList childXmlNodes = GetNodesInRouteXmlFile();

            foreach (XmlNode childNode in childXmlNodes)
            {
                XmlElement currentElement = (XmlElement)childNode;

                if (childNode.SelectSingleNode("HopCount").InnerText.Equals(PacketConstants.OneHop))
                {
                    neighbourNodeList.Add(currentElement.GetAttribute("DestinationID"));
                }
            }
            ReleaseRouteTable();


            return neighbourNodeList;
        }

        public string GetIPAddressByIDInRouterTable(string nodeId)
        {
            LockRouteTable();
            XmlNodeList childXmlNodes = GetNodesInRouteXmlFile();

            foreach (XmlNode childNode in childXmlNodes)
            {
                XmlElement currentElement = (XmlElement)childNode;
                if (currentElement.GetAttribute("DestinationID").Equals(nodeId))
                {
                    ReleaseRouteTable();
                    return currentElement.GetAttribute("IpAddress");
                }
            }
            ReleaseRouteTable();

            return string.Empty;
        }

        public string GetNameByIDInRouterTable(string nodeId)
        {
            try
            {
                LockRouteTable();
                XmlNodeList childXmlNodes = GetNodesInRouteXmlFile();

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;
                    if (currentElement.GetAttribute("DestinationID").Equals(nodeId))
                    {
                        ReleaseRouteTable();
                        return currentElement.GetAttribute("NAME");
                    }
                }
                ReleaseRouteTable();
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetNameByIDInRouterTable: An Exception has occured." + ex.ToString());
            }
            return string.Empty;
        }

        /// <summary>
        /// Set Path (Reverse or Forward Path) for Intiator Node 
        /// (Source or Destination) and NeighbourNode from which
        /// it received Packet
        /// </summary>
        public virtual void MakePathEntryForNode(Hashtable InitiatorInfoTable) { }

        public void DeleteRouteEntryForNode(string nodeId)
        {
            LockRouteTable();
            XmlNodeList childXmlNodes = GetNodesInRouteXmlFile();

            foreach (XmlNode childNode in childXmlNodes)
            {
                XmlElement currentElement = (XmlElement)childNode;

                if (currentElement.GetAttribute("DestinationID").Equals(nodeId))
                {
                    currentElement.SelectSingleNode("HopCount").InnerText = PacketConstants.Infinity;
                    currentElement.SelectSingleNode("NextHop").InnerText = "EMPTY";   
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

    public class AodvRouteTable : RouteTable
    {
        private static volatile AodvRouteTable instance;
        private static object syncRoot = new Object();

        public static AodvRouteTable aodvRouteTableInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new AodvRouteTable();
                    }
                }
                return instance;
            }
        }

        private AodvRouteTable() : base() { }

        public override Hashtable GetDestinationInfoFromRouteTable(string nodeId)
        {
            Hashtable destinationInfoList = new Hashtable();

            try
            {
                LockRouteTable();
                XmlNodeList childXmlNodes = GetNodesInRouteXmlFile();

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

        public override void MakePathEntryForNode(Hashtable InitiatorInfoTable)
        {
            try
            {
                LockRouteTable();
                XmlNodeList childXmlNodes = GetNodesInRouteXmlFile();
                string neighbourNodeId = InitiatorInfoTable["NextHop"].ToString();
                IDictionaryEnumerator ide = InitiatorInfoTable.GetEnumerator();
                bool updateTable = false;

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
                        {
                            updateTable = true;
                        }
                        else
                        {
                            if (updateTable == true)
                                updateTable = false;
                        }

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
    }

    public class RouteTableFactory
    {
        public static RouteTable GetInstance(string routeTable)
        {
            switch (routeTable)
            {
                case "Aodv":
                    return AodvRouteTable.aodvRouteTableInstance;
            }
            return RouteTable.routeTableInstance;
        }
    }
}