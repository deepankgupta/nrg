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
    public class UtilityConfFile
    {
        //Data Members
        private static readonly string ConfFile = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase)+"\\conf.xml";
        public  static XmlDocument xmlDoc;
        struct Sequence
        {
            public Point destination;
            public int speed;
            public int pause;
        }



        //Member Functions
        public static void Initialize()
        {
            try
            {
                xmlDoc = new XmlDocument();
                xmlDoc.Load(ConfFile);
            }
            catch (Exception e)
            {
                MessageBox.Show("SmartDevice Conf File() Exception is occurred: " + e.Message);
            }
        }

        public static string FindIdByIpAddressInConfFile(string nodeIpAddress)
        {
            string nodeId = "NA";
            try
            {
                XmlNode rootNode = xmlDoc.DocumentElement;
                XmlNodeList xNodes = rootNode.ChildNodes;

                foreach (XmlNode childNode in xNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;
                    if (currentElement.GetAttribute("IP").Equals(nodeIpAddress))
                    {
                        nodeId = currentElement.GetAttribute("ID").ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("FindIDbyIPAddress: An Exception has occured." + ex.ToString());
            }
            return nodeId;
        }

        public static string GetNameByIdInConfFile(string nodeId)
        {
            string nodeName = "NA";
            try
            {
                XmlNode rootNode = xmlDoc.DocumentElement;
                XmlNodeList xNodes = rootNode.ChildNodes;

                foreach (XmlNode childNode in xNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;
                    if (currentElement.GetAttribute("ID").Equals(nodeId))
                    {
                        nodeName = currentElement.GetAttribute("NAME").ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetNameById: An Exception has occured." + ex.ToString());
            }
            return nodeName;
        }

        public static int GetPowerRangeInConfFile(string nodeId)
        {
            string powerRange = "-1";
            try
            {
                XmlNode rootNode = xmlDoc.DocumentElement;
                XmlNodeList xNodes = rootNode.ChildNodes;

                foreach (XmlNode childNode in xNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;
                    if (currentElement.GetAttribute("ID").Equals(nodeId))
                    {
                        powerRange = currentElement.GetAttribute("PowerRange").ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetPowerRange: An Exception has occured." + ex.ToString());
            }
            return int.Parse(powerRange);
        }

        public static string GetIPAddressByIDInConfFile(string nodeId)
        {
            string nodeIpAddress = "NA";
            try
            {
                XmlNode rootNode = xmlDoc.DocumentElement;
                XmlNodeList xNodes = rootNode.ChildNodes;

                foreach (XmlNode childNode in xNodes)
                {

                    XmlElement currentElement = (XmlElement)childNode;
                    if (currentElement.GetAttribute("ID").Equals(nodeId))
                    {
                        nodeIpAddress = currentElement.GetAttribute("IP").ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetIPAddressByID: An Exception has occured." + ex.ToString());
            }
            return nodeIpAddress;
        }

    /*    public static Hashtable SetHashSequenceList(string nodeId)
        {
            try
            {
                Hashtable hashSequence = new Hashtable();
                XmlNode rootNode = xmlDoc.DocumentElement;
                XmlNodeList xNodes = rootNode.ChildNodes;

                foreach (XmlNode childNode in xNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;
                    string currentNodeId = currentElement.GetAttribute("ID").ToString();
                    XmlNodeList SequenceNodes = childNode.ChildNodes;
                    ArrayList tempSequenceArrayList = new ArrayList();
                    
                    foreach (XmlNode sequenceNode in SequenceNodes)//<Sequence>
                    {
                        XmlNodeList hopNodes = sequenceNode.ChildNodes;

                        foreach (XmlNode hopNode in hopNodes)//<Hop>
                        {
                            XmlNodeList pathNodes = hopNode.ChildNodes;
                            Sequence seqObject = new Sequence();
                            foreach (XmlNode pathNode in pathNodes)
                            {
                                XmlElement tempXmlElement = (XmlElement)pathNode;
                                if (tempXmlElement.Name.Equals("Destination"))
                                {
                                    string temp = tempXmlElement.FirstChild.Value;

                                    Point tempPoint = new Point();
                                    tempPoint.row = getRow(temp);
                                    tempPoint.col = getCol(temp);
                                    seqObject.destination = tempPoint;
                                }
                                else if (tempXmlElement.Name.Equals("Speed"))
                                {
                                    seqObject.speed = Convert.ToInt32(tempXmlElement.FirstChild.Value);
                                }
                                else
                                {
                                    seqObject.pause = Convert.ToInt32(tempXmlElement.FirstChild.Value);
                                }
                            }
                            tempSequenceArrayList.Add(seqObject);
                        }
                    }

                    hashSequence.Add(nodeId, tempSequenceArrayList);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception in SetHashSequenceList():" + ex.Message);
            }
        }
       */
    }
}
