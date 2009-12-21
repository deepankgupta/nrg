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
        public const string TERMINATE_CHAT_PACKET = "TCP";
        public const string RECEIPT_PACKET = "RP";
        public const string ROUTE_REQUEST_PACKET = "RREQ";

        //Elements of Packets in xml file
        public const string PACKET_TYPE = "TYPE";
        public const string BROADCAST_ID = "BID";
        public const string SOURCE_ID = "SRC";
        public const string CURRENT_ID = "CUR";
        public const string DESTINATION_ID = "DST";
        public const string SOURCES_SEQ_NUM = "S_SEQ";
        public const string DESTINATION_SEQ_NUM = "D_SEQ";
        public const string PAYLOAD = "MSG";
        public const string HOP_COUNT = "HOP";
        public const string LIFE_TIME = "LIFE_TIME";
        public const int EmptyInt = 0;
        public const string EmptyString = "~";
        public const string Infinity = "1000";


    }
}
