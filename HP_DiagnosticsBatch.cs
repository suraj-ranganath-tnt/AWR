///-----------------------------------------------------------------
///   Namespace:      Diagnostics
///   Class:          HP_DiagnosticsBatch
///   Description:    This windows form is used to pull metrics from HP Diagnostics and Generate Reports at 5 secs granularity
///   Author:         Suraj Ranganath <Date> : 09/11/2017 </Date>
///   Notes:          <Notes>
///   Revision History: 1.0
///   Name:           Date:        Description:
///-----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Globalization;
using static Diagnostics.DiagnosticsCustomClass; //Basic class which has functions used across both GUI and Console
using Diagnostics;


namespace Diagnostics
{
    public partial class HP_DiagnosticsBatch : Form
    {
        //declare all static variables
        static HttpClient client = new HttpClient();        
        List<DataTable> LstdtMetricsResult = new List<DataTable>();
        List<ProbeDetails> lstProbeData = new List<ProbeDetails>();
        List<string> lstServers = new List<string>();
        List<String> lstMetrics = new List<string>();
        List<String> lstProbeTypes = new List<string>();

        DataTable dtTable = new DataTable();
        

        public HP_DiagnosticsBatch()
        {                        
            InitializeComponent();

            //Change date picker format to accept date and time
            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.CustomFormat = "MM/dd/yyyy HH:mm:ss";

            dateTimePicker2.Format = DateTimePickerFormat.Custom;
            dateTimePicker2.CustomFormat = "MM/dd/yyyy HH:mm:ss";
            ChangeListViewSettings();

            //Fetch Server and Probe details from local SQL DB
            lstServers = FetchServers();
            lstProbeTypes = FetchProbeTypes();

            for (int i = 0; i < lstProbeTypes.Count(); i++)
            {
                cmBxProbType.Items.Add(lstProbeTypes[i]);
            }
            
        }
                
        /// <summary>
        /// Function to change display settings of ListView 
        /// </summary>
        public void ChangeListViewSettings()
        {
            lstBoxMetrics.Scrollable = true;
            lstBoxMetrics.View = View.Details;            
            lstBoxServerNames.Scrollable = true;
            lstBoxServerNames.View = View.Details;            

            ColumnHeader header = new ColumnHeader();
            header.Text = "Servers";
            header.Name = "Servers";
            lstBoxServerNames.Columns.Add(header);

            lstBoxServerNames.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            lstBoxServerNames.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            
            ColumnHeader header1 = new ColumnHeader();
            header1.Text = "Metrics";
            header1.Name = "Metrics";            
            lstBoxMetrics.Columns.Add(header1);
            lstBoxMetrics.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            lstBoxMetrics.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

        }

        /// <summary>
        /// Function to generate csv file. This is not being used for the current implementation.
        /// </summary>
        /// <param name="dataTable"> Data table containing time and values for server/metrics</param>
        /// <returns></returns>
        public bool DatatableToExcel(DataTable dataTable)
        {
            try
            {
                var lines = new List<string>();

                string[] columnNames = dataTable.Columns.Cast<DataColumn>().
                                                  Select(column => column.ColumnName).
                                                  ToArray();

                var header = string.Join(",", columnNames);
                lines.Add(header);

                var valueLines = dataTable.AsEnumerable()
                                   .Select(row => string.Join(",", row.ItemArray));
                lines.AddRange(valueLines);

                File.WriteAllLines("excel.csv", lines);
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// This function will generate the report based on values selected in the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGenerateReport_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;                
                string DateTimeStamp = DateTime.Now.Date.Day + "_" + DateTime.Now.Date.Month + "_" + DateTime.Now.Date.Year + "_" + DateTime.Now.TimeOfDay.Hours + "_" + DateTime.Now.TimeOfDay.Minutes + "_" + DateTime.Now.TimeOfDay.Seconds;
                // Basic validation before generating the report
                if (dateTimePicker1.Value >= dateTimePicker2.Value)
                {
                    MessageBox.Show("End Time must be greater than Start time");
                }
                else if (dateTimePicker1.Value.ToFileTime() >= DateTime.Now.ToFileTime()  || dateTimePicker2.Value.ToFileTime()>= DateTime.Now.ToFileTime())
                {
                    MessageBox.Show("Start Time or End time cannot be greater than current time");
                }
                else if (lstBoxServerNames.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Select Servers to generate report");
                }
                else if (lstBoxMetrics.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Select metrics to generate report");
                }
                else
                {
                    GenerateReport(DateTimeStamp);
                    MessageBox.Show("Report generated successfully. It can be found in path: "+ ConfigurationManager.AppSettings["ResultsFolderPath"].ToString() + @"\" +DateTimeStamp);
                }
                    
            }
            catch (Exception)
            {
                // Future implementation of logging to be done here.
                throw;
            }
            
        }

        /// <summary>
        /// Future implementation of progress bar to be done here. Basic code added to show progress.
        /// </summary>
        private void ProgressBar()
        {
            ProgressBar pBar = new ProgressBar();
            pBar.Dock = DockStyle.Bottom;
            pBar.Location = new System.Drawing.Point(20, 20);
            pBar.Name = "progressBar1";
            pBar.Width = 200;
            pBar.Height = 30;
            pBar.Minimum = 0;
            pBar.Maximum = 100;
            pBar.Value = 70;
        }
        

        private void cmBxProbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            lstBoxServerNames.Items.Clear();
            lstBoxMetrics.Items.Clear();
            lstServers = FetchServerBasedOnProbeTypes(cmBxProbType.SelectedItem.ToString());
            for (int i = 0; i < lstServers.Count(); i++)
            {
                lstBoxServerNames.Items.Add(lstServers[i]);
            }

            lstMetrics = FetchMetrics(cmBxProbType.SelectedItem.ToString());
            for (int i = 0; i < lstMetrics.Count(); i++)
            {
                lstBoxMetrics.Items.Add(lstMetrics[i]);
            }
        }

