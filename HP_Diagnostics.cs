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
//using excel = Microsoft.office.interop.excel;

namespace Diagnostics
{
    public partial class HP_Diagnostics : Form
    {
        static HttpClient client = new HttpClient();
        //static bool FetchAllServerDetails = false;
        List<DataTable> LstdtMetricsResult = new List<DataTable>();
        DataTable dtTable = new DataTable();
        DataRow drRow;

        public HP_Diagnostics()
        {                        
            InitializeComponent();
            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.CustomFormat = "MM/dd/yyyy HH:mm:ss";

            dateTimePicker2.Format = DateTimePickerFormat.Custom;
            dateTimePicker2.CustomFormat = "MM/dd/yyyy HH:mm:ss";

            AddServerToDDL();
        }

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
            catch (Exception ex)
            {
                throw;
            }
        }

        private void btnGenerateReport_Click(object sender, EventArgs e)
        {

            string ProbeGroup = string.Empty;//ConfigurationManager.AppSettings["ProbeGroup"].Split(',')[cmBxServerName.SelectedIndex];
            string ProbeName = string.Empty;// ConfigurationManager.AppSettings["ProbeName"].Split(',')[cmBxServerName.SelectedIndex];
            string ProbeType = string.Empty;
            string ServerName = string.Empty;

            //if (chBxGranularity.Checked)
            //{
            //    ProbeGroup = ConfigurationManager.AppSettings["ProbeGroup"].Split(',')[cmBxServerName.SelectedIndex];
            //    ProbeName = ConfigurationManager.AppSettings["ProbeName"].Split(',')[cmBxServerName.SelectedIndex];
            //    DivideGranularityTo5Mins(dateTimePicker1.Value, dateTimePicker2.Value,ProbeGroup,ProbeName,cmBxServerName.SelectedItem.ToString(),cmBxProbType.SelectedItem.ToString(),cmBxMetrics.SelectedItem.ToString());
            //}

            if (chBxAllServers.Checked)
            {
                
                if (chBxGranularity.Checked == true)
                {
                    List<String> metrics = new List<string>();
                    for (int ServerCount = 0; ServerCount < ConfigurationManager.AppSettings["ServerName"].Split(',').Count(); ServerCount++)
                    {
                        ProbeType = ConfigurationManager.AppSettings["ProbType"].Split(',')[ServerCount];
                        ProbeGroup = ConfigurationManager.AppSettings["ProbeGroup"].Split(',')[ServerCount];
                        ProbeName = ConfigurationManager.AppSettings["ProbeName"].Split(',')[ServerCount];
                        ServerName= ConfigurationManager.AppSettings["ServerName"].Split(',')[ServerCount];
                        if (ProbeType == "Java")
                            metrics = JavaMetricsArray();
                        else if (ProbeType == "MQ")
                            metrics = MQMetricsArray();
                        else if (ProbeType == "Oracle")
                            metrics = OracleMetricsArray();

                        for (int MetricsCount = 0; MetricsCount < metrics.Count(); MetricsCount++)
                        {
                            DivideGranularityTo5Mins(dateTimePicker1.Value, dateTimePicker2.Value, ProbeGroup, ProbeName, ServerName, ProbeType , metrics[MetricsCount],true);
                        }
                    }
                    DatatableToExcel(dtTable);
                }
            }
            else
            {

                /*
                // Create Request
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(@"http://gblabl32:2006/query/?action=trend&granularity=[name='5m',start='9223372036854775807',end='9223372036854775807']&response_format=excel&path=%2Fgroupby%5Bname%3D'Default%20Client%3A'%5D%2Fprobegroup%5Bname%3D'ACAS_profiling'%5D%2Fprobe%5Bname%3D'gblabvl281_ACAS_server1'%2ChostName%3D'gblabvl281.gb.tntpost.com'%2CsystemGroup%3D'Default'%2CprobeType%3D'Java'%5D%2Fmetric[name='Availability',type='average']");

                // Create Client
                WebClient client = new WebClient();

                // Assign Credentials
                client.Credentials = new NetworkCredential("admin", "admin");

                // Grab Data
                //string htmlCode = client.DownloadString(@"http://gblabl32:2006/query/?action=trend&granularity=[name='5m',start='9223372036854775807',end='9223372036854775807']&response_format=excel&path=%2Fgroupby%5Bname%3D'Default%20Client%3A'%5D%2Fprobegroup%5Bname%3D'ACAS_profiling'%5D%2Fprobe%5Bname%3D'gblabvl281_ACAS_server1'%2ChostName%3D'gblabvl281.gb.tntpost.com'%2CsystemGroup%3D'Default'%2CprobeType%3D'Java'%5D%2Fmetric[name='ProcessCpuUtil',type='average']");

                string ProbeGroup = ConfigurationManager.AppSettings["ProbeGroup"].Split(',')[cmBxServerName.SelectedIndex];
                string ProbeName = ConfigurationManager.AppSettings["ProbeName"].Split(',')[cmBxServerName.SelectedIndex];

                //9223372036854775807 - "http://gblabl32:2006/query/?action=trend&granularity=[name='5m',start='"+ ((dateTimePicker1.Value.ToOADate() - 25569) * 60 * 60 * 24 * 1000).ToString() + "',end='" + ((dateTimePicker2.Value.ToOADate() - 25569) * 60 * 60 * 24 * 1000).ToString() + "']&response_format=excel&path=%2Fgroupby%5Bname%3D'Default%20Client%3A'%5D%2Fprobegroup%5Bname%3D'ACAS_profiling'%5D%2Fprobe%5Bname%3D'gblabvl281_ACAS_server1'%2ChostName%3D'gblabvl281.gb.tntpost.com'%2CsystemGroup%3D'Default'%2CprobeType%3D'"+cmBxProbType.SelectedItem.ToString()+"'%5D%2Fmetric[name='"+cmBxMetrics.SelectedItem.ToString()+"',type='average']"
                string url = @"http://gblabl32:2006/query/?action=trend&granularity=[name='5m',start='9223372036854775807',end='9223372036854775807']&response_format=excel&path=%2Fgroupby%5Bname%3D'Default%20Client%3A'%5D%2Fprobegroup%5Bname%3D'ACAS_profiling'%5D%2Fprobe%5Bname%3D'gblabvl281_ACAS_server1'%2ChostName%3D'gblabvl281.gb.tntpost.com'%2CsystemGroup%3D'Default'%2CprobeType%3D'" + cmBxProbType.SelectedItem.ToString() + "'%5D%2Fmetric[name='" + cmBxMetrics.SelectedItem.ToString() + "',type='average']";
                url = "http://gblabl32:2006/query/?action=trend&granularity=[name='5m',start='" + ((dateTimePicker1.Value.ToOADate() - 25569) * 60 * 60 * 24 * 1000).ToString() + "',end='" + ((dateTimePicker2.Value.ToOADate() - 25569) * 60 * 60 * 24 * 1000).ToString() + "']&response_format=excel&path=%2Fgroupby%5Bname%3D'Default%20Client%3A'%5D%2Fprobegroup%5Bname%3D'"+ ProbeGroup + "'%5D%2Fprobe%5Bname%3D'"+ ProbeName + "'%2ChostName%3D'"+ cmBxServerName.SelectedItem.ToString() +"'%2CsystemGroup%3D'Default'%2CprobeType%3D'" + cmBxProbType.SelectedItem.ToString() + "'%5D%2Fmetric[name='" + cmBxMetrics.SelectedItem.ToString() + "',type='average']";
                string htmlCode = client.DownloadString(url);

                // Display Data
                richTextBox1.Text  = htmlCode;
                ConvertStringToTable(htmlCode);

                */
            }
        }

