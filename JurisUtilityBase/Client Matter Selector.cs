using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using JDataEngine;
using JurisAuthenticator;
using JurisUtilityBase.Properties;
using System.Data.OleDb;

namespace JurisUtilityBase
{
    public partial class Client_Matter_Selector : Form
    {
        public Client_Matter_Selector(string fromFeeSched, JurisUtility _jurisUtility, string ClientMatterOption, string MatterStatusOption)
        {
            InitializeComponent();
            listViewClient.MultiSelect = true;
            listViewClient.CheckBoxes = true;
            listViewClient.FullRowSelect = true;
            listViewMatter.MultiSelect = true;
            listViewMatter.CheckBoxes = true;
            listViewMatter.FullRowSelect = true;
            setColumns();
            lvwColumnSorterClient = new ListViewColumnSorter();
            this.listViewClient.ListViewItemSorter = lvwColumnSorterClient;
            lvwColumnSorterMatter = new ListViewColumnSorter();
            this.listViewMatter.ListViewItemSorter = lvwColumnSorterMatter;

            if (ClientMatterOption == "C") //they chose "Client Only" in the main GUI
            {
                listViewMatter.Visible = false;
                label2.Visible = false;
                clientOnly = true;
                matterOnly = false;
                both = false;
                loadClientList(fromFeeSched, _jurisUtility);
            }
            else if (ClientMatterOption == "M") //they chose "Matter Only" in the main GUI
            {
                listViewClient.Visible = false;
                label1.Visible = false;
                matterOnly = true;
                clientOnly = false;
                both = false;
                loadMatterList(fromFeeSched, _jurisUtility, MatterStatusOption);
            }
            else
            {
                clientOnly = false;
                matterOnly = false;
                both = true;
                loadClientList(fromFeeSched, _jurisUtility);
                loadMatterList(fromFeeSched, _jurisUtility, MatterStatusOption);
            }
        }

        private ListViewColumnSorter lvwColumnSorterClient;

        private ListViewColumnSorter lvwColumnSorterMatter;

        public string matterIDs = "";

        public string clientIDs = "";

        public bool clientOnly;

        public bool matterOnly;

        public bool both;

        public bool canLoad = true;

        public void setColumns()
        {
            listViewClient.Columns.Add("Client ID", 25, HorizontalAlignment.Left);
            listViewClient.Columns.Add("Client Code", 75, HorizontalAlignment.Left);
            listViewClient.Columns.Add("Client Name", 250, HorizontalAlignment.Left);

            listViewMatter.Columns.Add("Matter ID", 25, HorizontalAlignment.Left);
            listViewMatter.Columns.Add("Matter Code", 75, HorizontalAlignment.Left);
            listViewMatter.Columns.Add("Matter Name", 250, HorizontalAlignment.Left);
            listViewMatter.Columns.Add("Client Code", 75, HorizontalAlignment.Left);
            listViewMatter.Columns.Add("Client Name", 250, HorizontalAlignment.Left);
        }

        private void loadClientList(string fromFeeSched, JurisUtility _jurisUtility)
        {
            string SQLTkpr = "SELECT [CliSysNbr] ,dbo.jfn_formatclientcode(clicode) as CliCode,[CliNickName] FROM [Client] where CliFeeSch = '" + fromFeeSched + "'";
            DataSet myRSTkpr = _jurisUtility.RecordsetFromSQL(SQLTkpr);

            if (myRSTkpr.Tables[0].Rows.Count == 0)
            {
                MessageBox.Show("There are no clients in this database with that fee schedule. No clients will be listed.", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                if (clientOnly)
                    canLoad = false;
            }
            else
            {

                foreach (DataRow dr in myRSTkpr.Tables[0].Rows)
                {
                    ListViewItem lvi = new ListViewItem(dr["CliSysNbr"].ToString());
                    lvi.SubItems.Add(dr["CliCode"].ToString().Trim());
                    lvi.SubItems.Add(dr["CliNickName"].ToString());
                    listViewClient.Items.Add(lvi);
                }
            }
        }

        private void loadMatterList(string fromFeeSched, JurisUtility _jurisUtility, string matterStatus)
        {
            //if they chose open matters or closed matters, SQL will be appended to end. If they chose all, then status is blank so it does nothing
            string SQLTkpr = "SELECT dbo.jfn_formatclientcode(clicode) as CliCode,[CliNickName], MatSysNbr, dbo.jfn_formatmattercode(matcode) as MatCode, MatNickName FROM [matter] inner join client on MatCliNbr = clisysnbr where MatFeeSch = '" + fromFeeSched + "'" + matterStatus;
            DataSet myRSTkpr = _jurisUtility.RecordsetFromSQL(SQLTkpr);

            if (myRSTkpr.Tables[0].Rows.Count == 0)
            {
                MessageBox.Show("There are no matters in this database with that fee schedule. No matters will be listed", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                if (matterOnly)
                    canLoad = false;
            }
            else
            {

                foreach (DataRow dr in myRSTkpr.Tables[0].Rows)
                {
                    ListViewItem lvi = new ListViewItem(dr["MatSysNbr"].ToString());
                    lvi.SubItems.Add(dr["MatCode"].ToString().Trim());
                    lvi.SubItems.Add(dr["MatNickName"].ToString());
                    lvi.SubItems.Add(dr["CliCode"].ToString().Trim());
                    lvi.SubItems.Add(dr["CliNickName"].ToString());
                    listViewMatter.Items.Add(lvi);

                }
            }
        }





        private void listViewClient_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == lvwColumnSorterClient.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorterClient.Order == SortOrder.Ascending)
                {
                    lvwColumnSorterClient.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorterClient.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorterClient.SortColumn = e.Column;
                lvwColumnSorterClient.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            this.listViewClient.Sort();
        }

        private void listViewMatter_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == lvwColumnSorterMatter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorterMatter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorterMatter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorterMatter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorterMatter.SortColumn = e.Column;
                lvwColumnSorterMatter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            this.listViewMatter.Sort();
        }

        private void listViewClient_MouseClick(object sender, MouseEventArgs e)
        {
            var where = listViewClient.HitTest(e.Location);

            if (where.Location == ListViewHitTestLocations.Label)
            {
                where.Item.Checked = true;
            }
        }

        private void listViewMatter_MouseClick(object sender, MouseEventArgs e)
        {
            var where = listViewMatter.HitTest(e.Location);

            if (where.Location == ListViewHitTestLocations.Label)
            {
                where.Item.Checked = true;
            }
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonContinue_Click(object sender, EventArgs e)
        {
            clientIDs = "";
            matterIDs = "";

            foreach (ListViewItem eachItem in listViewClient.CheckedItems)
            {
                clientIDs = clientIDs + eachItem.SubItems[0].Text + ","; //get the client id from each item selected
            }
            clientIDs = clientIDs.TrimEnd(',');

            foreach (ListViewItem eachItem in listViewMatter.CheckedItems)
            {
                matterIDs = matterIDs + eachItem.SubItems[0].Text + ","; //get the client id from each item selected
            }
            matterIDs = matterIDs.TrimEnd(',');

            if (clientOnly && string.IsNullOrEmpty(clientIDs))
                MessageBox.Show("At least one client selection is required with the chosen settings", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else if (matterOnly && string.IsNullOrEmpty(matterIDs))
                MessageBox.Show("At least one matter selection is required with the chosen settings", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else if (both)
            {
                if (string.IsNullOrEmpty(clientIDs) || string.IsNullOrEmpty(matterIDs))
                    MessageBox.Show("At least one matter and client selection is required with the chosen settings", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    this.Hide();
            }
            else
                this.Hide();
        }
    }
}
