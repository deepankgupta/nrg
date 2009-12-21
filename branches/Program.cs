using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;

namespace SmartDeviceApplication
{
    static class Program
    {
        // <summary>
        /// The main entry point for the application.
        /// </summary>

        private static Thread ReceiverThread;

        [MTAThread]
        static void Main()
        {
            NetworkClass.InitializeIpAddress();
            Node.InitializeNode();
            ReceiverThread = new Thread(new ThreadStart(AodvProtocolClass.
                                           ReceiveMessageServerThread));
            ReceiverThread.Start();
            RouterTableClass.Initialize();
            AodvProtocolClass.Initialize();

        }
    }
}