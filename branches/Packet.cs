using System;
using System.Windows.Forms;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace SmartDeviceApplication
{

    /// <summary>
    /// Base class for different packet classes
    /// </summary>
    public abstract class GeneralPacket
    {
        public string packetId;
        public string packetType;
        public string sourceId;
        public XmlDocument packetXmlDocument;

        public void AppendTextInXml(string text, XmlElement tempElement, XmlNode rootNode)
        {
            tempElement.InnerText = text;
            rootNode.AppendChild(tempElement);
        }

        public virtual string CreateStreamFromPacket()
        {
            packetXmlDocument = new XmlDocument();
            packetXmlDocument.LoadXml("<Packet></Packet>");
            XmlNode rootNode = packetXmlDocument.DocumentElement;

            //1. Type
            XmlElement tempPacketElement = packetXmlDocument.CreateElement(PacketConstants.PACKET_TYPE);
            AppendTextInXml(packetType, tempPacketElement, rootNode);

            //2. ID
            tempPacketElement = packetXmlDocument.CreateElement(PacketConstants.PACKET_ID);
            AppendTextInXml(packetId.ToString(), tempPacketElement, rootNode);

            //3. SourceId
            tempPacketElement = packetXmlDocument.CreateElement(PacketConstants.SOURCE_ID);
            AppendTextInXml(sourceId, tempPacketElement, rootNode);

            return packetXmlDocument.InnerXml.ToString();
        }

        public static XmlNodeList allNodesInReceivedStream(string receivedXmlStream)
        {
            XmlDocument receivedXmlDoc = new XmlDocument();
            receivedXmlDoc.LoadXml(receivedXmlStream);
            XmlNode rootXmlNode = receivedXmlDoc.DocumentElement;
            return rootXmlNode.ChildNodes;
        }
    }

    /// <summary>
    /// Base class for Routing Protocol Packets
    /// </summary>
    public abstract class RoutingProtocolPacket : GeneralPacket
    {
        public string previousId;
        public string destinationId;
        public int sourceSeqNum;
        public int destinationSeqNum;
        public int hopCount;
        public int timeToLive;

        public override string CreateStreamFromPacket()
        {
            base.CreateStreamFromPacket();

            XmlNode rootNode = packetXmlDocument.DocumentElement;

            //DestinationID
            XmlElement tempPacketElement = packetXmlDocument.CreateElement(PacketConstants.DESTINATION_ID);
            AppendTextInXml(destinationId, tempPacketElement, rootNode);

            //SourceSeqNum
            tempPacketElement = packetXmlDocument.CreateElement(PacketConstants.SOURCES_SEQ_NUM);
            AppendTextInXml(sourceSeqNum.ToString(), tempPacketElement, rootNode);

            //DestinationSeqNum
            tempPacketElement = packetXmlDocument.CreateElement(PacketConstants.DESTINATION_SEQ_NUM);
            AppendTextInXml(destinationSeqNum.ToString(), tempPacketElement, rootNode);

            //HopCount
            tempPacketElement = packetXmlDocument.CreateElement(PacketConstants.HOP_COUNT);
            AppendTextInXml(hopCount.ToString(), tempPacketElement, rootNode);

            //previousId
            tempPacketElement = packetXmlDocument.CreateElement(PacketConstants.PREVIOUS_ID);
            AppendTextInXml(previousId, tempPacketElement, rootNode);

            //TimeToLive
            tempPacketElement = packetXmlDocument.CreateElement(PacketConstants.TIME_TO_LIVE);
            AppendTextInXml(timeToLive.ToString(), tempPacketElement, rootNode);

            return packetXmlDocument.InnerXml.ToString();
        }
    }
}