        //yet to complete
        public bool DivideGranularityTo5Mins(DateTime StartTime,DateTime EndTime,string ProbeGroupName, string ProbeName,string ServerName, string ProbeType, string metrics,bool FetchAllServer=false)
        {

            try
            {
                List<string> TimeInterval = new List<string>();
                DateTime start = DateTime.Parse(StartTime.ToString());
                DateTime end = DateTime.Parse(EndTime.ToString());
                List<TimeValue> ObjList = new List<TimeValue>();
                TimeValue ObjValue = new TimeValue();
                string htmlCode;
                string htmlResponse = string.Empty;
                // Create Request
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(@"http://gblabl32:2006/query/?action=trend&granularity=[name='5m',start='9223372036854775807',end='9223372036854775807']&response_format=excel&path=%2Fgroupby%5Bname%3D'Default%20Client%3A'%5D%2Fprobegroup%5Bname%3D'ACAS_profiling'%5D%2Fprobe%5Bname%3D'gblabvl281_ACAS_server1'%2ChostName%3D'gblabvl281.gb.tntpost.com'%2CsystemGroup%3D'Default'%2CprobeType%3D'Java'%5D%2Fmetric[name='Availability',type='average']");
                // Create Client
                WebClient client = new WebClient();
                // Assign Credentials
                client.Credentials = new NetworkCredential("admin", "admin");


                while (end > start)
                {
                    TimeInterval.Add(start.ToString("h.mmtt", CultureInfo.InvariantCulture));
                    var url = "http://gblabl32:2006/query/?action=trend&granularity=[name='5m',start='" + ((start.ToOADate() - 25569) * 60 * 60 * 24 * 1000).ToString() + "',end='" + ((start.AddMinutes(5).ToOADate() - 25569) * 60 * 60 * 24 * 1000).ToString() + "']&response_format=excel&path=%2Fgroupby%5Bname%3D'Default%20Client%3A'%5D%2Fprobegroup%5Bname%3D'" + ProbeGroupName + "'%5D%2Fprobe%5Bname%3D'" + ProbeName + "'%2ChostName%3D'" + ServerName + "'%2CsystemGroup%3D'Default'%2CprobeType%3D'" + ProbeType + "'%5D%2Fmetric[name='" + metrics + "',type='average']";
                    htmlCode = client.DownloadString(url);


                    int numLines = htmlCode.Split('\n').Length;
                    for (int i = 4; i < numLines - 1; i++)
                    {
                        ObjValue = new TimeValue();
                        ObjValue.Time = DateTime.FromOADate(Convert.ToDouble(htmlCode.Split('\n')[i].Split('\t')[0])).ToString();
                        ObjValue.value = Convert.ToString(Convert.ToDecimal((htmlCode.Split('\n')[i].Split('\t')[1])) * 100);
                        ObjList.Add(ObjValue);


                    }

                    start = start.AddMinutes(5);
                    htmlResponse = string.Concat(htmlCode);

                }


                if (dtTable.Columns.Count== 0)
                {
                    dtTable.Columns.Add("DateTime", Type.GetType("System.String"));
                    dtTable.Columns.Add("Value", Type.GetType("System.String"));
                }
                drRow = dtTable.NewRow();
                if (FetchAllServer)
                {
                    for (int i = 0; i < ObjList.Count(); i++)
                    {
                        drRow["DateTime"] = ObjList[i].Time.ToString();
                        drRow["Value"] = ObjList[i].value.ToString();
                        //dtTable.Rows.Add(ObjList[i].Time +" " + ObjList[i].value);
                        
                    }
                    LstdtMetricsResult.Add(dtTable);
                }

                dataGridView1.DataSource = ObjList;
                dataGridView1.Columns[0].HeaderText = "Date Time";
                dataGridView1.Columns[1].HeaderText = "Value";

                //TimeSpan Difference = DateTime.Parse(EndTime.ToString()).Subtract(DateTime.Parse(StartTime.ToString()));

                richTextBox1.Text = htmlResponse;
                
                              
                return true;
            }
            catch (Exception)
            {

                return false;
                
            }
        }

