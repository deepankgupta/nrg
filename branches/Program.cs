using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;

namespace SmartDeviceApplication
{
    static class Program
    {
        // <summary>
        /// The main entry point for the application
        /// and Loads Intial Network layer ,Application
        /// Layer information and manages other Threads
        /// as well .
        /// </summary>

        private static Thread ReceiverThread;

        [MTAThread]
        public static void Main()
        {
            NetworkLayer networkLayer = NetworkLayer.networkLayerInstance;
            ReceiverThread = new Thread(new ThreadStart(networkLayer.
                                ReceiveMessageServerThread));
            ReceiverThread.Start();
            MessageApplicationForm messageForm = MessageApplicationForm.
                                                    messageFormInstance;
            Application.Run(messageForm);
        }


        public static void TerminateAllThreads()
        {
            try
            {
                NetworkLayer networklayer = NetworkLayer.networkLayerInstance;
                networklayer.udpReceiverSocket.Close();
                Program.ReceiverThread.Abort();
                Application.Exit();
            }
            catch (Exception e)
            {
                MessageBox.Show("stop_server() exception occurred :" + e.Message);
            }
        }
    }
}