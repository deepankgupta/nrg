using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Windows.Forms;


namespace SmartDeviceApplication
{

    /// <summary>
    /// Packet Class for creating new Packets,
    /// Transforming packets to XML Stream and Vice versa.
    /// </summary>
    public class Packet
    {
        public string packetType;
        public int broadcastId;
        public string currentId;
        public string sourceId;
        public string destinationId;
        public int sourceSeqNum;
        public int destinationSeqNum;
        public string payloadMessage;
        public int hopCount;
        public int lifeTime;


        public Packet()
        {

        }

        public Packet(PacketBuilder packetBuilder)
        {
            broadcastId = packetBuilder.broadcastId;
            hopCount = packetBuilder.hopCount;
            lifeTime = packetBuilder.lifeTime;
            packetType = packetBuilder.packetType;
            payloadMessage = packetBuilder.payloadMessage;
            currentId = packetBuilder.currentId;
            sourceId = packetBuilder.sourceId;
            sourceSeqNum = packetBuilder.sourceSeqNum;
            destinationId = packetBuilder.destinationId;
            destinationSeqNum = packetBuilder.destinationSeqNum;
        }

        public void AppendTextInXml(string text, XmlElement tempElement, XmlNode root)
        {
            tempElement.InnerText = text;
            root.AppendChild(tempElement);
        }

        // Convert Packet to XML stream
        public string CreateMessageXmlstringFromPacket()
        {
            XmlDocument packetXml = new XmlDocument();
            try
            {
                packetXml.LoadXml("<Packet></Packet>");

                XmlNode root = packetXml.DocumentElement;

                //1. Type
                XmlElement tempPacketElement = packetXml.CreateElement(PacketConstants.PACKET_TYPE);
                AppendTextInXml(packetType, tempPacketElement, root);

                //2. ID
                tempPacketElement = packetXml.CreateElement(PacketConstants.BROADCAST_ID);
                AppendTextInXml(broadcastId.ToString(), tempPacketElement,root);

                //3. SourceId
                tempPacketElement = packetXml.CreateElement(PacketConstants.SOURCE_ID);
                AppendTextInXml(sourceId, tempPacketElement, root);

                //4. DestinationID
                tempPacketElement = packetXml.CreateElement(PacketConstants.DESTINATION_ID);
                AppendTextInXml(destinationId, tempPacketElement, root);

                //5. SourceSeqNum
                tempPacketElement = packetXml.CreateElement(PacketConstants.SOURCES_SEQ_NUM);
                AppendTextInXml(sourceSeqNum.ToString(), tempPacketElement, root);

                //6. DestinationSeqNum
                tempPacketElement = packetXml.CreateElement(PacketConstants.DESTINATION_SEQ_NUM);
                AppendTextInXml(destinationSeqNum.ToString(), tempPacketElement, root);

                //7. PayLoad
                tempPacketElement = packetXml.CreateElement(PacketConstants.PAYLOAD);
                AppendTextInXml(payloadMessage, tempPacketElement, root);

                //8. HopCount
                tempPacketElement = packetXml.CreateElement(PacketConstants.HOP_COUNT);
                AppendTextInXml(hopCount.ToString(), tempPacketElement, root);

                //9. LifeTime
                tempPacketElement = packetXml.CreateElement(PacketConstants.LIFE_TIME);
                AppendTextInXml(lifeTime.ToString(), tempPacketElement, root);

                //10. CurrentID
                tempPacketElement = packetXml.CreateElement(PacketConstants.CURRENT_ID);
                AppendTextInXml(currentId, tempPacketElement, root);

            }
            catch (Exception e)
            {
                MessageBox.Show("Exception is occurred in CreateMessageXmlstringFromPacket() : " + e.Message);
            }

            return packetXml.InnerXml.ToString();
        }

        //Convert XML to Packet 
        public static Packet TransformXmlMessageIntoPacket(string ReceivedXmlPacketMessage)
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
                    else if (tempXmlElement.Name.Equals(PacketConstants.BROADCAST_ID))
                    {
                        ReceivedPacket.broadcastId = Convert.ToInt32(tempXmlElement.FirstChild.Value);
                    }
                    //sourceId
                    else if (tempXmlElement.Name.Equals(PacketConstants.SOURCE_ID))
                    {
                        ReceivedPacket.sourceId = tempXmlElement.FirstChild.Value;
                    }
                    //DestinationID
                    else if (tempXmlElement.Name.Equals(PacketConstants.DESTINATION_ID))
                    {
                        ReceivedPacket.destinationId = tempXmlElement.FirstChild.Value;
                    }
                    //SourceSeqNum
                    else if (tempXmlElement.Name.Equals(PacketConstants.SOURCES_SEQ_NUM))
                    {
                        ReceivedPacket.sourceSeqNum = Convert.ToInt32(tempXmlElement.FirstChild.Value);
                    }
                    //DestinationSeqNum
                    else if (tempXmlElement.Name.Equals(PacketConstants.DESTINATION_SEQ_NUM))
                    {
                        ReceivedPacket.destinationSeqNum = Convert.ToInt32(tempXmlElement.FirstChild.Value);
                    }
                    //PayLoad
                    else if (tempXmlElement.Name.Equals(PacketConstants.PAYLOAD))
                    {
                        ReceivedPacket.payloadMessage = tempXmlElement.FirstChild.Value;
                    }
                    //HopCount
                    else if (tempXmlElement.Name.Equals(PacketConstants.HOP_COUNT))
                    {
                        ReceivedPacket.hopCount = Convert.ToInt32(tempXmlElement.FirstChild.Value);
                    }
                    //LifeTime
                    else if (tempXmlElement.Name.Equals(PacketConstants.LIFE_TIME))
                    {
                        ReceivedPacket.lifeTime = Convert.ToInt32(tempXmlElement.FirstChild.Value);
                    }
                    //CurrentId
                    else if (tempXmlElement.Name.Equals(PacketConstants.CURRENT_ID))
                    {
                        ReceivedPacket.currentId = tempXmlElement.FirstChild.Value;
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