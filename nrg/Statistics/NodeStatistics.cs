using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Xml;
using System.Windows.Forms;

namespace StatisticAnalyser
{
    class NodeStatistics
    {
        private string _nodeId;
        private Hashtable _packetsHashTable=new Hashtable();

        private int _noOfDataPacketsSent = 0;
        private int _noOfRouteRepPacketsSent = 0;
        private int _noOfRouteErrPacketsSent = 0;
        private int _noOfStartChatPacketsSent = 0;
        private int _noOfHelloMsgPacketsSent = 0;
        private int _noOfReplyHelloMsgPacketsSent = 0;
        private int _noOfAcceptStartChatPacketsSent = 0;
        private int _noOfRejectStartChatPacketSent = 0;
        private int _noOfTerminateChatPacketSent = 0;
        private int _noOfReceiptPacketsSent = 0;
        private int _noOfRouteRequestPacketsSent = 0;

        private int _noOfDataPacketsReceived = 0;
        private int _noOfRouteRepPacketsReceived = 0;
        private int _noOfRouteErrPacketsReceived = 0;
        private int _noOfStartChatPacketsReceived = 0;
        private int _noOfHelloMsgPacketsReceived = 0;
        private int _noOfReplyHelloMsgPacketsReceived = 0;
        private int _noOfAcceptStartChatPacketsReceived = 0;
        private int _noOfRejectStartChatPacketReceived = 0;
        private int _noOfTerminateChatPacketReceived = 0;
        private int _noOfReceiptPacketsReceived = 0;
        private int _noOfRouteRequestPacketsReceived = 0;

        public Hashtable PacketHashTable
        {
            get
            {
                return _packetsHashTable;
            }
        }

        public string NodeId
        {
            get
            {
                return _nodeId;
            }
            set
            {
                _nodeId = value;
            }
        }

        public void LoadStatistics(XmlDocument xmlDocument)
        {
            foreach (XmlElement xmlElement in xmlDocument.DocumentElement.ChildNodes)
            {
                try
                {
                    _packetsHashTable.Add(xmlElement.SelectSingleNode("PID").InnerText, xmlElement);
                }
                catch (System.ArgumentNullException ex)
                {
                    MessageBox.Show(ex.Message);
                }
                catch (System.ArgumentException ex)
                {
                    MessageBox.Show(ex.Message);
                }
                catch (System.NotSupportedException ex)
                {
                    MessageBox.Show(ex.Message);
                }

                if (xmlElement.SelectSingleNode("PREV").InnerText.Equals(_nodeId))
                {
                    switch (xmlElement.SelectSingleNode("TYPE").InnerText)
                    {

                        case "DP":
                            _noOfDataPacketsSent++;
                            break;

                        case "RRP":
                            _noOfRouteRepPacketsSent++;
                            break;

                        case "REP":
                            _noOfRouteErrPacketsSent++;
                            break;

                        case "SCP":
                            _noOfStartChatPacketsSent++;
                            break;

                        case "HMSG":
                            _noOfHelloMsgPacketsSent++;
                            break;

                        case "RHMSG":
                            _noOfReplyHelloMsgPacketsSent++;
                            break;

                        case "ASCP":
                            _noOfAcceptStartChatPacketsSent++;
                            break;

                        case "RSCP":
                            _noOfRejectStartChatPacketSent++;
                            break;

                        case "TCP":
                            _noOfTerminateChatPacketSent++;
                            break;

                        case "RP":
                            _noOfReceiptPacketsSent++;
                            break;

                        case "RREQ":
                            _noOfRouteRequestPacketsSent++;
                            break;

                    }
                }
                else
                {
                    switch (xmlElement.SelectSingleNode("TYPE").InnerText)
                    {

                        case "DP":
                            _noOfDataPacketsReceived++;
                            break;

                        case "RRP":
                            _noOfRouteRepPacketsReceived++;
                            break;

                        case "REP":
                            _noOfRouteErrPacketsReceived++;
                            break;

                        case "SCP":
                            _noOfStartChatPacketsReceived++;
                            break;

                        case "HMSG":
                            _noOfHelloMsgPacketsReceived++;
                            break;

                        case "RHMSG":
                            _noOfReplyHelloMsgPacketsReceived++;
                            break;

                        case "ASCP":
                            _noOfAcceptStartChatPacketsReceived++;
                            break;

                        case "RSCP":
                            _noOfRejectStartChatPacketReceived++;
                            break;

                        case "TCP":
                            _noOfTerminateChatPacketReceived++;
                            break;

                        case "RP":
                            _noOfReceiptPacketsReceived++;
                            break;

                        case "RREQ":
                            _noOfRouteRequestPacketsReceived++;
                            break;

                    }


                }
            }



        }
    }
}
