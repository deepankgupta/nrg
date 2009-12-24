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
            TransportLayer transportLayer = TransportLayer.transportLayerInstance;
            transportLayer.routeTimer = new TransportLayer.RouteTimer(TimerConstants.ROUTE_TIMER);
            transportLayer.routeTimer.SetTimer();

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
                TransportLayer transportLayer = TransportLayer.transportLayerInstance;

                transportLayer.routeTimer.ReleaseTimer();
                transportLayer.dataPacketTimer.ReleaseTimer();
                Program.ReceiverThread.Abort();
                networklayer.udpReceiverSocket.Close();
                Application.Exit();
            }
            catch (Exception e)
            {
                MessageBox.Show("stop_server() exception occurred :" + e.Message);
            }
        }
    }
}