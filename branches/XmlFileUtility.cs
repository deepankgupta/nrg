using System;
using System.Xml;

namespace SmartDeviceApplication
{
    /// <summary>
    /// Class for storing XML Files Path 
    /// and Loading XML Files  
    /// </summary>

    public class XmlFileUtility
    {
        public static readonly string pathName = System.IO.Path.GetDirectoryName
                         (System.Reflection.Assembly.GetExecutingAssembly()
                         .GetName().CodeBase);
        public static readonly string ConfFile = pathName + "\\conf.xml";
        public static readonly string RouteFile = pathName + "\\RouteTable.xml";


        public static XmlDocument FindXmlDoc(string XmlFileName)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(XmlFileName);
            return xmlDoc;
        }

    }
}