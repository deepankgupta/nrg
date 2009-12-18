using System;
using System.Collections;
using System.Xml;


namespace SmartDeviceApplication
{
    public class Node
    {

        public static string Id;
        public static string Name;
        public static int sequenceNumber;
        private int powerRange;
        private Point Position;
        struct  Point
        {
            public int xCoord;
            public int yCoord;
            public int zCoord;
        }
         
        public Node()
        {
            UtilityConfFile.Initialize();
            UtilityConfFile.FindNodeValuesInConfFile(ref Id, ref Name, ref powerRange, ref sequenceNumber);
        }       

    }

}