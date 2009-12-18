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
        public static void FindNodeValuesInConfFile(ref string nodeId, ref string nodeName, 
                                            ref int nodePowerRange, ref int nodeSequenceNumber)
        {

            try
            {
                XmlNode rootNode = xmlDoc.DocumentElement;
                XmlNodeList childXmlNodes = rootNode.ChildNodes;

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;

                    nodeId = currentElement.GetAttribute("ID");
                    nodeName = currentElement.GetAttribute("NAME");
                    nodePowerRange = Convert.ToInt32(currentElement.GetAttribute("PowerRange"));
                    nodeSequenceNumber = Convert.ToInt32(currentElement.GetAttribute("SequenceNumber"));
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("FindIDbyIPAddress: An Exception has occured." + ex.ToString());
            }
                    
        }

    }
}
