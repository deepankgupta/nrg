using System;
using System.Xml;

namespace SmartDeviceApplication
{
    /// <summary>
    /// Load Xml Files and contains functions to create 
    /// Xml documents and filtering Xml documents into
    /// Elements
    /// </summary>

    public class XmlFileUtility
    {

        private static readonly string pathname = System.IO.Path.GetDirectoryName
                         (System.Reflection.Assembly.GetExecutingAssembly()
                         .GetName().CodeBase);
        private static readonly string configFile = pathname + "\\conf.xml";
        private static readonly string routeTableFile = pathname + "\\RouteTable.xml";
        public static XmlDocument configFileDocument;
        public static XmlDocument routeTableDocument;


        public static void Initialize()
        {
            configFileDocument=new XmlDocument();
            configFileDocument.Load(configFile);
            routeTableDocument=new XmlDocument();
            routeTableDocument.Load(routeTableFile);
        }

        public static string CombineLayerStreams(XmlElement layerHeaderElement, string currentLayerStream,
                                                                            string upperLayerStream)
        {
            XmlDocument combinedLayerDocument = new XmlDocument();
            combinedLayerDocument.LoadXml("<Stream></Stream>");
            XmlNode rootNode = (XmlNode)combinedLayerDocument.DocumentElement;

            XmlElement headerElement = combinedLayerDocument.CreateElement("header");
            XmlElement dataStreamElement = combinedLayerDocument.CreateElement("data");
            XmlElement currentLayerStreamElement = combinedLayerDocument.CreateElement("currentLayer");
            XmlElement upperLayerStreamElement = combinedLayerDocument.CreateElement("upperLayer");

            //Header Element

            foreach (XmlAttribute headerAttribute in layerHeaderElement.Attributes)
            {
                headerElement.SetAttribute(headerAttribute.Name, headerAttribute.Value);
   
            }
            
            rootNode.AppendChild(headerElement);
            
            //Combined DataStream
            currentLayerStreamElement.InnerXml = currentLayerStream;
            upperLayerStreamElement.InnerXml = upperLayerStream;
            combinedLayerDocument.DocumentElement.AppendChild(dataStreamElement);
            dataStreamElement.AppendChild(currentLayerStreamElement);
            dataStreamElement.AppendChild(upperLayerStreamElement);

            return combinedLayerDocument.InnerXml.ToString();
        }

        public static void FilterStream(string receivedLowerLayerStream,XmlElement headerElement,object[] dataStream)
        {
            XmlDocument receivedStreamDocument = new XmlDocument();
            receivedStreamDocument.LoadXml(receivedLowerLayerStream);

            XmlElement rootElement = receivedStreamDocument.DocumentElement;
            headerElement = (XmlElement)rootElement.SelectSingleNode("header");
            XmlElement dataStreamElement = (XmlElement)rootElement.SelectSingleNode("data");

            dataStream[0] = ((XmlElement)dataStreamElement.SelectSingleNode("currentLayer")).InnerXml;
            dataStream[1] = ((XmlElement)dataStreamElement.SelectSingleNode("upperLayer")).InnerXml;
            
        }
    }
}