using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Xml;
using System.IO;

namespace SmartDeviceApplication
{
    public class Node
    {
        //Data Members 
        private string myId;
        private string myName;
        private string buddyId;
        private int myPowerRange;
        private Hashtable hashSequenceList;
        private Point myPosition;
        struct  Point
        {
            public int xCoord;
            public int yCoord;
        }
         

        //Member Functions
        public Node()
        {
            UtilityConfFile.Initialize();
            myId = UtilityConfFile.FindIdByIpAddressInConfFile(NetworkClass.myIpAddress.ToString());
            myName = UtilityConfFile.GetNameByIdInConfFile(myId);
            myPowerRange = UtilityConfFile.GetPowerRangeInConfFile(myId);
            //hashSequenceList = UtilityConfFile.SetHashSequenceList(myId,ref hashSequenceList);
        }       

    }

}