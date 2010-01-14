using System;
using System.Windows.Forms;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace SmartDeviceApplication
{
    public abstract class GeneralPacket
    {
        public string packetId;
        public string packetType;
        public string sourceId;
        public XmlDocument packetXmlDocument;

        public GeneralPacket() { }

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

    public class SessionLayerPacket : GeneralPacket
    {
        public SessionLayerPacket(PacketBuilder packetBuilder) 
        {
            packetId = packetBuilder.packetId;
            packetType = packetBuilder.packetType;
            sourceId = packetBuilder.sourceId;
        }

        public SessionLayerPacket() { }

        public class PacketBuilder
        {
            public string packetId;
            public string packetType;
            public string sourceId;

            public PacketBuilder()
            {
                packetId = string.Empty;
                packetType = string.Empty;
                sourceId = string.Empty;
            }

            public PacketBuilder SetPacketId(string _packetId)
            {
                packetId = _packetId;
                return this;
            }

            public PacketBuilder SetPacketType(string _packetType)
            {
                packetType = _packetType;
                return this;
            }

            public PacketBuilder SetSourceId(string _sourceId)
            {
                sourceId = _sourceId;
                return this;
            }

            public SessionLayerPacket BuildAll()
            {
                return new SessionLayerPacket(this);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static SessionLayerPacket TransformStreamToPacket(string receivedXmlStream)
        {
            SessionLayerPacket receivedPacket = new SessionLayerPacket();
            try
            {
                XmlNodeList childXmlNodes = allNodesInReceivedStream(receivedXmlStream);

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement tempXmlElement = (XmlElement)childNode;

                    //Type
                    if (tempXmlElement.Name.Equals(PacketConstants.PACKET_TYPE))
                    {
                        receivedPacket.packetType = tempXmlElement.FirstChild.Value;
                    }
                    //ID
                    else if (tempXmlElement.Name.Equals(PacketConstants.PACKET_ID))
                    {
                        receivedPacket.packetId = tempXmlElement.FirstChild.Value;
                    }
                    //sourceId
                    else if (tempXmlElement.Name.Equals(PacketConstants.SOURCE_ID))
                    {
                        receivedPacket.sourceId = tempXmlElement.FirstChild.Value;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Empty value Exception is occurred in transformXmlIntoPacket() : " + e.Message);
            }
            return receivedPacket;

        }
   
    }

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

    public class AodvPacket:RoutingProtocolPacket
    {
        public int lifeTime;
        public string activePath;

        public AodvPacket()
        {

        }

        public AodvPacket(PacketBuilder packetBuilder)
        {
            packetId = packetBuilder.packetId;
            hopCount = packetBuilder.hopCount;
            lifeTime = packetBuilder.lifeTime;
            packetType = packetBuilder.packetType;
            previousId = packetBuilder.previousId;
            sourceId = packetBuilder.sourceId;
            sourceSeqNum = packetBuilder.sourceSeqNum;
            destinationId = packetBuilder.destinationId;
            destinationSeqNum = packetBuilder.destinationSeqNum;
        }

        public class PacketBuilder
        {

            public string packetType;
            public String packetId;
            public string previousId;
            public string sourceId;
            public string destinationId;
            public int sourceSeqNum;
            public int destinationSeqNum;
            public string payloadMessage;
            public int hopCount;
            public int lifeTime;

            public PacketBuilder()
            {
                payloadMessage = PacketConstants.EmptyString;
                hopCount = PacketConstants.EmptyInt;
                packetId = String.Empty;
                lifeTime = PacketConstants.EmptyInt;
            }

            public PacketBuilder SetPacketType(string _packetType)
            {
                packetType = _packetType;
                return this;
            }

            public PacketBuilder SetPacketId(string _packetId)
            {
                packetId = _packetId;
                return this;
            }

            public PacketBuilder SetPreviousId(string _previousId)
            {
                previousId = _previousId;
                return this;
            }

            public PacketBuilder SetSourceId(string _sourceId)
            {
                sourceId = _sourceId;
                return this;
            }

            public PacketBuilder SetDestinationId(string _destinationId)
            {
                destinationId = _destinationId;
                return this;
            }

            public PacketBuilder SetSourceSeqNum(int _sourceSeqNum)
            {
                sourceSeqNum = _sourceSeqNum;
                return this;
            }

            public PacketBuilder SetDestinationSeqNum(int _destinationSeqNum)
            {
                destinationSeqNum = _destinationSeqNum;
                return this;
            }

            public PacketBuilder SetPayLoadMessage(string _payloadMessage)
            {
                payloadMessage = _payloadMessage;
                return this;
            }

            public PacketBuilder SetHopCount(int _hopCount)
            {
                hopCount = _hopCount;
                return this;
            }

            public PacketBuilder SetLifeTime(int _lifeTime)
            {
                lifeTime = _lifeTime;
                return this;
            }

            public AodvPacket BuildAll()
            {
                return new AodvPacket(this);
            }
        }

        // Convert Packet to XML stream
        public override string CreateStreamFromPacket()
        {
            base.CreateStreamFromPacket();

            XmlNode rootNode = packetXmlDocument.DocumentElement;

            // LifeTime
            XmlElement tempPacketElement = packetXmlDocument.CreateElement(PacketConstants.LIFE_TIME);
            AppendTextInXml(lifeTime.ToString(), tempPacketElement, rootNode);
  
            return packetXmlDocument.InnerXml.ToString();
        }

        //Convert XML to Packet 
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static AodvPacket TransformStreamToPacket(string receivedXmlStream)
        {
            AodvPacket receivedPacket = new AodvPacket();
            try
            {
                XmlNodeList childXmlNodes = allNodesInReceivedStream(receivedXmlStream);

                foreach (XmlNode childNode in childXmlNodes)
                {
                    XmlElement tempXmlElement = (XmlElement)childNode;

                    //Type
                    if (tempXmlElement.Name.Equals(PacketConstants.PACKET_TYPE))
                    {
                        receivedPacket.packetType = tempXmlElement.FirstChild.Value;
                    }
                    //ID
                    else if (tempXmlElement.Name.Equals(PacketConstants.PACKET_ID))
                    {
                        receivedPacket.packetId = tempXmlElement.FirstChild.Value;
                    }
                    //sourceId
                    else if (tempXmlElement.Name.Equals(PacketConstants.SOURCE_ID))
                    {
                        receivedPacket.sourceId = tempXmlElement.FirstChild.Value;
                    }
                    //DestinationID
                    else if (tempXmlElement.Name.Equals(PacketConstants.DESTINATION_ID))
                    {
                        receivedPacket.destinationId = tempXmlElement.FirstChild.Value;
                    }
                    //SourceSeqNum
                    else if (tempXmlElement.Name.Equals(PacketConstants.SOURCES_SEQ_NUM))
                    {
                        receivedPacket.sourceSeqNum = Convert.ToInt32(tempXmlElement.FirstChild.Value);
                    }
                    //DestinationSeqNum
                    else if (tempXmlElement.Name.Equals(PacketConstants.DESTINATION_SEQ_NUM))
                    {
                        receivedPacket.destinationSeqNum = Convert.ToInt32(tempXmlElement.FirstChild.Value);
                    }
                    //HopCount
                    else if (tempXmlElement.Name.Equals(PacketConstants.HOP_COUNT))
                    {
                        receivedPacket.hopCount = Convert.ToInt32(tempXmlElement.FirstChild.Value);
                    }
                    //LifeTime
                    else if (tempXmlElement.Name.Equals(PacketConstants.LIFE_TIME))
                    {
                        receivedPacket.lifeTime = Convert.ToInt32(tempXmlElement.FirstChild.Value);
                    }
                    //previousId
                    else if (tempXmlElement.Name.Equals(PacketConstants.PREVIOUS_ID))
                    {
                        receivedPacket.previousId = tempXmlElement.FirstChild.Value;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Empty value Exception is occurred in transformXmlIntoPacket() : " + e.Message);
            }
            return receivedPacket;

        }
    }
}
