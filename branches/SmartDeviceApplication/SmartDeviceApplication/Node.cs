using System;
using System.Collections;
using System.Xml;


namespace SmartDeviceApplication
{
    public class Node
    {

        public static string Id;
        public static string Name;
        private int PowerRange;
        private Hashtable hashSequenceList;
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
            UtilityConfFile.FindValuesInXml(ref Id,ref Name,ref PowerRange);
        }       

    }

}