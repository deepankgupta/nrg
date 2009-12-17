using System;
using System.Net.Sockets;
using System.Net;

namespace SmartDeviceApplication
{
    public class AodvProtocolClass
    {
        private int sourceSeqNum;
        private string requestId;
        private  string buddyId;
        private string sessionId;
        NetworkClass networkHelpingObject;


        public AodvProtocolClass(string buddyIdText)
        {
           
            buddyId = buddyIdText;
            requestId = "";
            sourceSeqNum = 0;
            networkHelpingObject = new NetworkClass();
            SetSessionId();

        }

        //TODO
        public void SetSessionId()
        {
            Random random = new Random();
            int randomSessonNum = random.Next(0, 100000);
            sessionId = randomSessonNum + ":" + Node.Id + ":" + buddyId;
        }

        //TODO
        public void SetRequestIdForPacket(string packetType)
        {

            if (packetType.Equals(PacketConstants.START_CHAT_PACKET))
            {
                requestId = sessionId + ":" + "0";
            }
            //else if (packetType.Equals(PacketConstants.ROUTE_DISCOVERY_PACKET))
            //{
            //    discoverSeqNum--;
            //    requestID = MySessionID + ":" + discoverSeqNum;
            //}
            else if (packetType.Equals(PacketConstants.DATA_PACKET))
            {
                sourceSeqNum++;
                requestId = sessionId + ":" + sourceSeqNum;
            }
  
        }

       
        public void SendMessage(string packetPayLoad)
        {
            try
            {
                int destinationSeqNum;
                int hopCount;
                int lifeTime;
                string payLoadXmlMessage;
                string destinationIpAddr;

                if (!RouterTableClass.IsDestinationPathEmpty(buddyId))
                {
                    Packet sendPacket = new Packet();
                    SetRequestIdForPacket(PacketConstants.START_CHAT_PACKET);
                    destinationIpAddr = UtilityConfFile.GetIPAddressByIDInConfFile(buddyId);
                    RouterTableClass.GetDestinationInfoFromRouteTable(buddyId,out destinationSeqNum,out hopCount,out lifeTime);         
                    
                    sendPacket.SetPacketInfo(PacketConstants.START_CHAT_PACKET,requestId,
                                            NetworkClass.IpAddress.ToString(),destinationIpAddr.ToString()
                                            ,sourceSeqNum,destinationSeqNum,packetPayLoad,hopCount,lifeTime);             
                    
                    payLoadXmlMessage=sendPacket.CreateMessageXmlstringFromPacket();
                    
                  
                }
                
            }
            catch (Exception Excep)
            {
            }
        }
    }
}