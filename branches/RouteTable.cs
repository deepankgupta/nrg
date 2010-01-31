using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using OpenNETCF;
using OpenNETCF.Threading;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Xml;
using System.IO;


namespace SmartDeviceApplication
{
    /// <summary>
    /// Route Table Base Class for Finding and Managing
    /// Neibour Nodes Information From Route Table xml file.
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