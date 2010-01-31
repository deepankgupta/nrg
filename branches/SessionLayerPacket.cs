using System;
using System.Windows.Forms;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace SmartDeviceApplication
{
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
}
