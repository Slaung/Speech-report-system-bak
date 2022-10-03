using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace Operator
{
    public partial class Form1 : Form
    {
        #region 自定義變數

        /// <summary>
        /// 定義套接字Socket
        /// </summary>
        public static Socket SocketClient = null;

        /// <summary>
        /// 接線員ID (目前以單一接線員做測試，之後改多個接線員時，介面可能也要做登入方式，去設置接線員ID)
        /// </summary>
        private string Operator_Name = null;

        /// <summary>
        /// Client意圖 (向Server傳資料都使用他來傳送)
        /// </summary>
        private string intent = null;

        /// <summary>
        /// Client意圖: 接線員或消防局連線
        /// </summary>
        private string cmd01 = "cmd01"; // 此變數必須為cmd01，否則Server不知道意圖。

        /// <summary>
        /// Client意圖: 接線員傳送報案訊息
        /// </summary>
        private string cmd02 = "cmd02"; // 此變數必須為cmd02，否則Server不知道意圖。

        /// <summary>
        /// Client意圖: 接線員傳送派車請求
        /// </summary>
        private string cmd03 = "cmd03"; // 此變數必須為cmd03，否則Server不知道意圖。

        /// <summary>
        /// Client意圖: 接線員請求資料庫，取得未處理的案件。
        /// </summary>
        private string cmd08 = "cmd08"; // 此變數必須為cmd04，否則Server不知道意圖。

        /// <summary>
        /// 取得未處理的案件封包，之後由Form2拆解。
        /// </summary>
        public string cmd08_str = null;

        /// <summary>
        /// 選擇完未處理案件後，停止不斷更新介面的觸發器。
        /// </summary>
        private Boolean The_Trigger = false;

        /// <summary>
        /// 上傳案件的標籤，判斷使用者這輪的案件有沒有上傳過。
        /// </summary>
        private Boolean Flag1 = true;

        /// <summary>
        /// 連線情況變數
        /// </summary>
        string connect = null;

        /// <summary>
        /// 介面所顯示專屬Rid
        /// </summary>
        string Rid = null;

        int port = 0;
        string host = null;

        /// <summary>
        /// 介面計數器
        /// </summary>
        int interface_count = 0;

        #endregion

        #region 自定義變數 get & set
        /// <summary>
        /// 設定意圖 cmd: 參照自定義變數的意圖, msg: 發送的消息
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="msg"></param>
        public void setIntent(string cmd, string msg)
        {
            this.intent = cmd + " " + msg;
        }
        #endregion

        public Form1()
        {
            InitializeComponent();
            label7.ForeColor = Color.Red;
            label6.ForeColor = Color.Red;
            button6.Enabled = false;
            button7.Enabled = false;
        }

        #region Socket

        /// <summary>
        /// Socket
        /// </summary>
        public void Start()
        {
            try
            {
                // 設置Socket
                IPAddress ip = IPAddress.Parse(host);
                IPEndPoint ipe = new IPEndPoint(ip, port);

                // 建立套接字
                SocketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    // Client端的套接連線到網路節點上(連線請求)
                    SocketClient.Connect(ipe);

                    // 向Server發送連線訊息
                    setIntent(cmd01, Operator_Name);
                    ClientSendMsg(intent);
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
                Thread.Sleep(1000); // 1s內暫止目前的執行緒 ArgumentOutOfRangeException(逾時值為負且不等於 Infinite)
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// 持續監聽服務端發來的訊息
        /// </summary>
        private void Recv()
        {
            while (true)
            {
                try
                {
                    // 定義1M記憶體緩衝區 用來臨時性儲存接收到的訊息
                    byte[] arrRecvmsg = new byte[1024 * 1024];

                    // 將客戶端Socket接收到的資料存入記憶體緩衝區 取得長度
                    int length = SocketClient.Receive(arrRecvmsg);

                    // 將套接字獲取到的字元陣列轉為人可以看懂得字串
                    string strRevMsg = Encoding.UTF8.GetString(arrRecvmsg, 0, length);

                    if (strRevMsg.Substring(0, 5) == "cmd01")
                    {
                        connect = "Succ";
                        setText();
                        MessageBox.Show("連線成功!");
                    }
                    else if (strRevMsg.Substring(0, 5) == "eor01")
                    {
                        connect = null;
                        setText();
                        MessageBox.Show("與伺服器連線中斷(Robot名稱重複!)");
                    }
                    else if (strRevMsg.Substring(0, 5) == "eor02")
                    {
                        Rid = "";
                        Flag1 = true;
                        connect = "RepeatReport";
                        setText();
                        MessageBox.Show("此案件已報案過了!");
                    }
                    else if (strRevMsg.Substring(0, 5) == "cmd02")
                    {
                        Rid = strRevMsg.Substring(6, strRevMsg.Length - 6); // 取得Rid 封包格式:cmd02 Rid
                        Flag1 = true;
                        connect = "ReportSucc";
                        setText();
                        MessageBox.Show("案件上傳成功!");
                    }
                    else if (strRevMsg.Substring(0, 5) == "cmd03")
                    {
                        setText();
                        MessageBox.Show("此案件已派車完畢!");
                    }
                    else if (strRevMsg.Substring(0, 5) == "eor03")
                    {
                        string Showstr = strRevMsg.Substring(6, strRevMsg.Length - 6);
                        setText();
                        MessageBox.Show(Showstr);
                    }
                    else if (strRevMsg.Substring(0, 5) == "cmd04")
                    {
                        string needFnum = strRevMsg.Substring(6, strRevMsg.Length - 6);
                        setText();
                        MessageBox.Show("此案件尚有" + needFnum + "台消防車未派遣");
                    }
                    else if (strRevMsg.Substring(0, 5) == "cmd05")
                    {
                        string needAnum = strRevMsg.Substring(6, strRevMsg.Length - 6);
                        setText();
                        MessageBox.Show("此案件尚有" + needAnum + "台救護車未派遣");
                    }
                    else if (strRevMsg.Substring(0, 5) == "cmd06")
                    {
                        setText();
                        MessageBox.Show("成功配置派遣分局!");
                    }
                    else if (strRevMsg.Substring(0, 5) == "cmd08") // 顯示未處理的案件
                    {
                        cmd08_str = strRevMsg.Substring(5, strRevMsg.Length - 5);
                        showUntreatReport(cmd08_str);
                    }
                }
                catch (Exception) // Server中斷連線
                {
                    connect = null;
                    setText();
                    break;
                }
            }
        }

        /// <summary>
        /// 傳送資訊到Server端的方法
        /// </summary>
        /// <param name="sendMsg"></param>
        public string ClientSendMsg(string sendMsg)
        {
            // 輸入內容的字串轉為機器識別的位元組陣列
            byte[] arrClientSendMsg = Encoding.UTF8.GetBytes(sendMsg);
            try
            {
                // 呼叫客戶端套接字傳送位元組陣列
                SocketClient.Send(arrClientSendMsg);
                return "Succ";
            }
            catch (SocketException) // 嘗試存取通訊端時發生錯誤
            {
                return "SocketError";
            }
            catch (NullReferenceException)
            {
                return "Error";
            }
            catch (ObjectDisposedException) // Socket已關閉
            {
                return "Error";
            }
        }

        #endregion

        /// <summary>
        /// 開始連線
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            Operator_Name = comboBox1.Text;
            if (Operator_Name == "" || textBox8.Text == "" || textBox7.Text == "")
            {
                MessageBox.Show("請填寫連線資訊!");
            }
            else
            {
                port = Convert.ToInt32(textBox8.Text);
                host = textBox7.Text;
                if (connect == "Succ")
                {
                    MessageBox.Show("已經連線成功");
                }
                else
                {
                    Start();
                }
            }
        }

        /// <summary>
        /// 更新介面
        /// </summary>
        public void setText()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new MethodInvoker(() => setText()));
                }
                else
                {
                    
                    if (connect == null)
                    {
                        button6.Enabled = false;
                        MessageBox.Show("與伺服器連線中斷");
                        label7.Text = "連線狀況: 未連線";
                        label7.ForeColor = Color.Red;

                    }
                    else
                    {
                        textBox4.Text = Rid;
                        button6.Enabled = true;
                        label7.Text = "連線狀況:連線成功";
                        label7.ForeColor = Color.Green;
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
        /// 顯示robot speek與user speek
        /// </summary>
        private string splitDialogue(string dialogue)
        {
            string[] Split_Dialogue = dialogue.Split(',');
            string output = null;
            for (int i = 0; i < Split_Dialogue.Length - 1; i++)
            {
                if (i % 2 == 0) // robot speek
                {
                    output += "robot: " + Split_Dialogue[i] + "\r\n";
                }
                else // user speek
                {
                    output += "  user: " + Split_Dialogue[i] + "\r\n";
                }
            }
            return output;
        }

        /// <summary>
        /// 對話內容顯示
        /// </summary>
        /// <param name="output"></param>
        private void updateOutput(string output)
        {
            string[] Split_Output = output.Split('/');

            try
            {
                if (Split_Output.Length == 1) // ASR無法辨識時的處理
                {
                    //textBox6.Text = Split_Output[0];
                }
                else
                {
                    textBox4.Text = "";
                    textBox6.Text = splitDialogue(Split_Output[9]);
                    for (int i = 0; i <= 6; i++)
                    {
                        textBox2.Text += Split_Output[i];
                    }
                    textBox2.Text = Split_Output[0] + Split_Output[1] + Split_Output[2] + Split_Output[3] + Split_Output[4] + Split_Output[5] + Split_Output[6];
                    textBox3.Text = Split_Output[7];
                    textBox10.Text = Split_Output[8];
                    textBox6.Text = splitDialogue(Split_Output[9]);
                }
                if (interface_count == 1)
                {
                    string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    textBox1.Text = time;
                    interface_count = 2;
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// 初始話介面
        /// </summary>
        private void setOutput(string output)
        {
            string[] Split_Output = output.Split('/');
            if (interface_count == 0)
            {
                textBox1.Text = "";
                //string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                //textBox1.Text = time;
                interface_count = 1;
            }
            if (Split_Output.Length == 1) // ASR無法辨識時的處理
            {
                textBox4.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
                textBox6.Text = "";
                textBox10.Text = "";
            }
        }

        /// <summary>
        /// 初始化Data_Transmission.txt
        /// </summary>
        public void setLogTxt()
        {
            try
            {
                // Data_Transmission檔請自行在路徑底下新增空白txt即可
                StreamWriter sw = new StreamWriter(@"D:/大三專題/task_chatbot7/Data_Transmission.txt", false, Encoding.UTF8); // 第二參數為覆寫。
                sw.Write("");
                sw.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }

            try
            {
                // Data_Transmission檔請自行在路徑底下新增空白txt即可
                using (var sr = new StreamReader(@"D:/大三專題/task_chatbot7/Data_Transmission.txt", System.Text.Encoding.UTF8))
                {
                    setOutput(sr.ReadToEnd());
                    sr.Close();
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
        }

        /// <summary>
        /// 直接啟動Robot的py檔案(讀取約10秒，20秒才會開始說話)
        /// </summary>
        public void startCallPy()
        {

            string fileName = @"D:/大三專題/task_chatbot7/chatbot_run.py";

            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(@"C:\Users\user\Anaconda3\python.exe", fileName)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            p.Start();

            //string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            Console.ReadLine();
        }

        /// <summary>
        /// 以非同步方式讀取Data_Transmission.txt，並且顯示資訊。
        /// </summary>
        public void startLogTxt()
        {
            while (true)
            {
                if (The_Trigger == true)
                {
                    try
                    {
                        // 判斷機器人結束本次報案沒
                        using (var sr = new StreamReader(@"D:/大三專題/task_chatbot7/startinfo.txt", System.Text.Encoding.Default))
                        {
                            string str = sr.ReadToEnd();
                            sr.Close();
                            if (str == "end")
                            {
                                The_Trigger = false;
                                button9.Enabled = true;
                            }
                        }

                        // Data_Transmission檔請自行在路徑底下新增空白txt即可
                        using (var sr = new StreamReader(@"D:/大三專題/task_chatbot7/Data_Transmission.txt", System.Text.Encoding.Default))
                        {
                            string str = sr.ReadToEnd();
                            sr.Close();
                            updateOutput(str);
                        }
                    }
                    catch (IOException e)
                    {
                        
                    }
                }
                Thread.Sleep(500);
                Console.WriteLine("Trigger: " + The_Trigger);
            }
        }

        /// <summary>
        /// 顯示從Form2選取的未處理案件資訊
        /// </summary>
        /// <param name="str"></param>
        private void showUntreatReport(string str) // 傳入從server取得的所有未處理報案資訊
        {
            Form2 f = new Form2(str);
        }

        /// <summary>
        /// 中斷連線
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            // 線程需要關閉 TempThread.Abort()
            // 監聽需要關閉 lisner.Stop()
            // 關閉流文件 MyFileStream.close()
            try
            {
                SocketClient.Close(); // 關閉客戶端套接字 
                connect = null;
                label7.Text = "連線狀況: 未連線";
                label7.ForeColor = Color.Red;
                /* 正式時再移除註解
                textBox1.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
                textBox4.Text = "";
                textBox5.Text = "";
                textBox10.Text = "";
                */
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("已中斷連線!");
            }
        }

        /// <summary>
        /// 初始化系統
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            button4.Enabled = false;
            interface_count = 0;
            setLogTxt();

            // 設startCallPy為背景執行緒，並且開始呼叫機器人。
            Thread myThread = new Thread(startCallPy);
            Form.CheckForIllegalCrossThreadCalls = false;
            myThread.IsBackground = true;
            myThread.Start();

            // 設startLogTxt為背景執行緒，並且開始讀取log檔。
            Thread logThread = new Thread(startLogTxt);
            logThread.IsBackground = true;
            logThread.Start();

       
            // 判斷機器人是否初始化完畢
            while (true){
                try
                {
                    using (var sr = new StreamReader(@"D:/大三專題/task_chatbot7/startinfo.txt", System.Text.Encoding.Default))
                    {
                        string str = sr.ReadToEnd();
                        sr.Close();
                        if (str == "success")
                        {
                            The_Trigger = true;
                            label6.ForeColor = Color.Green;
                            label6.Text = "Chat-bot已啟動";
                            button7.Enabled = true;
                            button8.Enabled = true;
                            break;
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("初始化機器人失敗!");
                    break;
                }
                Thread.Sleep(3000);
            }
            
        }

        /// <summary>
        /// 開啟通話
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            if (Flag1 == false)
            {
                MessageBox.Show("請先上傳當前案件!");
            }
            else
            {
                Flag1 = false;
                button6.Enabled = false;
                button9.Enabled = false;
                interface_count = 0;
                The_Trigger = true;
                setLogTxt();
                // 改startinfo為success，否則重新通話介面不會一直更ㄒ因
                try
                {
                    // startinfo.txt檔請自行在路徑底下新增空白txt即可
                    StreamWriter sw = new StreamWriter(@"D:/大三專題/task_chatbot7/startinfo.txt", false, Encoding.UTF8); // 第二參數為覆寫。
                    sw.Write("success");
                    sw.Close();
                }
                catch (Exception)
                {
                    Console.WriteLine("Exception: ");
                }

                try
                {
                    // mode檔請自行在路徑底下新增空白txt即可
                    StreamWriter sw = new StreamWriter(@"D:/大三專題/task_chatbot7/mode.txt", false, Encoding.UTF8); // 第二參數為覆寫。
                    sw.Write("run");
                    sw.Close();
                }
                catch (Exception)
                {
                    Console.WriteLine("Exception: ");
                }
            }
        }

        /// <summary>
        /// 關閉通話
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                try
                {
                    // mode檔請自行在路徑底下新增空白txt即可
                    StreamWriter sw = new StreamWriter(@"D:/大三專題/task_chatbot7/mode.txt", false, Encoding.UTF8); // 第二參數為覆寫。
                    sw.Write("interrupt");
                    sw.Close();
                }
                catch (Exception)
                {
                    Console.WriteLine("Exception: ");
                }
            }
            catch
            {

            }
            The_Trigger = false;
            Flag1 = true;
            label6.ForeColor = Color.Red;
            label6.Text = "Chat-bot未啟動";
            button8.Enabled = false;
            button7.Enabled = false;
            button4.Enabled = true;
        }

        /// <summary>
        /// 未派遣案件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            // 1. 向Server請求此RobotID未派遣的所有報案資料。
            setIntent(cmd08, comboBox1.Text); // (cmd08 受理人員)
            string Server_INFO = ClientSendMsg(intent); // 發送 cmd02指令與報案訊息

            if (Server_INFO == "SocketError")
            {
                label7.Text = "連線狀況: 未連線";
                label7.ForeColor = Color.Red;
                MessageBox.Show("尚未連線，請求失敗");
            }
            else if (Server_INFO == "Error")
            {
                MessageBox.Show("請求失敗");
            }
            else if (Server_INFO == "SuccSendMsg")
            {

                //MessageBox.Show("上傳成功");
            }
            Thread.Sleep(500); // !!一定要延遲，沒延遲會導致第一次連線按下去後，看不到任何Data

            // 2. 將這些報案資料顯示於介面上。
            // 3. 顯示後點選某案件，按下選擇會離開此介面，並且顯示完整報案資訊。
            Form2 f = new Form2();
            f.ShowDialog(this);
            if (f.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                textBox6.Text = f.Get_Output_Dialogue;
                textBox4.Text = f.Get_Output_Id;
                textBox1.Text = f.Get_Output_Time;
                textBox2.Text = f.Get_Output_Address;
                textBox3.Text = f.Get_Output_Cases;
                textBox10.Text = f.Get_Output_Injured;
            }
            
        }

        /// <summary>
        /// 上傳案件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            if (Flag1 == false)
            {
                button6.Enabled = true;
            }
            if (textBox6.Text=="" || textBox1.Text == "" || textBox2.Text == "" || textBox3.Text == "" || textBox10.Text == "")
            {
                MessageBox.Show("請將報案資訊填寫完整!");
            }
            else if (textBox4.Text != "")
            {
                MessageBox.Show("此案件已經上傳過了!");
            }
            else
            {
                string msg = ",案件對話:" + textBox6.Text + ",案件時間:" + textBox1.Text + ",案件地點:" + textBox2.Text + ",案件類別:" + textBox3.Text + ",受傷人數:" + textBox10.Text + ",受理人員:" + comboBox1.Text;

                setIntent(cmd02, msg);
                string Server_INFO = ClientSendMsg(intent); // 發送 cmd02指令與報案訊息

                if (Server_INFO == "SocketError")
                {
                    label7.Text = "連線狀況: 未連線";
                    label7.ForeColor = Color.Red;
                    MessageBox.Show("尚未連線，上傳失敗");
                }
                else if (Server_INFO == "Error")
                {
                    MessageBox.Show("上傳失敗");
                }
                else if (Server_INFO == "SuccSendMsg")
                {
                    MessageBox.Show("上傳成功");
                }
            }
            
        }

        /// <summary>
        /// 請求派車
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            if (textBox4.Text == "")
            {
                MessageBox.Show("請點'未派遣案件'按鈕，來選擇案件來請求派車。");
            }
            else
            {
                string msg = "," + textBox4.Text;

                setIntent(cmd03, msg);
                string Server_INFO = ClientSendMsg(intent); // 發送 cmd03指令與報案編號

                if (Server_INFO == "SocketError")
                {
                    label7.Text = "連線狀況: 未連線";
                    label7.ForeColor = Color.Red;
                    MessageBox.Show("尚未連線，上傳失敗");
                }
                else if (Server_INFO == "Error")
                {
                    MessageBox.Show("上傳失敗");
                }
                else if (Server_INFO == "SuccSendMsg")
                {
                    MessageBox.Show("上傳成功");
                }
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
            textBox6.Text = "";
            textBox10.Text = "";
            Flag1 = true;
            The_Trigger = false;
        }
    }
}
