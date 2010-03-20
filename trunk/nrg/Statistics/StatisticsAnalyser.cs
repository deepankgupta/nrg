using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace StatisticAnalyser
{
    
    public partial class StatisticsAnalyser : Form
    {
        
        private ArrayList _nodeStatsList = new ArrayList();


        public StatisticsAnalyser()
        {
            InitializeComponent();
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog fDialog = new OpenFileDialog();
            fDialog.Title = "Open the statistics Xml";
            fDialog.Filter = "XML Files|*.xml";
            fDialog.InitialDirectory = @"C:\";
            fDialog.AddExtension = true;
            fDialog.CheckFileExists = true;
            fDialog.CheckPathExists = true;

            if (fDialog.ShowDialog() == DialogResult.OK)
            {

                XmlDocument xmlStatisticsDocument = new XmlDocument();
                NodeStatistics nodeStatistics = new NodeStatistics();
               
                _nodeStatsList.Add(nodeStatistics); 
                try
                {
                    xmlStatisticsDocument.Load(fDialog.FileName);
                }
                catch (System.Xml.XmlException ex)
                {
                    if (ex.Message != string.Empty)
                        MessageBox.Show("Unable to load Statistics Xml file :" + ex.Message, "StatisticsAnalyser", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                        MessageBox.Show("Unable to load Statistics Xml file because of some error.", "StatisticsAnalyser", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    throw;
                }
                nodeStatistics.NodeId = fDialog.SafeFileName.Substring(0, 1);
                nodeStatistics.LoadStatistics(xmlStatisticsDocument);
                
            }
        }


    }
}