        //function to get all server details

        public bool GetAllServerDetails()
        {
            try
            {
                double StartTime = dateTimePicker1.Value.ToOADate();
                double EndTime = dateTimePicker1.Value.ToOADate();
                string ProbeGroup = ConfigurationManager.AppSettings["ProbeGroup"].Split(',')[cmBxServerName.SelectedIndex];
                string ProbeName = ConfigurationManager.AppSettings["ProbeName"].Split(',')[cmBxServerName.SelectedIndex];

                if (chBxGranularity.Checked==true)
                {

                }
                return true;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void ConvertStringToTable(String Input)
        {
            List<TimeValue> ObjList = new List<TimeValue>();
            TimeValue ObjValue = new TimeValue();
            
            int numLines = Input.Split('\n').Length;
            for (int i = 4; i < numLines-1 ; i++)
            {
                ObjValue = new TimeValue();
                ObjValue.Time = DateTime.FromOADate(Convert.ToDouble(Input.Split('\n')[i].Split('\t')[0])).ToString();                
                ObjValue.value= Convert.ToString(Convert.ToDecimal((Input.Split('\n')[i].Split('\t')[1])) * 100);
                ObjList.Add(ObjValue);
            }
            dataGridView1.DataSource = ObjList;
            dataGridView1.Columns[0].HeaderText = "Date Time";
            dataGridView1.Columns[1].HeaderText = "Value";
        }

        public class TimeValue
        {
            public string Time { get; set; }
            public string value { get; set; }
        }


        private string epoch2string(int epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epoch).ToShortDateString();
        }

        public bool AddServerToDDL()
        {
            
                
            int NoOfServers = ConfigurationManager.AppSettings["ServerName"].Split(',').Count();
            for (int i = 0; i < NoOfServers; i++)
            {
                cmBxServerName.Items.Add(ConfigurationManager.AppSettings["ServerName"].Split(',')[i]);
            }

             NoOfServers = ConfigurationManager.AppSettings["ProbType"].Split(',').Count();
            for (int i = 0; i < NoOfServers; i++)
            {
                cmBxProbType.Items.Add(ConfigurationManager.AppSettings["ProbType"].Split(',')[i]);
            }

            
            return true;
        }

        private void cmBxServerName_SelectedIndexChanged(object sender, EventArgs e)
        {
            int SelectedIndex = cmBxServerName.SelectedIndex;
            cmBxProbType.SelectedIndex = SelectedIndex;

            string Metrics = null;
            if (cmBxProbType.SelectedItem.ToString() == "Java")
            {
                cmBxMetrics.Items.Clear();
                Metrics = JavaMetrics();
                for (int i = 0; i < Metrics.Split(',').Count(); i++)
                {
                    cmBxMetrics.Items.Add(Metrics.Split(',')[i]);
                }

            }
            else if (cmBxProbType.SelectedItem.ToString() == "Oracle")
            {
                cmBxMetrics.Items.Clear();
                Metrics = OracleMetrics();
                for (int i = 0; i < Metrics.Split(',').Count(); i++)
                {
                    cmBxMetrics.Items.Add(Metrics.Split(',')[i]);
                }
            }
            else if (cmBxProbType.SelectedItem.ToString() == "MQ")
            {
                cmBxMetrics.Items.Clear();
                Metrics = MQMetrics();
                for (int i = 0; i < Metrics.Split(',').Count(); i++)
                {
                    cmBxMetrics.Items.Add(Metrics.Split(',')[i]);
                }
            }
        }

        public string JavaMetrics()
        {
            return @"Availability,Classes Loaded,collection_leak_count,CompilationTime,EJB Ready Beans,EJB Response Time,EJB Total Method Calls,FileBytesInPerSec,FileBytesOutPerSec,GC Time Spent in Collections,HeapFree,HeapTotal,HeapUsed,HeapUsedPct,HeapUtilization,J2C Use Time,J2C Wait Time,J2C Waiting Thread Count,Java Heap Used After GC,JDBC Concurrent Waiters,JDBC Connection Pools[Derby JDBC Provider XA)] JDBC Concurrent Waiters,JDBC Connection Pools[Derby JDBC Provider XA)] JDBC Free Pool Size,JDBC Connection Pools[Derby JDBC Provider XA)] JDBC Percent Used,JDBC Connection Pools[Derby JDBC Provider XA)] JDBC Wait Time,JDBC Connection Pools[Oracle JDBC Driver XA)] JDBC Concurrent Waiters,JDBC Connection Pools[Oracle JDBC Driver XA)] JDBC Free Pool Size,JDBC Connection Pools[Oracle JDBC Driver XA)] JDBC Percent Used,JDBC Connection Pools[Oracle JDBC Driver XA)] JDBC Wait Time,JDBC Free Pool Size,JDBC Percent Used,JDBC Wait Time,MDB Message Count,MonitoringProfile,Non Heap Memory Used,ProbeOverhead,ProcessCpuUtil,ProcessCpuUtilAbs,Servlets Response Time,Servlets Total Requests,SM Active Sessions,SM Live Sessions,SocketBytesInPerSec,SocketBytesOutPerSec,ThreadRetardation,Threads Active,Threads Created / sec,Threads Current Count,Threads Pool Size,ThrottlingLocations,TM Active Global Txns,TM Global Txns Comitted,TM Global Txns Rolled - Back";

        }

        public string OracleMetrics()
        {
            return @"Availability,Background Checkpoints Per Sec,Branch Node Splits Per Sec,Branch Node Splits Per Txn,Buffer Cache Hit Ratio,Consistent Read Changes Per Sec,Consistent Read Changes Per Txn,Consistent Read Gets Per Sec,Consistent Read Gets Per Txn,CPU Usage Per Sec,CPU Usage Per Txn,CR Blocks Created Per Sec,CR Blocks Created Per Txn,CR Undo Records Applied Per Sec,CR Undo Records Applied Per Txn,Current Logons Count,Current Open Cursors Count,Current OS Load,Cursor Cache Hit Ratio,Database CPU Time Ratio,Database Time Per Sec,Database Wait Time Ratio,DB Block Changes Per Sec,DB Block Changes Per Txn,DB Block Changes Per User Call,DB Block Gets Per Sec,DB Block Gets Per Txn,DB Block Gets Per User Call,DBWR Checkpoints Per Sec,Disk Sort Per Sec,Disk Sort Per Txn,Enqueue Deadlocks Per Sec,Enqueue Deadlocks Per Txn,Enqueue Requests Per Sec,Enqueue Requests Per Txn,Enqueue Timeouts Per Sec,Enqueue Timeouts Per Txn,Enqueue Waits Per Sec,Enqueue Waits Per Txn,Execute Without Parse Ratio,Executions Per Sec,Executions Per Txn,Executions Per User Call,Full Index Scans Per Sec,Full Index Scans Per Txn,GC CR Block Received Per Second,GC CR Block Received Per Txn,GC Current Block Received Per Second,GC Current Block Received Per Txn,Global Cache Average CR Get Time,Global Cache Average Current Get Time,Global Cache Blocks Corrupted,Global Cache Blocks Lost,Hard Parse Count Per Sec,Hard Parse Count Per Txn,Host CPU Utilization %),Leaf Node Splits Per Sec,Leaf Node Splits Per Txn,Library Cache Hit Ratio,Library Cache Miss Ratio,Load - Background CPU,Load - Database CPU,Load - RMAN CPU,Logical Reads Per Sec,Logical Reads Per Txn,Logical Reads Per User Call,Logons Per Sec,Logons Per Txn,Long Table Scans Per Sec,Long Table Scans Per Txn,Memory Sorts Ratio,Network Traffic Volume Per Sec,Open Cursors Per Sec,Open Cursors Per Txn,OracleLoad,Parse Failure Count Per Sec,Parse Failure Count Per Txn,PGA Cache Hit %,Physical Read Bytes Per Sec,Physical Read IO Requests Per Sec,Physical Read Total Bytes Per Sec,Physical Read Total IO Requests Per Sec,Physical Reads Direct Lobs Per Sec,Physical Reads Direct Lobs Per Txn,Physical Reads Direct Per Sec,Physical Reads Direct Per Txn,Physical Reads Per Sec,Physical Reads Per Txn,Physical Write Bytes Per Sec,Physical Write IO Requests Per Sec,Physical Write Total Bytes Per Sec,Physical Write Total IO Requests Per Sec,Physical Writes Direct Lobs Per Txn,Physical Writes Direct Lobs Per Sec,Physical Writes Direct Per Sec,Physical Writes Direct Per Txn,Physical Writes Per Sec,Physical Writes Per Txn,Process Limit %,PX downgraded 1 to 25% Per Sec,PX downgraded 25 to 50% Per Sec,PX downgraded 50 to 75% Per Sec,PX downgraded 75 to 99% Per Sec,PX downgraded to serial Per Sec,Recursive Calls Per Sec,Recursive Calls Per Txn,Redo Allocation Hit Ratio,Redo Generated Per Sec,Redo Generated Per Txn,Redo Writes Per Sec,Redo Writes Per Txn,Response Time Per Txn,Row Cache Hit Ratio,Row Cache Miss Ratio,Rows Per Sort,Session Limit %,Shared Pool Free %,Soft Parse Ratio,SQL Service Response Time,Total Index Scans Per Sec,Total Index Scans Per Txn,Total Parse Count Per Sec,Total Parse Count Per Txn,Total Sorts Per User Call,Total Table Scans Per Sec,Total Table Scans Per Txn,Total Table Scans Per User Call,Txns Per Logon,User Calls Per Sec,User Calls Per Txn,User Calls Ratio,User Commits Per Sec,User Commits Percentage,User Limit %,User Rollback Undo Records Applied Per Txn,User Rollback UndoRec Applied Per Sec,User Rollbacks Per Sec,User Rollbacks Percentage,User Transaction Per Sec";

        }

        public string MQMetrics()
        {
            return @"Availability,ChannelBuffersReceivedRate,ChannelBuffersSentRate,ChannelBytesReceivedRate,ChannelBytesSentRate,ChannelMessagesTransferredRate,QueueCurrentDepth,QueueMessageDequeueRate,QueueMessageEnqueueRate,QueueOpenInputCount,QueueOpenOutputCount";

        }

        public List<string> JavaMetricsArray()
        {
            string metrics = @"Availability,Classes Loaded,collection_leak_count,CompilationTime,EJB Ready Beans,EJB Response Time,EJB Total Method Calls,FileBytesInPerSec,FileBytesOutPerSec,GC Time Spent in Collections,HeapFree,HeapTotal,HeapUsed,HeapUsedPct,HeapUtilization,J2C Use Time,J2C Wait Time,J2C Waiting Thread Count,Java Heap Used After GC,JDBC Concurrent Waiters,JDBC Connection Pools[Derby JDBC Provider XA)] JDBC Concurrent Waiters,JDBC Connection Pools[Derby JDBC Provider XA)] JDBC Free Pool Size,JDBC Connection Pools[Derby JDBC Provider XA)] JDBC Percent Used,JDBC Connection Pools[Derby JDBC Provider XA)] JDBC Wait Time,JDBC Connection Pools[Oracle JDBC Driver XA)] JDBC Concurrent Waiters,JDBC Connection Pools[Oracle JDBC Driver XA)] JDBC Free Pool Size,JDBC Connection Pools[Oracle JDBC Driver XA)] JDBC Percent Used,JDBC Connection Pools[Oracle JDBC Driver XA)] JDBC Wait Time,JDBC Free Pool Size,JDBC Percent Used,JDBC Wait Time,MDB Message Count,MonitoringProfile,Non Heap Memory Used,ProbeOverhead,ProcessCpuUtil,ProcessCpuUtilAbs,Servlets Response Time,Servlets Total Requests,SM Active Sessions,SM Live Sessions,SocketBytesInPerSec,SocketBytesOutPerSec,ThreadRetardation,Threads Active,Threads Created / sec,Threads Current Count,Threads Pool Size,ThrottlingLocations,TM Active Global Txns,TM Global Txns Comitted,TM Global Txns Rolled - Back";
            List<string> results = new List<string>();
            foreach (var item in metrics.Split(','))
            {
                results.Add(item);
            }
            return results;
        }

        public List<string> OracleMetricsArray()
        {
            string metrics = @"Availability,Background Checkpoints Per Sec,Branch Node Splits Per Sec,Branch Node Splits Per Txn,Buffer Cache Hit Ratio,Consistent Read Changes Per Sec,Consistent Read Changes Per Txn,Consistent Read Gets Per Sec,Consistent Read Gets Per Txn,CPU Usage Per Sec,CPU Usage Per Txn,CR Blocks Created Per Sec,CR Blocks Created Per Txn,CR Undo Records Applied Per Sec,CR Undo Records Applied Per Txn,Current Logons Count,Current Open Cursors Count,Current OS Load,Cursor Cache Hit Ratio,Database CPU Time Ratio,Database Time Per Sec,Database Wait Time Ratio,DB Block Changes Per Sec,DB Block Changes Per Txn,DB Block Changes Per User Call,DB Block Gets Per Sec,DB Block Gets Per Txn,DB Block Gets Per User Call,DBWR Checkpoints Per Sec,Disk Sort Per Sec,Disk Sort Per Txn,Enqueue Deadlocks Per Sec,Enqueue Deadlocks Per Txn,Enqueue Requests Per Sec,Enqueue Requests Per Txn,Enqueue Timeouts Per Sec,Enqueue Timeouts Per Txn,Enqueue Waits Per Sec,Enqueue Waits Per Txn,Execute Without Parse Ratio,Executions Per Sec,Executions Per Txn,Executions Per User Call,Full Index Scans Per Sec,Full Index Scans Per Txn,GC CR Block Received Per Second,GC CR Block Received Per Txn,GC Current Block Received Per Second,GC Current Block Received Per Txn,Global Cache Average CR Get Time,Global Cache Average Current Get Time,Global Cache Blocks Corrupted,Global Cache Blocks Lost,Hard Parse Count Per Sec,Hard Parse Count Per Txn,Host CPU Utilization %),Leaf Node Splits Per Sec,Leaf Node Splits Per Txn,Library Cache Hit Ratio,Library Cache Miss Ratio,Load - Background CPU,Load - Database CPU,Load - RMAN CPU,Logical Reads Per Sec,Logical Reads Per Txn,Logical Reads Per User Call,Logons Per Sec,Logons Per Txn,Long Table Scans Per Sec,Long Table Scans Per Txn,Memory Sorts Ratio,Network Traffic Volume Per Sec,Open Cursors Per Sec,Open Cursors Per Txn,OracleLoad,Parse Failure Count Per Sec,Parse Failure Count Per Txn,PGA Cache Hit %,Physical Read Bytes Per Sec,Physical Read IO Requests Per Sec,Physical Read Total Bytes Per Sec,Physical Read Total IO Requests Per Sec,Physical Reads Direct Lobs Per Sec,Physical Reads Direct Lobs Per Txn,Physical Reads Direct Per Sec,Physical Reads Direct Per Txn,Physical Reads Per Sec,Physical Reads Per Txn,Physical Write Bytes Per Sec,Physical Write IO Requests Per Sec,Physical Write Total Bytes Per Sec,Physical Write Total IO Requests Per Sec,Physical Writes Direct Lobs Per Txn,Physical Writes Direct Lobs Per Sec,Physical Writes Direct Per Sec,Physical Writes Direct Per Txn,Physical Writes Per Sec,Physical Writes Per Txn,Process Limit %,PX downgraded 1 to 25% Per Sec,PX downgraded 25 to 50% Per Sec,PX downgraded 50 to 75% Per Sec,PX downgraded 75 to 99% Per Sec,PX downgraded to serial Per Sec,Recursive Calls Per Sec,Recursive Calls Per Txn,Redo Allocation Hit Ratio,Redo Generated Per Sec,Redo Generated Per Txn,Redo Writes Per Sec,Redo Writes Per Txn,Response Time Per Txn,Row Cache Hit Ratio,Row Cache Miss Ratio,Rows Per Sort,Session Limit %,Shared Pool Free %,Soft Parse Ratio,SQL Service Response Time,Total Index Scans Per Sec,Total Index Scans Per Txn,Total Parse Count Per Sec,Total Parse Count Per Txn,Total Sorts Per User Call,Total Table Scans Per Sec,Total Table Scans Per Txn,Total Table Scans Per User Call,Txns Per Logon,User Calls Per Sec,User Calls Per Txn,User Calls Ratio,User Commits Per Sec,User Commits Percentage,User Limit %,User Rollback Undo Records Applied Per Txn,User Rollback UndoRec Applied Per Sec,User Rollbacks Per Sec,User Rollbacks Percentage,User Transaction Per Sec";
            List<string> results = new List<string>();
            foreach (var item in metrics.Split(','))
            {
                results.Add(item);
            }
            return results;
        }

        public List<string> MQMetricsArray()
        {
            string metrics = @"Availability,ChannelBuffersReceivedRate,ChannelBuffersSentRate,ChannelBytesReceivedRate,ChannelBytesSentRate,ChannelMessagesTransferredRate,QueueCurrentDepth,QueueMessageDequeueRate,QueueMessageEnqueueRate,QueueOpenInputCount,QueueOpenOutputCount";
            List<string> results = new List<string>();
            foreach (var item in metrics.Split(','))
            {
                results.Add(item);
            }
            return results;
        }

        
    }
}
