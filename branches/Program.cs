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
            XmlFileUtility.Initialize();
            NetworkLayer networkLayer = NetworkLayer.networkLayerInstance;
            ReceiverThread = new Thread(new ThreadStart(networkLayer.
                                ReceiveMessageServerThread));
            ReceiverThread.Priority = ThreadPriority.Highest;
            ReceiverThread.Start();
            MessageApplicationForm messageForm = MessageApplicationForm.
                                                    messageFormInstance;
            Application.Run(messageForm);
            //TODO SET ROUTE TABLE TIMER
        }

        public static void TerminateAllThreads()
        {
            try
            {
                NetworkLayer networklayer = NetworkLayer.networkLayerInstance;
                SessionLayer sessionLayer = SessionLayer.sessionLayerInstance;
                
                Program.ReceiverThread.Abort();
                networklayer.ResetAll();
                sessionLayer.ResetAll();
                Application.Exit();
            }
            catch (Exception e)
            {
                MessageBox.Show("stop_server() exception occurred :" + e.Message);
            }
        }
    }
}