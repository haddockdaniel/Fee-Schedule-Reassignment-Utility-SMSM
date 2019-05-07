using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Globalization;
using Gizmox.Controls;
using JDataEngine;
using JurisAuthenticator;
using JurisUtilityBase.Properties;

namespace JurisUtilityBase
{
    public partial class UtilityBaseMain : Form
    {
        #region Private  members

        private JurisUtility _jurisUtility;

        #endregion

        #region Public properties

        public string CompanyCode { get; set; }

        public string JurisDbName { get; set; }

        public string JBillsDbName { get; set; }

        public int FldClient { get; set; }

        public int FldMatter { get; set; }

        public string fromFeeSched = "";

        public string toFeeSched = "";

        public string clientIDs = "";

        public string matterIDs = "";

        #endregion

        #region Constructor

        public UtilityBaseMain()
        {
            InitializeComponent();
            _jurisUtility = new JurisUtility();
        }

        #endregion



        #region Public methods

        public void LoadCompanies()
        {
            var companies = _jurisUtility.Companies.Cast<object>().Cast<Instance>().ToList();
//            listBoxCompanies.SelectedIndexChanged -= listBoxCompanies_SelectedIndexChanged;
            listBoxCompanies.ValueMember = "Code";
            listBoxCompanies.DisplayMember = "Key";
            listBoxCompanies.DataSource = companies;
//            listBoxCompanies.SelectedIndexChanged += listBoxCompanies_SelectedIndexChanged;
            var defaultCompany = companies.FirstOrDefault(c => c.Default == Instance.JurisDefaultCompany.jdcJuris);
            if (companies.Count > 0)
            {
                listBoxCompanies.SelectedItem = defaultCompany ?? companies[0];
            }
        }

        #endregion

        #region MainForm events

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void listBoxCompanies_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_jurisUtility.DbOpen)
            {
                _jurisUtility.CloseDatabase();
            }
            CompanyCode = "Company" + listBoxCompanies.SelectedValue;
            _jurisUtility.SetInstance(CompanyCode);
            JurisDbName = _jurisUtility.Company.DatabaseName;
            JBillsDbName = "JBills" + _jurisUtility.Company.Code;
            _jurisUtility.OpenDatabase();
            if (_jurisUtility.DbOpen)
            {
                ///GetFieldLengths();
            }

            string FSIndex;
            cbFrom.ClearItems();
            string SQLFS = "select feeschcode + case when len(feeschcode)=1 then '     ' when len(feeschcode)=2 then '     ' when len(feeschcode)=3 then '   ' else '  ' end +  feeschdesc as FS from feeschedule where feeschactive='Y'   order by feeschcode";
            DataSet myRSFS = _jurisUtility.RecordsetFromSQL(SQLFS);

            if (myRSFS.Tables[0].Rows.Count == 0)
                cbFrom.SelectedIndex = 0;
            else
            {
                foreach (DataRow dr in myRSFS.Tables[0].Rows)
                {
                    FSIndex = dr["FS"].ToString();
                    cbFrom.Items.Add(FSIndex);
                }
            }

            string FSIndex2;
            cbTo.ClearItems();
            string SQLFS2 = "select feeschcode + case when len(feeschcode)=1 then '     ' when len(feeschcode)=2 then '     ' when len(feeschcode)=3 then '   ' else '  ' end +  feeschdesc as FS from feeschedule where feeschactive='Y'  order by feeschcode";
            DataSet myRSFS2 = _jurisUtility.RecordsetFromSQL(SQLFS2);

