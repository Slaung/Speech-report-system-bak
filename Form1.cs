using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Form1 : Form
    {
        #region 自定義變數

        /// <summary>
        /// 定義套接字Socket
        /// </summary>
        Socket SocketClient = null;

        /// <summary>
        /// Client意圖 (向Server傳資料都使用他來傳送)
        /// </summary>
        private string intent = null;

        /// <summary>
        /// Client意圖名稱: 接線員或消防局連線
        /// </summary>
        private string cmd01 = "cmd01"; // 此變數必須為cmd01，否則Server會有錯誤。

        /// <summary>
        /// Client意圖名稱: 消防局傳送已派車
        /// </summary>
        private string cmd04 = "cmd04"; // 此變數必須為cmd04，否則Server會有錯誤。

        /// <summary>
        /// Client意圖名稱: 消防局傳送已歸隊
        /// </summary>
        private string cmd05 = "cmd05"; // 此變數必須為cmd05，否則Server會有錯誤。

        /// <summary>
        /// Client意圖名稱: Server傳送案件狀況表
        /// </summary>
        private string cmd06 = "cmd06"; // 此變數必須為cmd06，否則Server會有錯誤。

        /// <summary>
        /// Client意圖名稱: 消防局回報送修消防車、救護車車數 => 更新資料庫
        /// </summary>
        private string cmd07 = "cmd07"; // 此變數必須為cmd07，否則Server會有錯誤。


        /// <summary>
        /// 連線情況變數
        /// </summary>
        string connect = null;

        /// <summary>
        /// 此消防局ID
        /// </summary>
        string ClientId = null;

        int port = 0;
        string host = null;

        /// <summary>
        /// 報案編號
        /// </summary>
        public string rId = null;

        /// <summary>
        /// 各分局全部消防車數
        /// </summary>
        public int allF = 0;

        /// <summary>
        /// 各分局全部救護車數
        /// </summary>
        public int allA = 0;

        /// <summary>
        /// 各分局消防車數減去待值勤和值勤中的車數
        /// </summary>
        public int allF2 = 0;

        /// <summary>
        /// 各分局全部救護車數減去待值勤和值勤中的車數
        /// </summary>
        public int allA2 = 0;

        /// <summary>
        /// 維修中消防車數
        /// </summary>
        public int Fservicing = 0;

        /// <summary>
        /// 維修中救護車數
        /// </summary>
        public int Aservicing = 0;

        /// <summary>
        /// 取得選取的ID
        /// </summary>
        public string Get_Output_Id = null;

        /// <summary>
        /// 取得選取的時間
        /// </summary>
        public string Get_Output_Time = null;

        /// <summary>
        /// 取得選取的地址
        /// </summary>
        public string Get_Output_Address = null;

        /// <summary>
        /// 取得選取的案件
        /// </summary>
        public string Get_Output_Cases = null;

        /// <summary>
        /// 取得選取的受傷人數
        /// </summary>
        public string Get_Output_Injured = null;

        /// <summary>
        /// 取得選取的受傷人數
        /// </summary>
        public string Get_Output_Fire_truck = null;

        /// <summary>
        /// 取得選取的受傷人數
        /// </summary>
        public string Get_Output_Ambulance = null;

        /// <summary>
        /// 取得案件狀態
        /// </summary>
        public string Get_Output_Status = null;

        /// <summary>
        /// 從Server傳來的所有案件資訊(包含歷史案件)
        /// </summary>
        public static string[] historyReport = null;

        #endregion

        #region 自定義變數 get & set
        /// <summary>
        /// 設定意圖 cmd: 參照自定義變數的意圖, msg: 發送的消息
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="msg"></param>
        private void setIntent(string cmd, string msg)
        {
            this.intent = cmd + " " + msg;
        }

        #endregion


        public Form1()
        {
            InitializeComponent();
            label7.ForeColor = Color.Red;
            button2.Enabled = false;
            getCarsNum();
        }

        public void Start()
        {
            try
            {
                // 設置Socket
                IPAddress ip = IPAddress.Parse(host);
                IPEndPoint ipe = new IPEndPoint(ip, port);

                //建立套接字
                SocketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    // Client端的套接連線到網路節點上(連線請求)
                    SocketClient.Connect(ipe);

                    // 向Server發送連線訊息
                    setIntent(cmd01, ClientId);
                    AsyncSend(SocketClient, intent);
                }
                catch (Exception)
                {
                    connect = null;
                    return; // 連線失敗
                }

                // 新增背景執行緒，去做後續資料傳輸處理。
                Thread ThreadClient = new Thread(Recv);
                ThreadClient.IsBackground = true;
                ThreadClient.Start();
                Thread.Sleep(1000);
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        ///  向Server傳送訊息 (連線or派車or歸隊直接呼叫使用)
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="intent"></param>
        public void AsyncSend(Socket socket, string intent) // 此為非同步作業
        {
            if (socket == null || intent == string.Empty) return;
            //編碼
            byte[] data = Encoding.UTF8.GetBytes(intent);
            try
            {
                socket.BeginSend(data, 0, data.Length, SocketFlags.None, asyncResult =>
                {
                    //完成傳送訊息
                    int length = socket.EndSend(asyncResult);
                }, null);
            }
            catch (Exception) // Server中斷連線
            {
                connect = null;
                setText("Discontinue");
                connect = null;
            }
        }

        /// <summary>
        /// 持續監聽服務端發來的訊息 (此為非同步作業)
        /// </summary>
        public void Recv()
        {
            while (true)
            {
                try
                {
                    // 定義1M記憶體緩衝區 用來臨時性儲存接收到的訊息
                    byte[] arrRecvmsg = new byte[1024 * 1024];

                    // 將客戶端套接字接收到的資料存入記憶體緩衝區 取得長度
                    int length = SocketClient.Receive(arrRecvmsg);

                    // 將套接字獲取到的字元陣列轉為人可以看懂得字串
                    string strRevMsg = Encoding.UTF8.GetString(arrRecvmsg, 0, length);
                    if (strRevMsg == "RepeatID")
                    {
                        setText(strRevMsg);
                        MessageBox.Show("與伺服器連線中斷(分局名稱重複!)");
                        SocketClient.Close(); // 關閉客問端套接字 
                        connect = null;
                        break;
                    }
                    else if (strRevMsg.Substring(0, 5) == "cmd03") // 當接收到報案資訊時
                    {
                        setText(strRevMsg);
                    }
                    else if (strRevMsg.Substring(0, 5) == "cmd06") // 當接收到車數和案件狀況時
                    {
                        setText(strRevMsg);
                        connect = "Succ";
                    }
                    else if (strRevMsg.Substring(0, 5) == "eor01") // 分局名稱重複
                    {
                        connect = null;
                        setText(strRevMsg);
                        MessageBox.Show("與伺服器連線中斷(分局名稱重複!)");
                    }
                }
                catch (Exception) // Server中斷連線
                {
                    MessageBox.Show("與伺服器連線中斷");
                    connect = null;
                    break;
                }
            }
        }

        /// <summary>
        /// 開始連線
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            port = Convert.ToInt32(textBox8.Text);
            host = textBox7.Text;
            ClientId = comboBox1.Text;

            if (ClientId == "")
            {
                MessageBox.Show("請選擇分局名稱");
            }
            else
            {
                if (connect == "Succ")
                {
                    MessageBox.Show("已經連線成功");
                    label7.Text = "連線狀況:連線成功";
                    label7.ForeColor = Color.Green;
                }
                else
                {
                    Start();
                }
            }
        }

        /// <summary>
        /// 中斷連線
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                SocketClient.Close(); // 關閉客問端套接字 
                connect = null;
                label7.Text = "連線狀況: 未連線";
                label7.ForeColor = Color.Red;
                textBox1.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
                textBox4.Text = "";
                textBox5.Text = "";
                textBox6.Text = "";
                textBox9.Text = "";
                textBox10.Text = "";
                listView1.Items.Clear();
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("已中斷連線!");
            }
        }

        /// <summary>
        /// 更新介面
        /// </summary>
        /// <param name="str"></param>
        private void setText(string str)
        {
            try
            {
                // 判斷跨執行緒的問題並導正回主執行緒
                if (this.InvokeRequired)
                {
                    this.Invoke(new MethodInvoker(() => setText(str)));
                }
                else
                {
                    if (str.Substring(0, 5) == "cmd03") // 報案資訊傳入
                    {
                        showReportText(str);
                    }
                    else if (str.Substring(0, 5) == "cmd06") // Server傳的連線成功資訊
                    {
                        label7.Text = "連線狀況:連線成功";
                        label7.ForeColor = Color.Green;
                        button2.Enabled = true;
                        showImmediateCarsNum(str);//即時車數
                        showCheckBoxText(str);
                    }
                }
            }
            catch
            {
                label7.Text = "連線狀況: 未連線";
                label7.ForeColor = Color.Red;
            }

        }

        /// <summary>
        /// 顯示接線員傳來的報案資訊
        /// </summary>
        /// <param name="str"></param>
        private void showReportText(string str)
        {
            // 報案資訊內容: str =  "cmd03,案件對話:喂/喂/不好意思我要叫救護車/嘿是怎樣齁/這裡發生車禍/地址給我/大同里大同路18號/好派車過去齁,案件時間:2021-02-19 15:33:00,案件地點:通霄鎮南和路二段463巷2弄7號5樓,案件類別:車禍案件,受傷人數:3,消防車數:0,救護車數:3,報案編號:1"
            string[] splitStr = str.Split(',');// [0]=判斷客戶端意圖 [1]=時間 [2]=地址 [3]=災害類別 [4]=受傷人數 [5]=對話 [6]=消防車數 [7]=救護車數 [8]=報案編號
            textBox1.Text = splitStr[1].Substring(5, splitStr[1].Length - 5);// 案件時間
            textBox2.Text = splitStr[2].Substring(5, splitStr[2].Length - 5);// 案件地點
            textBox3.Text = splitStr[3].Substring(5, splitStr[3].Length - 5);// 案件類別
            textBox4.Text = splitStr[6].Substring(5, splitStr[6].Length - 5);// 消防車數
            textBox5.Text = splitStr[7].Substring(5, splitStr[7].Length - 5);// 救護車數
            textBox6.Text = splitStr[4].Substring(5, splitStr[4].Length - 5);// 受傷人數
            rId = splitStr[8].Substring(5, splitStr[8].Length - 5);// 報案編號
        }

        /// <summary>
        /// 顯示出勤狀況
        /// </summary>
        /// <param name="str"></param>
        public void showCheckBoxText(string str)
        {
            getCarsNum();
            allF2 = allF;
            allA2 = allA;
            string newstr = "";

            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '案') // 這個消防局有處理過案件
                {
                    for (int j = i; j < str.Length; j++) // 把案件資訊另存到newstr，去掉前面的即時車數
                    {
                        newstr += str[j];
                    }
                    break;
                }
            }
            
            historyReport = newstr.Split('!'); // 分幾筆案件
            listView1.Items.Clear();
            for (int i = 0; i < historyReport.Length - 1; i++) // [0] = 案件時間: [1] = 案件地點: [2] = 案件類別: [3] = 受傷人數:  [4] = 報案編號: [5] = 消防車數: [6] = 救護車數: [7] = 案件狀態: 
            {
                string[] historyReport_Spilt = historyReport[i].Split(',');
                ListViewItem lvi = new ListViewItem();
                lvi.Text = historyReport_Spilt[4].Substring(5, historyReport_Spilt[4].Length - 5); // 報案編號
                lvi.SubItems.Add(historyReport_Spilt[2].Substring(5, historyReport_Spilt[2].Length - 5));//案件類別
                lvi.SubItems.Add(historyReport_Spilt[1].Substring(5, historyReport_Spilt[1].Length - 5));//案件地址
                lvi.SubItems.Add(historyReport_Spilt[7].Substring(5, historyReport_Spilt[7].Length - 5));//案件狀態
                lvi.SubItems.Add(historyReport_Spilt[0].Substring(5, historyReport_Spilt[0].Length - 5));//案件時間
                lvi.SubItems.Add(historyReport_Spilt[5].Substring(5, historyReport_Spilt[5].Length - 5));//消防車數
                lvi.SubItems.Add(historyReport_Spilt[6].Substring(5, historyReport_Spilt[6].Length - 5));//救護車數
                lvi.SubItems.Add(historyReport_Spilt[3].Substring(5, historyReport_Spilt[3].Length - 5));//受傷人數
                this.listView1.Items.Add(lvi);
                if (listView1.Items[i].SubItems[3].Text == "待值勤")
                {
                    this.listView1.Items[i].UseItemStyleForSubItems = false;
                    this.listView1.Items[i].SubItems[3].ForeColor = Color.Red;
                }
                if (listView1.Items[i].SubItems[3].Text != "已歸隊") // 設置維修車數下拉表單用
                {
                    allF2 -= Convert.ToInt32(listView1.Items[i].SubItems[5].Text);
                    allA2 -= Convert.ToInt32(listView1.Items[i].SubItems[6].Text);
                }
            }

            
        }


        /// <summary>
        /// 取得各分局自己的全部車數
        /// </summary>
        /// <param name="str"></param>
        public void getCarsNum()
        {
            string[] branches = { "苗栗", "頭屋", "公館", "銅鑼", "三義", "竹南", "後龍", "造橋", "頭份", "三灣", "南庄", "西湖", "通霄", "苑裡", "獅潭", "大湖", "卓蘭", "泰安", "象鼻" };//所有分局
            int[] f = { 8, 3, 4, 3, 4, 6, 6, 4, 8, 3, 4, 6, 8, 8, 3, 5, 4, 2, 2 };
            int[] a = { 5, 1, 2, 1, 1, 2, 2, 1, 3, 1, 1, 1, 2, 1, 1, 2, 1, 1, 1 };
            for (int i = 0; i < 19; i++)
            {
                if (branches[i] == ClientId)
                {
                    allF = f[i];
                    allA = a[i];
                }
            }
        }


        /// <summary>
        /// 顯示即時車數
        /// </summary>
        /// <param name="str"></param>
        public void showImmediateCarsNum(string str)
        {
            // 報案資訊內容: str =  "cmd06,消防車數:8,救護車數:5"
            string[] splitStr = str.Split(',');// [0]=判斷客戶端意圖 [1]=消防車數 [2]=救護車數
            textBox9.Text = splitStr[1].Substring(5, splitStr[1].Length - 5);
            textBox10.Text = splitStr[2].Substring(5, splitStr[2].Length - 5);
        }


        /// <summary>
        /// 通知Server扣除SQL fire_branch資料表裡該分局的消防車或救護車數量 cmd04 : Client意圖名稱: 消防局傳送已派車
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            if (Get_Output_Status == "值勤中")
            {
                MessageBox.Show("此案件值勤中 ! 請確認案件資訊");
            }
            else if (Get_Output_Status == "已歸隊")
            {
                MessageBox.Show("此案件已歸隊 ! 請確認案件資訊");
            }
            else if (textBox4.Text == "" || textBox5.Text == "" || rId == "")
            {
                MessageBox.Show("請先點選案件");
            }
            else
            {
                string msg = ",分局名稱:" + comboBox1.Text + ",消防車數:" + textBox4.Text + ",救護車數:" + textBox5.Text + ",報案編號:" + rId;
                setIntent(cmd04, msg);
                AsyncSend(SocketClient, intent);
            }
            //清空所有textbox
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
            textBox5.Text = "";
            textBox6.Text = "";
        }

        /// <summary>
        /// 通知Server加回SQL fire_branch資料表裡該分局的消防車或救護車數量 cmd05 : Client意圖名稱: 消防局傳送已歸隊
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            if (Get_Output_Status == "待值勤")
            {
                MessageBox.Show("此案件尚未派車 ! 請確認案件資訊");
            }
            else if (Get_Output_Status == "已歸隊")
            {
                MessageBox.Show("此案件已歸隊 ! 請確認案件資訊");
            }
            else
            {
                //讀取listView被選中的值
                string rid = Get_Output_Id;
                string msg = ",分局名稱:" + ClientId + ",報案編號:" + rid;
                setIntent(cmd05, msg);
                AsyncSend(SocketClient, intent);
            }
        }

        /// <summary>
        /// 開啟車輛送修回報介面,回報Server送修車數
        /// </summary>
        public void button5_Click(object sender, EventArgs e)
        {
            Form2 f = new Form2();
            f.Fnum = allF2;  //  設置Form2中即時消防車數fnum的值(已扣掉待執勤的車數)  
            f.Anum = allA2; //  設置Form2中即時救護車數anum的值(已扣掉待執勤的車數)  
            f.Fservicing = allF2 - Convert.ToInt32(textBox9.Text);  //  設置Form2中維修中消防車數anum的值
            f.Aservicing = allA2 - Convert.ToInt32(textBox10.Text); //  設置Form2中維修中救護車數anum的值
            f.SetValue();//設置Form2中comboBox1、comboBox2、comboBox3、comboBox4 下拉式選單的值

            f.ShowDialog(this); //設定Form2為Form1的上層，並開啟Form2視窗。由於在Form1的程式碼內使用this，所以this為Form1的物件本身
            if (f.DialogResult == System.Windows.Forms.DialogResult.OK) //若使用者在Form2按下了OK，則進入這個判斷式
            {
                string msg = ",分局名稱:" + comboBox1.Text + ",送修消防車數:" + f.serviceFire_truck + ",送修救護車數:" + f.serviceAmbulance + ",送修歸隊消防車數:" + f.backFire_truck + ",送修歸隊救護車數:" + f.backAmbulance;
                setIntent(cmd07, msg);
                AsyncSend(SocketClient, intent);
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                var selectedItem = listView1.SelectedItems[0];
                Get_Output_Id = selectedItem.SubItems[0].Text;
                Get_Output_Cases = selectedItem.SubItems[1].Text;
                Get_Output_Address = selectedItem.SubItems[2].Text;
                Get_Output_Status = selectedItem.SubItems[3].Text;
                Get_Output_Time = selectedItem.SubItems[4].Text;
                Get_Output_Fire_truck = selectedItem.SubItems[5].Text;
                Get_Output_Ambulance = selectedItem.SubItems[6].Text;
                Get_Output_Injured = selectedItem.SubItems[7].Text;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            textBox1.Text = Get_Output_Time;
            textBox2.Text = Get_Output_Address;
            textBox3.Text = Get_Output_Cases;
            textBox4.Text = Get_Output_Fire_truck;
            textBox5.Text = Get_Output_Ambulance;
            textBox6.Text = Get_Output_Injured;
            rId = Get_Output_Id;
        }


    }
}
