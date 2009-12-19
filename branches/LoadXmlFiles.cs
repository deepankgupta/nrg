using System;
using System.Xml;

namespace SmartDeviceApplication
{
    public class LoadXmlFiles
    {
        public static readonly string ConfFile = System.IO.Path.GetDirectoryName
                         (System.Reflection.Assembly.GetExecutingAssembly()
                         .GetName().CodeBase) + "\\conf.xml";
        public static readonly string RouteFile = System.IO.Path.GetDirectoryName
                         (System.Reflection.Assembly.GetExecutingAssembly()
                         .GetName().CodeBase) + "\\RouteTable.xml";


        public static XmlDocument FindXmlDoc(string XmlFileName)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(XmlFileName);
            return xmlDoc;
        }

    }
}