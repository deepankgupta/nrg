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
    public partial class MessageApplicationForm : Form
    {
        // Data Members
        private Node nodeDeviceObect;
        private NetworkClass networkHelpingObject;
        private Thread ReceiverThread;

        //Member Functions
        public MessageApplicationForm()
        {
            InitializeComponent();
            try
            {
                networkHelpingObject = new NetworkClass();
                nodeDeviceObect = new Node();
                ReceiverThread =new Thread(new ThreadStart(networkHelpingObject.ReceiverThreadFunction));
            }
            catch (Exception e)
            {
                MessageBox.Show("SmartDevice Conf File() Exception is occurred: " + e.Message);
            }
        }

        //Write Message Event 
        private void menuItem2_Click(object sender, EventArgs e)
        {
            WriteMessageForm textMessageForm = new WriteMessageForm();
            textMessageForm.Show();
        }

        //Exit
        private void menuItem5_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

    }
}