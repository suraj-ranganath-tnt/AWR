using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Diagnostics
{
    public class DiagnosticsCustomClass
    {
        //yet to complete
        public DataGrid DivideGranularityTo5Mins(DateTime StartTime, DateTime EndTime, string ProbeGroupName, string ProbeName, string ServerName, string ProbeType, string metrics)
        {

            try
            {
                DataGrid result = new DataGrid();
                List<string> TimeInterval = new List<string>();
                DateTime start = DateTime.Parse(StartTime.ToString());
                DateTime end = DateTime.Parse(EndTime.ToString());
                List<TimeValue> ObjList = new List<TimeValue>();
                TimeValue ObjValue = new TimeValue();
                string htmlCode=string.Empty;
                string htmlResponse = string.Empty;
                // Create Request
                //HttpWebRequest req = (HttpWebRequest)WebRequest.Create(@"" + ConfigurationManager.AppSettings["DiagnosticsServerURL"] +"/query/?action=trend&granularity=[name='5m',start='9223372036854775807',end='9223372036854775807']&response_format=excel&path=%2Fgroupby%5Bname%3D'Default%20Client%3A'%5D%2Fprobegroup%5Bname%3D'ACAS_profiling'%5D%2Fprobe%5Bname%3D'gblabvl281_ACAS_server1'%2ChostName%3D'gblabvl281.gb.tntpost.com'%2CsystemGroup%3D'Default'%2CprobeType%3D'Java'%5D%2Fmetric[name='Availability',type='average']");
                // Create Client
                WebClient client = new WebClient();
                // Assign Credentials
                
                client.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["DiagnosticsServerUserName"], ConfigurationManager.AppSettings["DiagnosticsServerPassword"]);


                while (end > start)
                {
                    TimeInterval.Add(start.ToString("h.mmtt", CultureInfo.InvariantCulture));
                    var url = "" + ConfigurationManager.AppSettings["DiagnosticsServerURL"] +"/query/?action=trend&granularity=[name='5m',start='" + ((start.ToOADate() - 25569) * 60 * 60 * 24 * 1000).ToString() + "',end='" + ((start.AddMinutes(5).ToOADate() - 25569) * 60 * 60 * 24 * 1000).ToString() + "']&response_format=excel&path=%2Fgroupby%5Bname%3D'Default%20Client%3A'%5D%2Fprobegroup%5Bname%3D'" + ProbeGroupName + "'%5D%2Fprobe%5Bname%3D'" + ProbeName + "'%2ChostName%3D'" + ServerName + "'%2CsystemGroup%3D'Default'%2CprobeType%3D'" + ProbeType + "'%5D%2Fmetric[name='" + metrics.Replace(',','_') + "',type='average']";
                    try
                    {
                        htmlCode = client.DownloadString(url);
                    }
                    catch (Exception)
                    {
                        
                    }
                    
                    int numLines = htmlCode.Split('\n').Length;
                    if (numLines <= 3)
                        goto exitloop;
                    for (int i = 4; i < numLines - 1; i++)
                    {
                        //Add column names
                        if(i==4)
                        {
                            ObjValue = new TimeValue();
                            ObjValue.Time = "DateTime";
                            ObjValue.value = "Value";
                            ObjList.Add(ObjValue);
                        }
                        ObjValue = new TimeValue();
                        ObjValue.Time = DateTime.FromOADate(Convert.ToDouble(htmlCode.Split('\n')[i].Split('\t')[0])).ToString();
                        ObjValue.value = Convert.ToString(Convert.ToDecimal((htmlCode.Split('\n')[i].Split('\t')[1])) * 100);
                        ObjList.Add(ObjValue);
                    }

                    start = start.AddMinutes(5);
                    htmlResponse = string.Concat(htmlCode);

                }
                result.DataSource = ObjList;
                
                exitloop:
                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        
        public DataGrid ConvertStringToTable(String Input)
        {
            List<TimeValue> ObjList = new List<TimeValue>();
            TimeValue ObjValue = new TimeValue();
            DataGrid result = new DataGrid();

            int numLines = Input.Split('\n').Length;
            for (int i = 4; i < numLines - 1; i++)
            {
                ObjValue = new TimeValue();
                ObjValue.Time = DateTime.FromOADate(Convert.ToDouble(Input.Split('\n')[i].Split('\t')[0])).ToString();
                ObjValue.value = Convert.ToString(Convert.ToDecimal((Input.Split('\n')[i].Split('\t')[1])) * 100);
                ObjList.Add(ObjValue);
            }
            result.DataSource = ObjList;
            //dataGridView1.Columns[0].HeaderText = "Date Time";
            //dataGridView1.Columns[1].HeaderText = "Value";
            return result;
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

            List<string> ServerNames = new List<string>();
            List<string> ProbeType = new List<string>();

            int NoOfServers = ConfigurationManager.AppSettings["ServerName"].Split(',').Count();
            for (int i = 0; i < NoOfServers; i++)
            {
                ServerNames.Add(ConfigurationManager.AppSettings["ServerName"].Split(',')[i]);
            }

            NoOfServers = ConfigurationManager.AppSettings["ProbType"].Split(',').Count();
            for (int i = 0; i < NoOfServers; i++)
            {
                ProbeType.Add(ConfigurationManager.AppSettings["ProbType"].Split(',')[i]);
            }


            return true;
        }

        public static List<string> FetchMetrics(string MetricsType,int isCrucial)
        {
            try
            {
                SQLiteConnection m_dbConnection;
                List<string> lstMetricsResults = new List<string>();
                ProbeDetails objProbeDetails = new ProbeDetails();

                //DB Details
                m_dbConnection = new SQLiteConnection(@"Data Source=" + ConfigurationManager.AppSettings["LocalDB_DS"] +";Version=3;");
                m_dbConnection.Open();
                string sql = string.Empty;
                switch (MetricsType)
                {
                    case "Java":
                        sql = @"select MetricsName from Diagnostics_JavaMetrics where isCrucial=" + isCrucial;
                        break;
                    case "Oracle":
                        sql = @"select MetricsName from Diagnostics_OracleMetrics where isCrucial=" + isCrucial;
                        break;
                    case "MQ":
                        sql = @"select MetricsName from Diagnostics_MQMetrics where isCrucial=" + isCrucial;
                        break;
                    default:
                        
                        break;
                }

                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    lstMetricsResults.Add(reader["MetricsName"].ToString());
                    //Console.WriteLine("Id: " + reader["Id"] + "\tProbeGroup: " + reader["ProbeGroup"] + "\tProbeName: " + reader["ProbeName"] + "\tHostName: " + reader["HostName"]);
                }

                reader.Close();
                m_dbConnection.Close();
                
                return lstMetricsResults;
            }
            catch (Exception)
            {

                throw;
            }


        }

        public static List<string> FetchMetrics(string MetricsType)
        {
            try
            {
                SQLiteConnection m_dbConnection;
                List<string> lstMetricsResults = new List<string>();
                ProbeDetails objProbeDetails = new ProbeDetails();

                //DB Details
                m_dbConnection = new SQLiteConnection(@"Data Source=" + ConfigurationManager.AppSettings["LocalDB_DS"] +";Version=3;");
                m_dbConnection.Open();
                string sql = string.Empty;
                switch (MetricsType)
                {
                    case "Java":
                        sql = @"select MetricsName from Diagnostics_JavaMetrics";
                        break;
                    case "Oracle":
                        sql = @"select MetricsName from Diagnostics_OracleMetrics";
                        break;
                    case "MQ":
                        sql = @"select MetricsName from Diagnostics_MQMetrics";
                        break;
                    default:

                        break;
                }

                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    lstMetricsResults.Add(reader["MetricsName"].ToString());
                    //Console.WriteLine("Id: " + reader["Id"] + "\tProbeGroup: " + reader["ProbeGroup"] + "\tProbeName: " + reader["ProbeName"] + "\tHostName: " + reader["HostName"]);
                }

                reader.Close();
                m_dbConnection.Close();

                return lstMetricsResults;
            }
            catch (Exception)
            {

                throw;
            }


        }
        public static List<string> FetchServers()
        {
            try
            {
                SQLiteConnection m_dbConnection;
                List<string> lstServerResults = new List<string>();
                ProbeDetails objProbeDetails = new ProbeDetails();

                //DB Details
                m_dbConnection = new SQLiteConnection(@"Data Source=" + ConfigurationManager.AppSettings["LocalDB_DS"] +";Version=3;");
                m_dbConnection.Open();
                string sql = "";
                               
                sql = @"select distinct hostname from Diagnostics_MasterData";
                 

                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    lstServerResults.Add(reader["hostname"].ToString());
                    //Console.WriteLine("Id: " + reader["Id"] + "\tProbeGroup: " + reader["ProbeGroup"] + "\tProbeName: " + reader["ProbeName"] + "\tHostName: " + reader["HostName"]);
                }

                reader.Close();
                m_dbConnection.Close();
                return lstServerResults;
            }
            catch (Exception)
            {

                throw;
            }


        }

        public static List<string> FetchProbeTypes()
        {
            try
            {
                SQLiteConnection m_dbConnection;
                List<string> lstServerResults = new List<string>();
                ProbeDetails objProbeDetails = new ProbeDetails();

                //DB Details
                m_dbConnection = new SQLiteConnection(@"Data Source=" + ConfigurationManager.AppSettings["LocalDB_DS"] +";Version=3;");
                m_dbConnection.Open();
                string sql = "";

                sql = @"select distinct ProbeType from Diagnostics_MasterData";


                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    lstServerResults.Add(reader["ProbeType"].ToString());
                    //Console.WriteLine("Id: " + reader["Id"] + "\tProbeGroup: " + reader["ProbeGroup"] + "\tProbeName: " + reader["ProbeName"] + "\tHostName: " + reader["HostName"]);
                }

                reader.Close();
                m_dbConnection.Close();
                return lstServerResults;
            }
            catch (Exception)
            {

                throw;
            }


        }

        public static List<string> FetchServerBasedOnProbeTypes(string ProbeType)
        {
            try
            {
                SQLiteConnection m_dbConnection;
                List<string> lstServerResults = new List<string>();
                ProbeDetails objProbeDetails = new ProbeDetails();

                //DB Details
                m_dbConnection = new SQLiteConnection(@"Data Source=" + ConfigurationManager.AppSettings["LocalDB_DS"] +";Version=3;");
                m_dbConnection.Open();
                string sql = "";

                sql = @"select distinct Hostname from Diagnostics_MasterData where probetype='"+ ProbeType +"'";


                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    lstServerResults.Add(reader["Hostname"].ToString());
                    //Console.WriteLine("Id: " + reader["Id"] + "\tProbeGroup: " + reader["ProbeGroup"] + "\tProbeName: " + reader["ProbeName"] + "\tHostName: " + reader["HostName"]);
                }

                reader.Close();
                m_dbConnection.Close();
                return lstServerResults;
            }
            catch (Exception)
            {

                throw;
            }


        }

        public static List<ProbeDetails> SelectData(string ServerName)
        {
            try
            {
                SQLiteConnection m_dbConnection;
                List<ProbeDetails> lstProbeResults = new List<ProbeDetails>();
                ProbeDetails objProbeDetails = new ProbeDetails();

                //DB Details
                m_dbConnection = new SQLiteConnection(@"Data Source=" + ConfigurationManager.AppSettings["LocalDB_DS"] +";Version=3;");
                m_dbConnection.Open();
                string sql = @"select * from Diagnostics_MasterData where HostName = '" + ServerName + "'";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    objProbeDetails.HostName = reader["HostName"].ToString();
                    objProbeDetails.ProbeGroup = reader["ProbeGroup"].ToString();
                    objProbeDetails.ProbeName = reader["ProbeName"].ToString();
                    objProbeDetails.ProbeType = reader["ProbeType"].ToString();
                    lstProbeResults.Add(objProbeDetails);
                    //Console.WriteLine("Id: " + reader["Id"] + "\tProbeGroup: " + reader["ProbeGroup"] + "\tProbeName: " + reader["ProbeName"] + "\tHostName: " + reader["HostName"]);
                }

                reader.Close();
                m_dbConnection.Close();
                return lstProbeResults;


            }
            catch (Exception)
            {

                throw;
            }


        }
        public class ProbeDetails
        {
            public string Id { get; set; }
            public string ProbeGroup { get; set; }
            public string ProbeName { get; set; }
            public string HostName { get; set; }
            public string ProbeType { get; set; }
        }

    }
}
