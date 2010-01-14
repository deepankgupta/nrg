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

        public string id;
        public string name;
        public int sequenceNumber;
        private int powerRange;
        private Point Position;
        struct Point
        {
            public int xCoord;
            public int yCoord;
            public int zCoord;
        }

        public static XmlDocument confFileDocument;
        private static volatile Node instance;
        private static object syncRoot = new Object();

        public static Node nodeInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new Node();
                    }
                }
                return instance;
            }
        }

        private Node()
        {
            InitializeNode();
        }

        public void InitializeNode()
        {
            confFileDocument = XmlFileUtility.configFileDocument;

            try
            {
                XmlNode rootNode = confFileDocument.DocumentElement;
                XmlElement currentElement = (XmlElement)rootNode.FirstChild;

                id = currentElement.GetAttribute("ID");
                name = currentElement.GetAttribute("NAME");
                powerRange = Convert.ToInt32(currentElement.GetAttribute("PowerRange"));
                sequenceNumber = Convert.ToInt32(currentElement.GetAttribute("SequenceNumber"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("InitializeNode: An Exception has occured." + ex.ToString());
            }
        }            
    }
}