using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Operator
{
    public partial class Form2 : Form
    {
        #region 自定義變數

        /// <summary>
        /// 取的選取的ID
        /// </summary>
        public string Get_Output_Id = null;

        /// <summary>
        /// 取的選取的時間
        /// </summary>
        public string Get_Output_Time = null;

        /// <summary>
        /// 取的選取的地址
        /// </summary>
        public string Get_Output_Address = null;

        /// <summary>
        /// 取的選取的案件
        /// </summary>
        public string Get_Output_Cases = null;

        /// <summary>
        /// 取的選取的受傷人數
        /// </summary>
        public string Get_Output_Injured = null;

        /// <summary>
        /// 取的選取的對話
        /// </summary>
        public string Get_Output_Dialogue = null;

        /// <summary>
        /// 從Server傳來的未處理報案資訊
        /// </summary>
        public static string[] Untreat_Report = null;

        #endregion
        public Form2()
        {
            InitializeComponent();
            button1.DialogResult = System.Windows.Forms.DialogResult.OK; // 設定button4為OK
        }
        public Form2(string strMsg)
        {
            InitializeComponent();
            Untreat_Report = strMsg.Split(',');
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            var report = getReportList();
            listView1.Items.Clear();
            foreach (var r in report)
            {
                var row = new string[] { r.Id.ToString(), r.time, r.address, r.cases, r.injured.ToString(), r.dialogue };
                var lvi = new ListViewItem(row);
                lvi.Tag = r;
                listView1.Items.Add(lvi);
            }
            if (Untreat_Report != null)
            {
                label1.Text = "目前尚有: " + (Untreat_Report.Length - 1) + "個案件待處理";
            }
            
        }

        /// <summary>
        /// 向Server請求未處理的案件。
        /// </summary>
        /// <returns></returns>
        private List<Report> getReportList()
        {
            var list = new List<Report>();
            if (Untreat_Report != null)
            {
                for (int i = 0; i < Untreat_Report.Length - 1; i++)
                {
                    string[] Untreat_Report_Spilt = Untreat_Report[i].Split('/');
                    list.Add(new Report { Id = Convert.ToInt32(Untreat_Report_Spilt[0]), time = Untreat_Report_Spilt[1], address = Untreat_Report_Spilt[2], cases = Untreat_Report_Spilt[3], injured = Convert.ToInt32(Untreat_Report_Spilt[4]), dialogue = Untreat_Report_Spilt[5] });
                }
            }
            return list;
        }

        /// <summary>
        /// 每次點選listView1時，就更新欲回傳的各變數。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var selectedItem = (Report)listView1.SelectedItems[0].Tag;
                Get_Output_Id = selectedItem.Id.ToString();
                Get_Output_Time = selectedItem.time;
                Get_Output_Address = selectedItem.address;
                Get_Output_Cases = selectedItem.cases;
                Get_Output_Injured = selectedItem.injured.ToString();
                Get_Output_Dialogue = selectedItem.dialogue;
            }
            catch
            {

            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
