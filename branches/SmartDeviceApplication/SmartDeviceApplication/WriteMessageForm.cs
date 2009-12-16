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
    public partial class WriteMessageForm : Form
    {

        private Hashtable neighbourFriendList;
        
        public WriteMessageForm()
        {
            InitializeComponent();
            neighbourFriendList = new Hashtable();
        }

        private void menuItem2_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void WriteMessageForm_Load(object sender, EventArgs e)
        {


        }

    }
}