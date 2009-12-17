using System;
using System.Collections.Generic;
using System.Text;

namespace SmartDeviceApplication
{
    public class PacketConstants
    {
        
        public const string DATA_PACKET = "DP";
        public const string ROUTE_REPLY_PACKET = "RRP";
        public const string ROUTE_ERROR_PACKET = "REP";
        public const string START_CHAT_PACKET = "SCP";
        public const string ACCEPT_START_CHAT_PACKET = "ASCP";
        public const string REJECT_START_CHAT_PACKET = "RSCP";
        public const string RECEIPT_PACKET = "RP";
        public const string ROUTE_DISCOVERY_PACKET = "RDP";
        public const string REPLY_ROUTE_DISCOVERY_PACKET = "RRDP";
        public const string BROADCAST_NT_PACKET = "B";


        //Elements of Packets in xml file
        public const string PACKET_TYPE = "TYPE";
        public const string PACKET_ID = "PID";
        public const string SOURCE_IP = "SRC";
        public const string DESTINATION_IP = "DST";
        public const string SOURCES_SEQ_NUM = "S_SEQ";
        public const string DESTINATION_SEQ_NUM = "D_SEQ";
        public const string PAYLOAD = "MSG";
        public const string HOP_COUNT = "HOP";
        public const string LIFE_TIME = "LIFE_TIME";

    }
}
