using System;
using System.Collections;
using System.Xml;
using System.Windows.Forms;


namespace SmartDeviceApplication
{

    /// <summary>
    /// Class for Managing Mobile Node Information
    /// like its Id,Name,position etc.
    /// </summary>
  
    public class Node
    {

        public static string id;
        public static string name;
        public static int sequenceNumber;
        private int powerRange;
        private Point Position;
        struct  Point
        {
            public int xCoord;
            public int yCoord;
            public int zCoord;
        }
        private XmlDocument confXmlDoc ;

        private void Initialize()
        {
            confXmlDoc = LoadXmlFiles.FindXmlDoc(LoadXmlFiles.ConfFile);
        }

        //Find Node Attributes from Conf File
        private void FindNodeValuesInConfFile()
        {

            try
            {
                XmlNode rootNode = confXmlDoc.DocumentElement;
                XmlNodeList childXmlNodes = rootNode.ChildNodes;

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement currentElement = (XmlElement)childNode;

                    id = currentElement.GetAttribute("ID");
                    name = currentElement.GetAttribute("NAME");
                    powerRange = Convert.ToInt32(currentElement.GetAttribute("PowerRange"));
                    sequenceNumber = Convert.ToInt32(currentElement.GetAttribute("SequenceNumber"));
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("FindIDbyIPAddress: An Exception has occured." + ex.ToString());
            }
        }            
        
        public Node()
        {
            Initialize();
            FindNodeValuesInConfFile();
        }       

    }

}