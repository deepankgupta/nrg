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
        public const string HELLO_MESSAGE = "HMSG";
        public const string REPLY_HELLO_MESSAGE = "RHMSG";
        public const string ACCEPT_START_CHAT_PACKET = "ASCP";
        public const string REJECT_START_CHAT_PACKET = "RSCP";
        public const string TERMINATE_CHAT_PACKET = "TCP";
        public const string RECEIPT_PACKET = "RP";
        public const string ROUTE_REQUEST_PACKET = "RREQ";

        //Elements of Packets in xml file
        public const string PACKET_TYPE = "TYPE";
        public const string PACKET_ID = "PID";
        public const string SOURCE_ID = "SRC";
        public const string PREVIOUS_ID = "PREV";
        public const string DESTINATION_ID = "DST";
        public const string SOURCES_SEQ_NUM = "S_SEQ";
        public const string DESTINATION_SEQ_NUM = "D_SEQ";
        public const string PAYLOAD = "MSG";
        public const string HOP_COUNT = "HOP";
        public const string TIME_TO_LIVE = "TTL";
        public const string LIFE_TIME = "LIFE_TIME";
        public const int EmptyInt = 0;
        public const string EmptyString = "~";
        public const string Infinity = "1000";
        public const string OneHop = "1";

        /*OTHER CONSTANTS*/
        public const string TIMER_EXPIRED = "Timer Expired !";
        public const string LINK_BREAK = "Link Break Occured !";
    }
}
