using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;
using System.Xml;

namespace SmartDeviceApplication
{
    public class RouterTableClass
    {
        private static XmlDocument routeTableXml;
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
            try
            {
                XmlNode rootXmlNode = routeTableXml.DocumentElement;
                XmlNodeList childXmlNodes = rootXmlNode.ChildNodes;

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;
                    if (currentElement.GetAttribute("DestinationID").Equals(nodeId))
                    {
                        XmlNodeList routeTableNodes = childNode.ChildNodes;
                        foreach (XmlNode routeTableNode in routeTableNodes)
                        {
                            XmlElement nextHop = (XmlElement)routeTableNode;
                            if (nextHop.InnerText.Equals("EMPTY"))
                                return true;                      
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("FindIDbyIPAddress: An Exception has occured." + ex.ToString());
            }
            return false;
        }

        //Get Destination Path Info
        public static void GetDestinationInfoFromRouteTable(string nodeId,out int destinationSeqNum,out int hopCount,out int lifeTime)
        {
            destinationSeqNum = -1;
            hopCount = -1;
            lifeTime = -1;

            try
            {
                int num = 0;
                XmlNode rootXmlNode = routeTableXml.DocumentElement;
                XmlNodeList childXmlNodes = rootXmlNode.ChildNodes;

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;
                    if (currentElement.GetAttribute("DestinationID").Equals(nodeId))
                    {
                        XmlNodeList routeTableNodes = childNode.ChildNodes;
                        foreach (XmlNode routeTableNode in routeTableNodes)
                        {
                            XmlElement nextHop = (XmlElement)routeTableNode;
                            if (nextHop.Name == "HopCount")
                                hopCount = Convert.ToInt32(nextHop.FirstChild.Value);

                            else if(nextHop.Name == "DestinationSequenceNum")
                                destinationSeqNum = Convert.ToInt32(nextHop.FirstChild.Value);

                            else if(nextHop.Name == "ExpirationTime")
                                lifeTime = Convert.ToInt32(nextHop.FirstChild.Value);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("FindIDbyIPAddress: An Exception has occured." + ex.ToString());
            }

        }

    }
}