        /// <summary>
        /// This function is used to create the report based on values selected in UI
        /// </summary>
        /// <param name="DateTimeStamp"> This parameter will contain DateTimeStamp value which will be used during file creation </param>
        /// <returns></returns>
        public bool GenerateReport(string DateTimeStamp)
        {
            try
            {
                string ProbeGroup = string.Empty;
                string ProbeName = string.Empty;
                string ProbeType = string.Empty;
                string ServerName = string.Empty;

                List<String> metrics = new List<string>();

                var ServersSelected = lstBoxServerNames.SelectedItems;
                var MetricsSelected = lstBoxMetrics.SelectedItems;

                

                for (int iServerCount = 0; iServerCount < ServersSelected.Count; iServerCount++)
                {
                    lstProbeData = SelectData(ServersSelected[iServerCount].Text.ToString());
                    for (int iProbeCount = 0; iProbeCount < lstProbeData.Count(); iProbeCount++)
                    {                        
                        for (int iMetricsCount = 0; iMetricsCount < MetricsSelected.Count; iMetricsCount++)
                        {
                            FetchData(dateTimePicker1.Value.ToString(), dateTimePicker2.Value.ToString(), lstProbeData[iProbeCount].ProbeGroup, lstProbeData[iProbeCount].ProbeName, lstProbeData[iProbeCount].HostName, lstProbeData[iProbeCount].ProbeType, MetricsSelected[iMetricsCount].Text.ToString(), DateTimeStamp);                            
                        }

                    }
                }

                return true;


            }
            catch (Exception)
            {

                throw;
            }
            
        }

        /// <summary>
        /// This function will call the Diagnostics API and will create the file based on values retrived
        /// </summary>
        /// <param name="StartTime">Start time of report</param>
        /// <param name="EndTime">End time of report</param>
        /// <param name="ProbeGroup">Probe group name</param>
        /// <param name="ProbeName">Probe name</param>
        /// <param name="HostName">Server name</param>
        /// <param name="ProbeType">Type of Probe</param>
        /// <param name="Metrics">Metrics selected</param>
        /// <param name="DateTimeStamp">DateTimeStamp used to create file and folder</param>
        public static void FetchData(string StartTime, string EndTime, string ProbeGroup, string ProbeName, string HostName, string ProbeType, string Metrics, string DateTimeStamp)
        {
            try
            {
                List<DiagnosticsCustomClass.TimeValue> obj = new List<DiagnosticsCustomClass.TimeValue>();
                DiagnosticsCustomClass objDiag = new DiagnosticsCustomClass();
                DataGrid objDataGrid = new DataGrid();
                DataTable dt = new DataTable();
                
                //Fetch data grid based for 5 secs granularity 
                objDataGrid = objDiag.DivideGranularityTo5Mins(Convert.ToDateTime(StartTime), Convert.ToDateTime(EndTime), ProbeGroup, ProbeName, HostName, ProbeType, Metrics);
                                
                string FolderName = ConfigurationManager.AppSettings["ResultsFolderPath"].ToString() + @"\" + DateTimeStamp;
                string FileName = HostName + "_" + ProbeName + "_" + Metrics + "_" + DateTimeStamp + ".csv";
                bool exists = System.IO.Directory.Exists(FolderName);
                if (!exists)
                {
                    System.IO.Directory.CreateDirectory(FolderName);

                }

                //If the data retrived is null, exit the loop
                if (objDataGrid.DataSource == null)
                    goto exitloop;
                obj = (List<DiagnosticsCustomClass.TimeValue>)objDataGrid.DataSource;
                
                using (StreamWriter writer = new StreamWriter(FolderName + @"\" + FileName))
                {
                    for (int i = 0; i < obj.Count; i++)
                    {
                        writer.WriteLine(obj[i].Time + "," + obj[i].value);
                    }
                    
                }

                exitloop:;
                
            }
            catch (Exception)
            {

                throw;
            }
            
        }
    }
}
