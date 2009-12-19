
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Windows.Forms;


namespace SmartDeviceApplication
{

    /// <summary>
    /// Class for setting Packet Components 
    /// and Creating a new Packet 
    /// </summary>
    public class PacketBuilder
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

        public PacketBuilder()
        {
            payloadMessage = PacketConstants.EmptyString;
            hopCount = PacketConstants.EmptyInt;
            broadcastId = PacketConstants.EmptyInt;
            lifeTime = PacketConstants.EmptyInt;
        }

        public PacketBuilder setPacketType(string _packetType)
        {
            packetType = _packetType;
            return this;
        }
 
        public PacketBuilder setBroadcastId (int _broadcastId)
        {
            broadcastId = _broadcastId;
            return this;
        }
 
        public PacketBuilder setCurrentId (string _currentId)
        {
            currentId = _currentId;
            return this;
        }
 
        public PacketBuilder setSourceId (string _sourceId)
        {
            sourceId = _sourceId;
            return this;
        }
 
        public PacketBuilder setDestinationId(string _destinationId)
        {
            destinationId = _destinationId;
            return this;
        }
 
        public PacketBuilder setSourceSeqNum (int _sourceSeqNum)
        {
            sourceSeqNum = _sourceSeqNum;
            return this;
        }
 
        public PacketBuilder setDestinationSeqNum (int _destinationSeqNum)
        {
            destinationSeqNum = _destinationSeqNum;
            return this;
        }
 
        public PacketBuilder setPayLoadMessage (string _payloadMessage)
        {
            payloadMessage = _payloadMessage;
            return this;
        }
 
        public PacketBuilder setHopCount (int _hopCount)
        {
            hopCount = _hopCount;
            return this;
        }
 
        public PacketBuilder setLifeTime (int _lifeTime)
        {
            lifeTime = _lifeTime;
            return this;
        }
 
        public  Packet  build() 
        {
            return new Packet(this);
        }
    }
}


 