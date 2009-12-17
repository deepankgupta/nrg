using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Windows.Forms;


namespace SmartDeviceApplication
{
    public class Packet
    {
        public string packetType;
        public string packetId;
        public string sourceIpAddress;
        public string destinationIpAddress;
        public int sourceSeqNum;
        public int destinationSeqNum;
        public string payloadMessage;
        public int hopCount;
        public int lifeTime;
    
        //Set Packet Attributes 
        public void SetPacketInfo( string _packetType,string _packetId,string _sourceIp,
                                   string _destinationIp,int _sourceSeqNum,int _destinationSeqNum,
                                   string _payLoad,int _hopCount,int _lifeTime)
        {
            packetId = _packetId;
            hopCount = _hopCount;
            lifeTime = _lifeTime;
            packetType = _packetType;
            payloadMessage = _payLoad;
            sourceIpAddress = _sourceIp;
            sourceSeqNum = _sourceSeqNum;
            destinationIpAddress = _destinationIp;
            destinationSeqNum = _destinationSeqNum;         
        }

        // Convert Packet to XML stream
        public string CreateMessageXmlstringFromPacket()
        {
            XmlDocument packetXml = new XmlDocument();
            packetXml.LoadXml("<Packet></Packet>");

            XmlNode root = packetXml.DocumentElement;

            //1. Type
            XmlElement tempPacketElement = packetXml.CreateElement(PacketConstants.PACKET_TYPE);
            tempPacketElement.InnerText = packetType;
            root.AppendChild(tempPacketElement);

            //2. ID
            tempPacketElement = packetXml.CreateElement(PacketConstants.PACKET_ID);
            tempPacketElement.InnerText = packetId;
            root.AppendChild(tempPacketElement);

            //3. SourceIP
            tempPacketElement = packetXml.CreateElement(PacketConstants.SOURCE_IP);
            tempPacketElement.InnerText = sourceIpAddress;
            root.AppendChild(tempPacketElement);

            //4. DestinationIP
            tempPacketElement = packetXml.CreateElement(PacketConstants.DESTINATION_IP);
            tempPacketElement.InnerText = destinationIpAddress;
            root.AppendChild(tempPacketElement);

            //5. SourceSeqNum
            tempPacketElement = packetXml.CreateElement(PacketConstants.SOURCES_SEQ_NUM);
            tempPacketElement.InnerText = sourceSeqNum.ToString();
            root.AppendChild(tempPacketElement);

            //6. DestinationSeqNum
            tempPacketElement = packetXml.CreateElement(PacketConstants.DESTINATION_SEQ_NUM);
            tempPacketElement.InnerText = destinationSeqNum.ToString();
            root.AppendChild(tempPacketElement);

            //7. PayLoad
            tempPacketElement = packetXml.CreateElement(PacketConstants.PAYLOAD);
            tempPacketElement.InnerText = payloadMessage;
            root.AppendChild(tempPacketElement);

            //8. HopCount
            tempPacketElement = packetXml.CreateElement(PacketConstants.HOP_COUNT);
            tempPacketElement.InnerText = hopCount.ToString() ;
            root.AppendChild(tempPacketElement);

            //9. LifeTime
            tempPacketElement = packetXml.CreateElement(PacketConstants.LIFE_TIME);
            tempPacketElement.InnerText = lifeTime.ToString();
            root.AppendChild(tempPacketElement);

            return packetXml.InnerXml.ToString();
        }

        //Convert XML to Packet 
        public Packet TransformXmlMessageIntoPacket(string ReceivedXmlPacketMessage)
        {
                    
            Packet ReceivedPacket = new Packet();
            try
            {
 
                XmlDocument receivedXmlDoc = new XmlDocument();
                receivedXmlDoc.LoadXml(ReceivedXmlPacketMessage);


                XmlNode rootXmlNode = receivedXmlDoc.DocumentElement;
                XmlNodeList childXmlNodes = rootXmlNode.ChildNodes;

                foreach (XmlNode childNode in childXmlNodes)
                {

                    XmlElement tempXmlElement = (XmlElement)childNode;
                    
                    //Type
                    if (tempXmlElement.Name.Equals(PacketConstants.PACKET_TYPE))
                    {
                        ReceivedPacket.packetType = tempXmlElement.FirstChild.Value;
                    }
                    //ID
                    else if (tempXmlElement.Name.Equals(PacketConstants.PACKET_ID))
                    {
                        ReceivedPacket.packetId = tempXmlElement.FirstChild.Value;
                    }
                    //SourceIP
                    else if (tempXmlElement.Name.Equals(PacketConstants.SOURCE_IP))
                    {
                        ReceivedPacket.sourceIpAddress = tempXmlElement.FirstChild.Value;
                    }
                    //DestinationIP
                    else if (tempXmlElement.Name.Equals(PacketConstants.DESTINATION_IP))
                    {
                        ReceivedPacket.destinationIpAddress = tempXmlElement.FirstChild.Value;
                    }
                    //SourceSeqNum
                    else if (tempXmlElement.Name.Equals(PacketConstants.SOURCES_SEQ_NUM))
                    {
                        ReceivedPacket.sourceSeqNum= Convert.ToInt32( tempXmlElement.FirstChild.Value);
                    }
                    //DestinationSeqNum
                    else if (tempXmlElement.Name.Equals(PacketConstants.DESTINATION_SEQ_NUM))
                    {
                        ReceivedPacket.destinationSeqNum = Convert.ToInt32(tempXmlElement.FirstChild.Value);
                    }
                    //PayLoad
                    else if (tempXmlElement.Name.Equals(PacketConstants.PAYLOAD))
                    {
                        ReceivedPacket.payloadMessage= tempXmlElement.FirstChild.Value;
                    }
                    else if (tempXmlElement.Name.Equals(PacketConstants.HOP_COUNT))
                    {
                        ReceivedPacket.hopCount = Convert.ToInt32( tempXmlElement.FirstChild.Value);
                    }
                    else if (tempXmlElement.Name.Equals(PacketConstants.LIFE_TIME))
                    {
                        ReceivedPacket.lifeTime = Convert.ToInt32(tempXmlElement.FirstChild.Value);
                    }
                }
                    
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception is occurred in transformXmlIntoPacket() : " + e.Message);
            }
            return ReceivedPacket;
        
        }
    }
}