            if (myRSFS2.Tables[0].Rows.Count == 0)
                cbTo.SelectedIndex = 0;
            else
            {
                foreach (DataRow dr in myRSFS2.Tables[0].Rows)
                {
                    FSIndex2 = dr["FS"].ToString();
                    cbTo.Items.Add(FSIndex2);
                }
            }

        }



        #endregion

        #region Private methods

        private void DoDaFix()
        {
            // Enter your SQL code here
            // To run a T-SQL statement with no results, int RecordsAffected = _jurisUtility.ExecuteNonQueryCommand(0, SQL);
            // To get an DataSet, DataSet myRS = _jurisUtility.RecordsetFromSQL(SQL);


            if ((string.IsNullOrEmpty(toFeeSched) || string.IsNullOrEmpty(fromFeeSched)) || fromFeeSched.Equals(toFeeSched))
                { MessageBox.Show("Please select a fee schedule from both drop downs and ensure they are different before continuing."); }
            else
            {
                if ((string.IsNullOrEmpty(clientIDs) && string.IsNullOrEmpty(matterIDs)) && !CBSelectAll.Checked)
                    { MessageBox.Show("Please select at least one client or matter before continuing."); }
                else
                {
                    DialogResult d1 = MessageBox.Show("Would you like to see a pre-report of the clients/maters to be changed?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (d1 == System.Windows.Forms.DialogResult.Yes)
                    {
                        string SQLTkpr = "";
                        if (!CBSelectAll.Checked)
                            SQLTkpr = getReportSQLforSelections();
                        else
                            SQLTkpr = getReportSQLforAll();

                        DataSet report = _jurisUtility.RecordsetFromSQL(SQLTkpr);

                        ReportDisplay rpds = new ReportDisplay(report);
                        rpds.ShowDialog();

                    }
                    string CM2 = "";

                    if (rbCM.Checked)
                    { CM2 = "Fee Schedules for Clients and Matters will be changed from "; }
                    if (rbClient.Checked)
                    { CM2 = "Fee Schedules for Clients will be changed from "; }
                    if (rbMatter.Checked)
                    { CM2 = "Fee Schedules for Matters will be changed from "; }

                    CM2 = CM2 + fromFeeSched + " to " + toFeeSched;
                    if (rbFSopen.Checked)
                        CM2 = CM2 + ". Only open matters will be changed based on the selection criteria.";
                    else if (rbFSClosed.Checked)
                        CM2 = CM2 + ". Only closed matters will be changed based on the selection criteria.";
                    else
                        CM2 = CM2 + ". Both open and closed matters will be changed based on the selection criteria.";

                    DialogResult rsBoth = MessageBox.Show(CM2 + " Do you wish to continue?", "Fee Schedule Assignment", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (rsBoth == DialogResult.Yes)
                    {

                        if (rbCM.Checked)
                        {
                            if (String.IsNullOrEmpty(clientIDs))
                                updateClients();
                            if (String.IsNullOrEmpty(matterIDs))
                                updateMatters();
                            Cursor.Current = Cursors.Default;
                            Application.DoEvents();
                            toolStripStatusLabel.Text = "Fee Schedules Updated: Clients and Matters";
                            statusStrip.Refresh();

                        }

                        if (rbClient.Checked)
                        {
                            if (String.IsNullOrEmpty(clientIDs))
                                updateClients();
                            Cursor.Current = Cursors.Default;
                            Application.DoEvents();
                            toolStripStatusLabel.Text = "Fee Schedules Updated: Client";
                            statusStrip.Refresh();
                        }

                        if (rbMatter.Checked)
                        {
                            if (String.IsNullOrEmpty(matterIDs))
                                updateMatters();
                            Cursor.Current = Cursors.Default;
                            Application.DoEvents();
                            toolStripStatusLabel.Text = "Fee Schedules Updated: Matters";
                            statusStrip.Refresh();
                        }

                        if (CBSelectAll.Checked && rbCM.Checked && rbFSAll.Checked)
                        {
                            DialogResult rsAll = MessageBox.Show("Fee Schedules Updated for all client matters.  Do you wish to delete fee schedule?", "Fee Schedule Assignment", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                            if (rsAll == DialogResult.Yes)
                            {
                                deleteFeeSchedule();
                            }
                        }

                        UpdateStatus("Fee Schedule Update Complete. Process Finished!", 5, 5);
                        string LogNote = "Fee Schedule Assignment - " + fromFeeSched.ToString().Trim() + " to " + toFeeSched.ToString().Trim();
                        WriteLog(LogNote.ToString());
                        MessageBox.Show("Fee Schedule Update Complete." + "\r\n" + "The tool will now close", "Process Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        clientIDs = "";
                        matterIDs = "";
                        cbFrom.SelectedIndex = -1;
                        cbTo.SelectedIndex = -1;
                        try
                        {
                            System.Environment.Exit(0);
                        }
                        catch (Exception ex1)
                        { }
                    }
                }
            }
       }

        
        private void updateClients()
        {  
            Cursor.Current = Cursors.WaitCursor;
            Application.DoEvents();
            toolStripStatusLabel.Text = "Updating Client Fee Schedules....";
            statusStrip.Refresh();
            UpdateStatus("Client Fee Schedules", 2, 5);

            if (CBSelectAll.Checked)
                clientIDs = "select clisysnbr from client where clifeesch = '" + fromFeeSched + "'";
            string CC2 = "update client set clifeesch ='" + toFeeSched.ToString() + "'  where CliSysNbr in (" + clientIDs + ")";
           _jurisUtility.ExecuteNonQueryCommand(0, CC2);
                 
           }

    private void updateMatters()
    {
        Cursor.Current = Cursors.WaitCursor;
        Application.DoEvents();
        toolStripStatusLabel.Text = "Updating Matter Fee Schedules....";
        statusStrip.Refresh();
        UpdateStatus("Matter  Fee Schedules", 3, 5);

        if (CBSelectAll.Checked)
            matterIDs = "select MatSysNbr from matter where matfeesch = '" + fromFeeSched + "'";
        string MOpen = "update matter set matfeesch='" + toFeeSched.ToString() + "' where MatSysNbr in (" + matterIDs + ")";
        _jurisUtility.ExecuteNonQueryCommand(0, MOpen);

    }

    private void deleteFeeSchedule()
    {
        string UT = "update unbilledtime set utfeesched='" + toFeeSched.ToString() + "' where utfeesched='" + fromFeeSched.ToString() + "'";
        _jurisUtility.ExecuteNonQueryCommand(0, UT);

        UpdateStatus("Matter  Fee Schedules", 7, 10);

        string BT = "update billedtime set btfeesched='" + toFeeSched.ToString() + "' where btfeesched='" + fromFeeSched.ToString() + "'";
        _jurisUtility.ExecuteNonQueryCommand(0, BT);

        UpdateStatus("Matter  Fee Schedules", 8, 10);

        string TBD = "update timebatchdetail set tbdfeesched='" + toFeeSched.ToString() + "' where tbdfeesched='" + fromFeeSched.ToString() + "'";
        _jurisUtility.ExecuteNonQueryCommand(0, TBD);



        string TE = "update timeentry set feeschedulecode='" + toFeeSched.ToString() + "' where feeschedulecode='" + fromFeeSched.ToString() + "'";
        _jurisUtility.ExecuteNonQueryCommand(0, TE);

        UpdateStatus("Matter  Fee Schedules", 9, 10);
        string TR = "delete from  tkprrate  where tkrfeesch='" + fromFeeSched.ToString() + "'";
        _jurisUtility.ExecuteNonQueryCommand(0, TR);

        string PR = "delete from  perstyprate  where ptrfeesch='" + fromFeeSched.ToString() + "'";
        _jurisUtility.ExecuteNonQueryCommand(0, PR);

        PR = "delete from  TaskCodeRate  where TCRFeeSch='" + fromFeeSched.ToString() + "'";
        _jurisUtility.ExecuteNonQueryCommand(0, PR);

        string FSC = "delete from  feeschedule  where feeschcode='" + fromFeeSched.ToString() + "'";
        _jurisUtility.ExecuteNonQueryCommand(0, FSC);

        string DT = "delete from  documenttree   where dtparentid=17 and dtkeyt='" + fromFeeSched.ToString() + "'";
        _jurisUtility.ExecuteNonQueryCommand(0, DT);
    }







    private bool VerifyFirmName()
        {
            //    Dim SQL     As String
            //    Dim rsDB    As DataSet
            //
            //    SQL = "SELECT CASE WHEN SpTxtValue LIKE '%firm name%' THEN 'Y' ELSE 'N' END AS Firm FROM SysParam WHERE SpName = 'FirmName'"
            //    Cmd.CommandText = SQL
            //    Set rsDB = Cmd.Execute
            //
            //    If rsDB!Firm = "Y" Then
            return true;
            //    Else
            //        VerifyFirmName = False
            //    End If

        }

    private bool FieldExistsInRS(DataSet ds, string fieldName)
    {

        foreach (DataColumn column in ds.Tables[0].Columns)
        {
            if (column.ColumnName.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }


        private static bool IsDate(String date)
        {
            try
            {
                DateTime dt = DateTime.Parse(date);
                return true;
            }
            catch
            {
                return false;
            }
        }
        private static bool IsNumeric(object Expression)
        {
            double retNum;

            bool isNum = Double.TryParse(Convert.ToString(Expression), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
            return isNum; 
        }

        private void WriteLog(string comment)
        {
            string sql = "Insert Into UtilityLog(ULTimeStamp,ULWkStaUser,ULComment) Values(convert(datetime,'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'),cast('" +  GetComputerAndUser() + "' as varchar(100))," + "cast('" + comment.ToString() + "' as varchar(8000)))";
            _jurisUtility.ExecuteNonQueryCommand(0, sql);
        }

        private string GetComputerAndUser()
        {
            var computerName = Environment.MachineName;
            var windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var userName = (windowsIdentity != null) ? windowsIdentity.Name : "Unknown";
            return computerName + "/" + userName;
        }

        /// <summary>
        /// Update status bar (text to display and step number of total completed)
        /// </summary>
        /// <param name="status">status text to display</param>
        /// <param name="step">steps completed</param>
        /// <param name="steps">total steps to be done</param>
        private void UpdateStatus(string status, long step, long steps)
        {
            labelCurrentStatus.Text = status;

            if (steps == 0)
            {
                progressBar.Value = 0;
                labelPercentComplete.Text = string.Empty;
            }
            else
            {
                double pctLong = Math.Round(((double)step/steps)*100.0);
                int percentage = (int)Math.Round(pctLong, 0);
                if ((percentage < 0) || (percentage > 100))
                {
                    progressBar.Value = 0;
                    labelPercentComplete.Text = string.Empty;
                }
                else
                {
                    progressBar.Value = percentage;
                    labelPercentComplete.Text = string.Format("{0} percent complete", percentage);
                }
            }
        }
        private void DeleteLog()
        {
            string AppDir = Path.GetDirectoryName(Application.ExecutablePath);
            string filePathName = Path.Combine(AppDir, "VoucherImportLog.txt");
            if (File.Exists(filePathName + ".ark5"))
            {
                File.Delete(filePathName + ".ark5");
            }
            if (File.Exists(filePathName + ".ark4"))
            {
                File.Copy(filePathName + ".ark4", filePathName + ".ark5");
                File.Delete(filePathName + ".ark4");
            }
            if (File.Exists(filePathName + ".ark3"))
            {
                File.Copy(filePathName + ".ark3", filePathName + ".ark4");
                File.Delete(filePathName + ".ark3");
            }
            if (File.Exists(filePathName + ".ark2"))
            {
                File.Copy(filePathName + ".ark2", filePathName + ".ark3");
                File.Delete(filePathName + ".ark2");
            }
            if (File.Exists(filePathName + ".ark1"))
            {
                File.Copy(filePathName + ".ark1", filePathName + ".ark2");
                File.Delete(filePathName + ".ark1");
            }
            if (File.Exists(filePathName ))
            {
                File.Copy(filePathName, filePathName + ".ark1");
                File.Delete(filePathName);
            }

        }

            

        private void LogFile(string LogLine)
        {
            string AppDir = Path.GetDirectoryName(Application.ExecutablePath);
            string filePathName = Path.Combine(AppDir, "VoucherImportLog.txt");
            using (StreamWriter sw = File.AppendText(filePathName))
            {
                sw.WriteLine(LogLine);
            }	
        }
        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            DoDaFix();
        }

        private void btExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void cbFrom_SelectedIndexChanged(object sender, EventArgs e)
        {
            fromFeeSched = this.cbFrom.GetItemText(this.cbFrom.SelectedItem).Split(' ')[0];
        }

        private void cbTo_SelectedIndexChanged(object sender, EventArgs e)
        {
            toFeeSched = this.cbTo.GetItemText(this.cbTo.SelectedItem).Split(' ')[0];
        }

        private void buttonReport_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(toFeeSched) || string.IsNullOrEmpty(fromFeeSched))
                MessageBox.Show("Please select from both Fee Schedule drop downs", "Selection Error");
            else
            {
                Client_Matter_Selector clm = new Client_Matter_Selector(fromFeeSched, _jurisUtility, getClientMatterOption(), getMatterStatusOption());
                if (clm.canLoad)
                {
                    matterIDs = "";
                    clientIDs = "";
                    clm.ShowDialog();
                    matterIDs = clm.matterIDs;
                    clientIDs = clm.clientIDs;
                    clm.Close();
                    button1.Enabled = true;
                }
            }
        }

        private string getReportSQLforSelections()
        {
            string reportSQL = "";
            string matterStatus = getMatterStatusOption();
            //if client
            if (rbClient.Checked)
                reportSQL = "select dbo.jfn_formatclientcode(clicode) as ClientCode, clireportingname as ClientName, clifeesch as OldFeeSchedule, '" + toFeeSched + "' as NewFeeSchedule from client" +
                        " where CliSysNbr in (" + clientIDs + ")";

            //if matter
            else if (rbMatter.Checked)
                reportSQL = "select dbo.jfn_formatmattercode(matcode) as MatterCode, matreportingname as MatterName, matfeesch as OldFeeSchedule, '" + toFeeSched + "' as NewFeeSchedule from matter" +
                        " where MatSysNbr in ('" + matterIDs + ") " + matterStatus;


            //if both
            else if (rbCM.Checked)
                reportSQL = "select dbo.jfn_formatclientcode(clicode) as ClientCode, clireportingname as ClientName, '' as MatterCode, '' as MatterName, clifeesch as OldClientFeeSchedule, '' as OldMatterFeeSchedule, '" + toFeeSched + "' as NewFeeSchedule, 'Client Change' as [Type] from client" +
                                        " where CliSysNbr in (" + clientIDs + ")" + 
                                        " UNION ALL " +
                                        " select dbo.jfn_formatclientcode(clicode) as ClientCode, clireportingname as ClientName, dbo.jfn_formatmattercode(matcode) as MatterCode, matreportingname as MatterName, '' as OldClientFeeSchedule,matfeesch as OldMatterFeeSchedule, '" + toFeeSched + "' as NewFeeSchedule, 'Matter Change' as [Type] from matter" +
                                        " inner join client on CliSysNbr = MatCliNbr " +
                                        " where MatSysNbr in (" + matterIDs + ") " + matterStatus;


            return reportSQL;
        }

        private string getReportSQLforAll()
        {
            string reportSQL = "";
            string matterStatus = " ";
            if (rbFSopen.Checked)
                matterStatus = " and matstatusflag='O'";
            else if (rbFSClosed.Checked)
                matterStatus = " and matstatusflag='C'";
            //if client
            if (rbClient.Checked)
                reportSQL = "select dbo.jfn_formatclientcode(clicode) as ClientCode, clireportingname as ClientName, clifeesch as OldFeeSchedule, '" + toFeeSched + "' as NewFeeSchedule from client" +
                        " where clifeesch='" + fromFeeSched + "'";

            //if matter
            else if (rbMatter.Checked)
                reportSQL = "select dbo.jfn_formatmattercode(matcode) as MatterCode, matreportingname as MatterName, matfeesch as OldFeeSchedule, '" + toFeeSched + "' as NewFeeSchedule from matter" +
                        " where matfeesch='" + fromFeeSched + "'" + matterStatus;


            //if both
            else if (rbCM.Checked)
                reportSQL = "select dbo.jfn_formatclientcode(clicode) as ClientCode, clireportingname as ClientName, '' as MatterCode, '' as MatterName, clifeesch as OldClientFeeSchedule, '' as OldMatterFeeSchedule, '" + toFeeSched + "' as NewFeeSchedule, 'Client Change' as [Type] from client" +
                                        " where clifeesch='" + fromFeeSched + "'" +
                                        " UNION ALL " +
                                        " select dbo.jfn_formatclientcode(clicode) as ClientCode, clireportingname as ClientName, dbo.jfn_formatmattercode(matcode) as MatterCode, matreportingname as MatterName, '' as OldClientFeeSchedule,matfeesch as OldMatterFeeSchedule, '" + toFeeSched + "' as NewFeeSchedule, 'Matter Change' as [Type] from matter" +
                                        " inner join client on CliSysNbr = MatCliNbr " +
                                        " where matfeesch='" + fromFeeSched + "' " + matterStatus;


            return reportSQL;
        }

        private string getClientMatterOption()
        {
            if (rbClient.Checked)
                return "C";
            else if (rbMatter.Checked)
                return "M";
            else if (rbCM.Checked)
                return "Both";

            //default
            return "Both";
        }

        private string getMatterStatusOption()
        {
            string matterStatus = "";
            if (rbFSopen.Checked)
                matterStatus = " and matstatusflag='O'";
            else if (rbFSClosed.Checked)
                matterStatus = " and matstatusflag='C'";

            return matterStatus;
        }

        private void CBSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            button1.Enabled = true;
            buttonReport.Enabled = false;
        }

        private void cbCherryPick_CheckedChanged(object sender, EventArgs e)
        {
            buttonReport.Enabled = true;
            button1.Enabled = false;
        }

    }
}
