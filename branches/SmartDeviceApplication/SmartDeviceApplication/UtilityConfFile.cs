using System;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using System.Xml;


namespace SmartDeviceApplication
{

    public class UtilityConfFile
    {
      
        private static readonly string ConfFile = System.IO.Path.GetDirectoryName
                            (System.Reflection.Assembly.GetExecutingAssembly()
                            .GetName().CodeBase)+"\\conf.xml";
        public  static XmlDocument xmlDoc;
      

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

        //Find Node Attributes from Conf File
        public static void FindValuesInXml(ref string nodeId,ref string nodeName,ref int nodePowerRange)
        {

            nodeId = "NA";
            nodeName = "NA";
            string powerRange = "-1";

            try
            {
                XmlNode rootNode = xmlDoc.DocumentElement;
                XmlNodeList childXmlNodes = rootNode.ChildNodes;

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;
                    if (currentElement.GetAttribute("IP").Equals(NetworkClass.IpAddress.ToString()))
                    {
                        nodeId = currentElement.GetAttribute("ID").ToString();
                        nodeName = currentElement.GetAttribute("NAME").ToString();
                        powerRange = currentElement.GetAttribute("PowerRange").ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("FindIDbyIPAddress: An Exception has occured." + ex.ToString());
            }
                    
            nodePowerRange = int.Parse(powerRange);
        }

        //Find IpAddress from Id
        public static string GetIPAddressByIDInConfFile(string nodeId)
        {
            string nodeIpAddress = "NA";
            try
            {
                XmlNode rootXmlNode = xmlDoc.DocumentElement;
                XmlNodeList childXmlNodes = rootXmlNode.ChildNodes;

                foreach (XmlNode childNode in childXmlNodes)
                {

                    XmlElement currentElement = (XmlElement)childNode;
                    if (currentElement.GetAttribute("ID").Equals(nodeId))
                    {
                        nodeIpAddress = currentElement.GetAttribute("IP").ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetIPAddressByID: An Exception has occured." + ex.ToString());
            }
            return nodeIpAddress;
        }

    }
}