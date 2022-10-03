using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;


namespace Server
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Server介面顯示
            label4.Text = "Server: 停止監聽";
            label4.ForeColor = Color.Red;
        }

        #region 自定義變數

        /// <summary>
        /// 執行緒同步事件 對所有進行等待的執行緒進行統一管理
        /// </summary>
        public static ManualResetEvent allDone = new ManualResetEvent(false); // 初始化執行緒為阻塞狀態

        /// <summary>
        /// 監聽控制元件開啟狀態
        /// </summary>
        private bool State = true;

        /// <summary>
        /// 存放所有連進來的客戶端資訊的集合 [ClientId,Socket]
        /// </summary>
        static Dictionary<string, Socket> ClientConnectionItems = new Dictionary<string, Socket> { };

        /// <summary>
        /// 執行緒
        /// </summary>
        private Thread mythread;

        /// <summary>
        /// 服務端ip
        /// </summary>
        private IPAddress _ip;
        // _ip = IPAddress.Any; 此為本機IP

        /// <summary>
        /// 服務端port
        /// </summary>
        private int _port;

        /// <summary>
        /// 保存客戶端所有對話的Hash表
        /// </summary>
        private Hashtable _transmit_tb = new Hashtable();

        /// <summary>
        /// 接收消息
        /// </summary>
        private Thread _receviccethread = null;

        /// <summary>
        /// 重複報案鎖
        /// </summary>
        object obj_lock_cmd02 = new object();

        /// <summary>
        /// 派車鎖
        /// </summary>
        object obj_lock_cmd03 = new object();

        /// <summary>
        /// 存放封包與IP地址 Package, IpAddress的結構
        /// </summary>
        public struct TCPParameter
        {
            public string Package;
            public string IpAddress;
        }

        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox2.Text == "" || textBox3.Text == "" || textBox4.Text == "" || textBox5.Text == "" || textBox6.Text == "" || textBox7.Text == "")
            {
                MessageBox.Show("參數尚未設置完成，請重新檢查!");
            }
            else
            {
                // 設置Socket參數
                _ip = IPAddress.Parse(textBox2.Text);
                _port = Convert.ToInt32(textBox3.Text);

                // 將MySql參數傳至MysqlConnect類別
                MysqlConnect.InitializeDB(textBox4.Text, textBox6.Text, textBox7.Text, textBox5.Text);

                // 介面處理
                label4.Text = "Server: 開啟監聽.....";
                label4.ForeColor = Color.Green;
                button1.Enabled = false;

                // 將監聽設為主執行緒 結束後代表整個應用程式跟著結束
                mythread = new Thread(Listen);
                mythread.Start();
            }


        }

        private object threadlock = new object();

        /// <summary>
        /// 主執行緒，不斷監聽客戶連線請求
        /// </summary>
        private void Listen()
        {
            try
            {
                // 定義套接字來監聽客戶端連線請求 Socket(通道的位址模式,通道的服務模式,此通訊端點使用的通訊協定)
                Socket Socket_Watch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Socket_Watch.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // 初始化終結點(將IP與埠組合到網路節點point上)
                IPEndPoint ipe = new IPEndPoint(_ip, _port);

                // 將Socket連結到通訊端點上 並給予一個通訊埠口號碼(IP + Port)
                Socket_Watch.Bind(ipe);

                // 設定Socket為聆聽狀態
                Socket_Watch.Listen(100);

                // 使用Reset讓執行緒呼叫到WaitOne又會被阻塞(若沒有手動Reset 他會直接跳過WaitOne)
                allDone.Reset();

                // Socket等待遠端連接訊號到達並接受連線 一有新連線則呼叫回撥函式onCall
                Socket_Watch.BeginAccept(new AsyncCallback(onCall), Socket_Watch);

                // 阻塞執行緒 直到呼叫Set方法才能繼續執行(呼叫Set方法代表有新的連線進入)
                allDone.WaitOne();
            }
            catch (Exception ex)
            {
                // do something..
            }
        }

        /// <summary>
        /// 背景執行緒，當有客戶端連線時的處理
        /// </summary>
        private void onCall(IAsyncResult ar)
        {
            // 釋放所有等待中的執行緒
            allDone.Set();

            // 初始化Socket 用來其他客戶的連接(還原傳入的原始Socket)
            Socket Server_Socket = (Socket)ar.AsyncState;

            // 在原始Socket上呼叫EndAccept方法，返回新的Socket
            Socket client = Server_Socket.EndAccept(ar);

            string ClientId = null;
            int cId = 0; //後面查詢消防局即時車數用
            try
            {
                if (Server_Socket != null)
                {
                    byte[] comes = new byte[1024];
                    EndPoint enp = client.RemoteEndPoint;

                    // 等待新的Client端連接(因為進入onCall函式為非同步作業 所以程式會直接繼續往下執行 不會等到有新的連近來才往下執行)
                    Server_Socket.BeginAccept(new AsyncCallback(onCall), Server_Socket);

                    // 開始C/S溝通 直到某方停止Socket或是結束執行緒
                    while (true)
                    {
                        int re = client.Receive(comes, comes.Length, 0);

                        // 取得封包與IP位址
                        TCPParameter parm = new TCPParameter();
                        parm.Package = Encoding.UTF8.GetString(comes, 0, re).ToString().Trim(); // 如果為空值會有錯誤 之後去修正
                        parm.IpAddress = client.RemoteEndPoint.ToString();

                        // 判斷客戶端意圖 (以後改版將額外新增一個類別 來處理這些事情)
                        // 1. cmd01:接線員或消防局連線 => 存放ID、重複ID判斷 / 需要傳入的屬性有(TCPParameter parm, Socket clent)
                        // 2. cmd02:接線員傳送報案訊息 => 重複報案判斷、 / 需要傳入的屬性有(TCPParameter parm, Socket clent)
                        // 3. cmd03:接線員傳送派車請求 => 判斷有車可派、計算派遣分局
                        // 4. cmd04:消防局傳送已派車 => 更新資料庫
                        // 5. cmd05:消防局傳送已歸隊 => 更新資料庫
                        // 6. cmd06:Server傳送即時車數和案件執行狀況給各分局
                        // 7. cmd07:消防局回報送修消防車、救護車車數 => 更新資料庫
                        // 8. cmd08:接線員請求查看未處理案件 => 取到該RobotID的所有未處理的報案資訊後回傳過去

                        try
                        {
                            if (parm.Package.Substring(0, 5) == "cmd01") // 做存放連線ID動作
                            {
                                // 取出ClientID
                                ClientId = parm.Package.Substring(6, parm.Package.Length - 6);
                                // 判斷ClientConnectionItems 是否有重複連線ID 做處理
                                bool contains = ClientConnectionItems.ContainsKey(ClientId);
                                if (contains == true) // 有重複ID
                                {
                                    showINFO("eor03", ClientId);
                                    client.Send(Encoding.ASCII.GetBytes("eor01")); // 回撥給Client告知重複ID!
                                    break;
                                }
                                else
                                {
                                    ClientConnectionItems.Add(ClientId, client); // 將連線的ID存放起來
                                    if (ClientId.Length != 2)//確認連線為接線員介面
                                    {
                                        showINFO("cmd01", ClientId);
                                        client.Send(Encoding.ASCII.GetBytes("cmd01")); // 回撥給Client連線成功
                                    }
                                    else//確認連線為消防局
                                    {
                                        string[] branches = { "苗栗", "頭屋", "公館", "銅鑼", "三義", "竹南", "後龍", "造橋", "頭份", "三灣", "南庄", "西湖", "通霄", "苑裡", "獅潭", "大湖", "卓蘭", "泰安", "象鼻" };//所有分局
                                        for (int i = 0; i < 19; i++)
                                        {
                                            if (branches[i] == ClientId)
                                            {
                                                cId = i + 1; //分局編號用來查該分局SQL車數用
                                            }
                                        }
                                        showINFO("cmd01", ClientId);
                                        MysqlConnect mc = new MysqlConnect();
                                        int fnum = mc.getSQLfire_truck(cId);
                                        int anum = mc.getSQLambulance(cId);

                                        //回傳即時車數
                                        string str = ",消防車數:" + fnum.ToString() + ",救護車數:" + anum.ToString();

                                        if (mc.getStatus(ClientId) != ",")//檢查狀態表是否有資料
                                        {
                                            //回傳狀態表
                                            str += mc.getStatus(ClientId);
                                        }
                                        client.Send(Encoding.UTF8.GetBytes("cmd06" + str)); //,消防車數:8,救護車數:5,案件時間:2021-03-19 23:35:48,案件地點:公館鄉五谷76-1號,案件類別:火災,報案編號:35,消防車數:1,救護車數:0,案件狀態:已歸隊!
                                        showINFO("cmd06", ClientId);
                                    }
                                }
                            }
                            else if (parm.Package.Substring(0, 5) == "cmd02") // 傳給指定的消防局
                            {
                                // 報案資訊內容: str =  "cmd02,案件對話:喂/喂/不好意思我要叫救護車/嘿是怎樣齁/這裡發生車禍/地址給我/大同里大同路18號/好派車過去齁,案件時間:2021-02-19 15:33:00,案件地點:通霄鎮南和路二段463巷2弄7號5樓,案件類別:車禍案件,受傷人數:3"
                                // 擷取報案資訊
                                // string str = parm.Package.Substring(6, parm.Package.Length - 6);
                                string str = parm.Package;
                                RepeatReport Rr = new RepeatReport();

                                // 鎖住重複報案驗證的鎖，1次只能有1個執行續操作。
                                lock (obj_lock_cmd02)
                                {
                                    // RepeatReport的重複報案驗證方法，回傳string。
                                    // 若為非重複報案，則直接在RepeatReport類別裡進行資料庫存取，包含插入新報案資訊、插入新執行狀態。
                                    string Report_Verification_Var = Rr.repeatReportVerification(str);

                                    if (Report_Verification_Var == "true") // 重複報案
                                    {
                                        showINFO("eor02", ClientId);
                                        client.Send(Encoding.ASCII.GetBytes("eor02"));
                                    }
                                    else if (Report_Verification_Var == "false") // 沒有重複報案資訊
                                    {
                                        showINFO("cmd02", Rr.Last_Inserted_Id.ToString());
                                        client.Send(Encoding.ASCII.GetBytes("cmd02 " + Rr.Last_Inserted_Id));
                                    }
                                }
                            }
                            else if (parm.Package.Substring(0, 5) == "cmd03")
                            {
                                string str = parm.Package;
                                string[] str2 = str.Split(',');
                                int rid = Convert.ToInt32(str2[1]);

                                RepeatReport Rr = new RepeatReport();
                                MysqlConnect mc = new MysqlConnect();

                                lock (obj_lock_cmd03)
                                {
                                    string[] ReportInformation = mc.getReportInformation(rid).Split(','); // 這邊用查rid報案編號資料表來得知報案資料
                                    string[] AllBranchCarNum = mc.getAllBranchCarNum().Split(',');
                                    int Fsum = Convert.ToInt32(AllBranchCarNum[0]); // 苗栗所有分局的消防車總數Fsum
                                    int Asum = Convert.ToInt32(AllBranchCarNum[1]); // 苗栗所有分局的救護車總數Asum
                                    string[] NeedCarNum = mc.getNeedCarNum(rid).Split(','); // 用rid報案編號來查詢剩餘需要車數
                                    int eid = Convert.ToInt32(NeedCarNum[0]); // 該案件在狀態為"未分配"的執行編號
                                    int needFnum = Convert.ToInt32(NeedCarNum[1]); // 該案件目前所需消防車數
                                    int needAnum = Convert.ToInt32(NeedCarNum[2]); // 該案件目前所需救護車數
                                    VehicleDispatching vd = new VehicleDispatching();
                                    if (needFnum == 0 && needAnum == 0)
                                    {
                                        //MessageBox.Show("此案件已派車完畢!"); //如果案件所需車數已派完
                                        client.Send(Encoding.ASCII.GetBytes("cmd03"));
                                    }
                                    else
                                    {
                                        // 判斷是否還有車可以派
                                        if (needFnum != 0 && Fsum == 0) // 無消防車可派
                                        {
                                            showINFO("eor04", ClientId);
                                            client.Send(Encoding.UTF8.GetBytes("eor03,目前無消防車可派")); //未完成
                                        }
                                        if (needAnum != 0 && Asum == 0) // 無救護車可派
                                        {
                                            showINFO("eor05", ClientId);
                                            client.Send(Encoding.UTF8.GetBytes("eor03,目前無救護車可派")); //未完成
                                        }
                                        if (Fsum == 0 && Asum == 0)
                                        {
                                            // 不知為何判斷式如果跟下面那行寫一起會出錯 //if ( ( Fsum > 0 && Fsum < needFnum) || ( Asum > 0 && Asum < needAnum) )
                                        }
                                        else if (Fsum < needFnum || Asum < needAnum) // 有車但不夠
                                        {
                                            // 1. 派車(設置派車字典)
                                            if (Fsum >= needFnum) // 消防車夠，現有消防車 >= 需要的消防車
                                            {
                                                vd.setVehicleDispatchingTask(needFnum, Asum, ReportInformation[1], ReportInformation[2]);
                                            }
                                            else if (Asum >= needAnum) // 救護車夠，現有救護車 >= 需要的救護車
                                            {
                                                vd.setVehicleDispatchingTask(Fsum, needAnum, ReportInformation[1], ReportInformation[2]);
                                            }
                                            else //全派
                                            {
                                                vd.setVehicleDispatchingTask(Fsum, Asum, ReportInformation[1], ReportInformation[2]);
                                            }

                                            //取得所有需要的消防局ID
                                            string[] Branch = vd.getAllBranch().Split('/');
                                            Branch = Branch.Where(s => !string.IsNullOrEmpty(s)).ToArray();
                                            for (int i = 0; i < Branch.Length; i++)
                                            {
                                                string Newstr = mc.getReportInformation(rid);
                                                string cid = Branch[i];
                                                int fnum = vd.getBranchCarNum(cid, "fire_truck");
                                                int anum = vd.getBranchCarNum(cid, "ambulance");
                                                mc.insertStatus(cid + "分局", fnum, anum, rid, "待值勤"); // 2. 新增案件狀況表
                                                needFnum -= fnum; // 該案件需要的車數扣除已分配車數
                                                needAnum -= anum; // 該案件需要的車數扣除已分配車數
                                                Newstr += ",消防車數:" + fnum.ToString() + ",救護車數:" + anum.ToString() + ",報案編號:" + rid.ToString();  //str加上消防車數、救護車數、報案編號
                                                bool containCid = ClientConnectionItems.ContainsKey(cid);// 判斷傳送的目的是否有連線
                                                if (containCid == true) // 目標ID有連線 寄過去 請該分局派車
                                                {
                                                    ClientConnectionItems[cid].Send(Encoding.UTF8.GetBytes("cmd03," + Newstr));
                                                    showINFO("cmd03", cid);
                                                }
                                                else
                                                {
                                                    // 消防局上線後，從出勤狀況表點選'待值勤'的資料，自行做派車。
                                                    showINFO("eor01", cid);
                                                }
                                            }
                                            // 3. 將還沒被分派的車數存回該執行狀況，修改eid其消防車數、救護車數。
                                            mc.updateNeedCarNum(needFnum, needAnum, eid);

                                            if ((needFnum == 0) && (needAnum == 0))
                                            {
                                                client.Send(Encoding.ASCII.GetBytes("cmd03"));
                                            }
                                            //看要不要通知接線員介面該案件剩多少車還沒派
                                            if (needFnum != 0)
                                            {
                                                client.Send(Encoding.ASCII.GetBytes("cmd04 " + needFnum));
                                                //MessageBox.Show("此案件尚有" + needFnum + "台消防車未派遣");
                                            }
                                            if (needAnum != 0)
                                            {
                                                client.Send(Encoding.ASCII.GetBytes("cmd05 " + needAnum));
                                                //MessageBox.Show("此案件尚有" + needAnum + "台救護車未派遣");
                                            }
                                        }
                                        else // 有車且足夠
                                        {
                                            // 派車(設置派車字典)，派出需要的車數
                                            vd.setVehicleDispatchingTask(needFnum, needAnum, ReportInformation[1], ReportInformation[2]); //輸入該次需派遣的消防車數、救護車數、案件資訊

                                            //取得所有需要的消防局ID
                                            string[] Branch = vd.getAllBranch().Split('/');
                                            Branch = Branch.Where(s => !string.IsNullOrEmpty(s)).ToArray();
                                            for (int i = 0; i < Branch.Length; i++)
                                            {
                                                string Newstr = mc.getReportInformation(rid);
                                                string cid = Branch[i];
                                                int fnum = vd.getBranchCarNum(cid, "fire_truck");
                                                int anum = vd.getBranchCarNum(cid, "ambulance");
                                                mc.insertStatus(cid + "分局", fnum, anum, rid, "待值勤"); // 2. 新增案件狀況表
                                                needFnum -= fnum; // 該案件需要的車數扣除已分配車數
                                                needAnum -= anum; // 該案件需要的車數扣除已分配車數
                                                Newstr += ",消防車數:" + fnum.ToString() + ",救護車數:" + anum.ToString() + ",報案編號:" + rid.ToString();  //str加上消防車數、救護車數、報案編號
                                                bool containCid = ClientConnectionItems.ContainsKey(cid);// 判斷傳送的目的是否有連線
                                                if (containCid == true) // 目標ID有連線 寄過去 請該分局派車
                                                {
                                                    ClientConnectionItems[cid].Send(Encoding.UTF8.GetBytes("cmd03," + Newstr));
                                                    showINFO("cmd03", cid);
                                                }
                                                else
                                                {
                                                    // 消防局上線後，從出勤狀況表點選'待值勤'的資料，自行做派車。
                                                    showINFO("eor01", cid);
                                                }
                                            }
                                            // 3. 將還沒被分派的車數存回該執行狀況，修改eid其消防車數、救護車數。
                                            mc.updateNeedCarNum(needFnum, needAnum, eid);
                                            // 4. 刪除該eid列
                                            mc.deleteExecuteRow(eid);

                                            if ((needFnum == 0) && (needAnum == 0))
                                            {
                                                client.Send(Encoding.ASCII.GetBytes("cmd03"));
                                            }
                                            if (needAnum != 0)
                                            {
                                                client.Send(Encoding.UTF8.GetBytes("cmd04 " + needAnum));
                                            }
                                            if (needFnum != 0)
                                            {
                                                client.Send(Encoding.UTF8.GetBytes("cmd05 " + needFnum));
                                            }
                                            
                                        }
                                    }
                                }


                            }
                            else if (parm.Package.Substring(0, 5) == "cmd04")
                            {
                                // 消防局按下派車按鈕
                                // 1. 更新fire_branch資料表，將該分局消防車或救護車數量做刪減
                                // 2. 回傳即時車數、更新案件執行狀況表

                                string str = parm.Package;// str= cmd04,分局名稱:苗栗,消防車數:1,救護車數:2,報案編號:1
                                string[] splitStr = str.Split(',');// [0]=cmd04 [1]=分局名稱:苗栗 [2]=消防車數:1 [3]=救護車數:2 [4]=報案編號
                                string branch = splitStr[1].Substring(5, splitStr[1].Length - 5);// 分局名稱
                                int fnum = Convert.ToInt32(splitStr[2].Substring(5, splitStr[2].Length - 5));// 消防車數
                                int anum = Convert.ToInt32(splitStr[3].Substring(5, splitStr[3].Length - 5));// 救護車數
                                string rid = splitStr[4].Substring(5, splitStr[4].Length - 5);// 報案編號

                                MysqlConnect mc = new MysqlConnect();
                                mc.updateFire_truck_Num(branch, fnum, '-');//輸入分局編號,派出車數更新資料庫車數
                                mc.updateAmbulance_Num(branch, anum, '-');//輸入分局編號,派出車數更新資料庫車數

                                // 將案件的執行狀態更改為值勤中
                                mc.updateStatus(rid, branch, "值勤中");

                                showINFO("cmd04", ClientId);

                                //回傳即時車數、狀態表
                                bool containCid = ClientConnectionItems.ContainsKey(ClientId);
                                if (containCid == true) // 目標ID有連線 寄過去
                                {
                                    int fnum2 = mc.getSQLfire_truck(cId);
                                    int anum2 = mc.getSQLambulance(cId);

                                    //回傳即時車數
                                    string str2 = ",消防車數:" + fnum2.ToString() + ",救護車數:" + anum2.ToString();

                                    if (mc.getStatus(ClientId) != ",")//檢查狀態表是否有資料
                                    {
                                        //回傳狀態表
                                        str2 += mc.getStatus(ClientId);
                                    }

                                    client.Send(Encoding.UTF8.GetBytes("cmd06" + str2)); //,消防車數:8,救護車數:5,案件時間:2021-03-19 23:35:48,案件地點:公館鄉五谷76-1號,案件類別:火災,報案編號:35,消防車數:1,救護車數:0,案件狀態:已歸隊!
                                    //showINFO("cmd06", ClientId);
                                }

                            }
                            else if (parm.Package.Substring(0, 5) == "cmd05")
                            {
                                // 消防局按下已歸隊按鈕
                                // 1. 更新fire_branch資料表，將該分局消防車或救護車數量加回
                                // 2. 回傳即時車數、案件執行狀況表

                                string str = parm.Package;// str= cmd04,分局名稱:苗栗,報案編號:10
                                string[] splitStr = str.Split(',');// [0]=cmd04 [1]=分局名稱:苗栗 [2]=報案編號:10
                                string branch = splitStr[1].Substring(5, splitStr[1].Length - 5);// 分局名稱
                                string rid = splitStr[2].Substring(5, splitStr[2].Length - 5);// 報案編號

                                MysqlConnect mc = new MysqlConnect();

                                string[] CarNum = mc.getDispatchCarNum(ClientId, rid).Split(',');//用分局名稱跟報案編號去execute_status查派出車數
                                int fnum = Convert.ToInt32(CarNum[0]);//消防車數
                                int anum = Convert.ToInt32(CarNum[1]);//救護車數

                                mc.updateFire_truck_Num(branch, fnum, '+');//輸入分局名稱,歸隊車數 更新資料庫車數
                                mc.updateAmbulance_Num(branch, anum, '+');//輸入分局名稱,歸隊車數 更新資料庫車數

                                mc.updateStatus(rid, branch, "已歸隊");//更新案件狀況表
                                showINFO("cmd05", "報案編號" + rid + " " + ClientId);//show已歸隊

                                //回傳即時車數、狀態表
                                bool containCid = ClientConnectionItems.ContainsKey(ClientId);
                                if (containCid == true) // 目標ID有連線 寄過去
                                {
                                    int fnum2 = mc.getSQLfire_truck(cId);
                                    int anum2 = mc.getSQLambulance(cId);

                                    //回傳即時車數
                                    string str2 = ",消防車數:" + fnum2.ToString() + ",救護車數:" + anum2.ToString();

                                    if (mc.getStatus(ClientId) != ",")//檢查狀態表是否有資料
                                    {
                                        //回傳狀態表
                                        str2 += mc.getStatus(ClientId);
                                    }

                                    client.Send(Encoding.UTF8.GetBytes("cmd06" + str2)); //,消防車數:8,救護車數:5,案件時間:2021-03-19 23:35:48,案件地點:公館鄉五谷76-1號,案件類別:火災,報案編號:35,消防車數:1,救護車數:0,案件狀態:已歸隊!
                                    //showINFO("cmd05", ClientId);
                                }

                            }
                            else if (parm.Package.Substring(0, 5) == "cmd07")
                            {
                                // 消防局回報送修車數
                                // 1. 更新fire_branch資料表，將該分局消防車或救護車數量加回
                                // 2. 回傳即時車數、案件執行狀況表

                                string str = parm.Package; // cmd06 ,分局名稱:苗栗,送修消防車數:0,送修救護車數:2,送修歸隊消防車數:0,送修歸隊救護車數:4
                                string[] splitStr = str.Split(','); // [0]=cmd06 [1]=分局名稱:苗栗 [2]=送修消防車數:1 [3]=送修救護車數:2 [4]=送修歸隊消防車數:3 [5]=送修歸隊救護車數:0

                                string branch = splitStr[1].Substring(5, splitStr[1].Length - 5);// 分局名稱
                                int sfnum = Convert.ToInt32(splitStr[2].Substring(7, splitStr[2].Length - 7)); // 維修消防車數
                                int sanum = Convert.ToInt32(splitStr[3].Substring(7, splitStr[3].Length - 7)); // 維修救護車數
                                int bfnum = Convert.ToInt32(splitStr[4].Substring(9, splitStr[4].Length - 9)); // 維修歸隊消防車數
                                int banum = Convert.ToInt32(splitStr[5].Substring(9, splitStr[5].Length - 9)); // 維修歸隊救護車數

                                MysqlConnect mc = new MysqlConnect();
                                mc.updateFire_truck_Num(branch, sfnum, '-');//輸入分局編號,維修車數 更新資料庫車數
                                mc.updateAmbulance_Num(branch, sanum, '-');//輸入分局編號,維修車數 更新資料庫車數
                                mc.updateFire_truck_Num(branch, bfnum, '+');//輸入分局名稱,維修回歸車數 更新資料庫車數
                                mc.updateAmbulance_Num(branch, banum, '+');//輸入分局名稱,維修回歸車數 更新資料庫車數

                                showINFO("cmd07", ClientId);//show維修情況

                                //回傳即時車數、狀態表
                                bool containCid = ClientConnectionItems.ContainsKey(ClientId);
                                if (containCid == true) // 目標ID有連線 寄過去
                                {
                                    int fnum2 = mc.getSQLfire_truck(cId);
                                    int anum2 = mc.getSQLambulance(cId);

                                    //回傳即時車數
                                    string str2 = ",消防車數:" + fnum2.ToString() + ",救護車數:" + anum2.ToString();

                                    if (mc.getStatus(ClientId) != ",")//檢查狀態表是否有資料
                                    {
                                        //回傳狀態表
                                        str2 += mc.getStatus(ClientId);
                                    }

                                    client.Send(Encoding.UTF8.GetBytes("cmd06" + str2)); //,消防車數:8,救護車數:5,案件時間:2021-03-19 23:35:48,案件地點:公館鄉五谷76-1號,案件類別:火災,報案編號:35,消防車數:1,救護車數:0,案件狀態:已歸隊!
                                    //showINFO("cmd06", ClientId);
                                }

                            }
                            else if (parm.Package.Substring(0, 5) == "cmd08") // 接線員請求資料庫回傳未處理的案件。
                            {
                                string str = parm.Package; // (cmd08,RobotId)
                                string[] splitStr = str.Split(' '); // [0]=cmd08,[1]=RobotId
                                string allData = null;

                                MysqlConnect mc = new MysqlConnect();
                                // 取得未處理案件的Rid
                                List<int> statusDataRid = new List<int>();
                                statusDataRid = mc.get_UnassignedStatusData_Rid();

                                for (int i = 0; i < statusDataRid.Count; i++)
                                {
                                    allData += mc.getReportDataForOperator(splitStr[1], statusDataRid[i]);
                                }
                                client.Send(Encoding.UTF8.GetBytes("cmd08" + allData));
                                showINFO("cmd08", splitStr[1]);
                            }
                        }
                        catch (ArgumentOutOfRangeException ex) // 當Client連線後馬上按離線的錯誤
                        {
                            // 移除遠端斷線的ID
                            ClientConnectionItems.Remove(ClientId);
                            showINFO("遠端主機已強制關閉一個現存的連線。", ClientId);
                            break;
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                // 移除遠端斷線的ID
                ClientConnectionItems.Remove(ClientId);
                showINFO(ex.Message, ClientId);
            }
        }

        /// <summary>
        /// 顯示與Client溝通上的訊息
        /// </summary>
        private void showINFO(string msg, string str)
        {
            try
            {
                // 判斷呼叫端是否在不同執行緒上，True時進行委派
                if (this.InvokeRequired)
                {
                    this.Invoke(new MethodInvoker(() => showINFO(msg, str)));
                }
                else // False表示在同一執行緒上，可以正常呼叫到此物件
                {
                    // 使用規則: msg前5個字串必須包含以下判斷式字串的其中一項。
                    // 需要擴充判斷式則自行增加。
                    string s = msg.Substring(0, 5);
                    string time = DateTime.Now.ToString("HH:mm:ss");

                    if (s == "cmd01") // 接線員或消防局連線
                    {
                        textBox1.Text += "\r\n" + "[" + time + " INFO]: " + str + " 連線成功!";
                    }
                    else if (s == "cmd02") // 接線員傳送報案訊息
                    {
                        textBox1.Text += "\r\n" + "[" + time + " INFO]: " + "成功新增一筆報案資訊，編號為: " + str;
                    }
                    else if (s == "cmd03") // 接線員傳送派車請求
                    {
                        textBox1.Text += "\r\n" + "[" + time + " INFO]: " + "報案資訊成功傳送到" + str + "分局!";
                    }
                    else if (s == "cmd04") // 消防局傳送已派車
                    {
                        textBox1.Text += "\r\n" + "[" + time + " INFO]: " + str + " 分局已派車";
                    }
                    else if (s == "cmd05") // 消防局傳送已歸隊
                    {
                        textBox1.Text += "\r\n" + "[" + time + " INFO]: " + str + " 分局已歸隊!";
                    }
                    else if (s == "cmd06") //通知會變很多、很雜
                    {
                        textBox1.Text += "\r\n" + "[" + time + " INFO]: 成功傳送即時車數和案件狀態";//到  " + str + "分局!";
                    }
                    else if (s == "cmd07") // 消防局回報維修狀況
                    {
                        textBox1.Text += "\r\n" + "[" + time + " INFO]: " + str + " 回報維修狀況";
                    }
                    else if (s == "cmd08") // 接線員請求查詢未處理的案件
                    {
                        textBox1.Text += "\r\n" + "[" + time + " INFO]: " + str + " 請求查詢未處理案件";
                    }
                    else if (s == "eor01") // 報案訊息傳送到消防局失敗
                    {
                        textBox1.Text += "\r\n" + "[" + time + " INFO]: " + str + " 分局尚未連線，故無法傳送訊息!";
                    }
                    else if (s == "eor02") // 報案訊息已重複報案
                    {
                        textBox1.Text += "\r\n" + "[" + time + " INFO]: " + str + " 上傳案件=> 此案件為重複報案，故不新增此案件!";
                    }
                    else if (s == "eor03") // 消防局連線有重複ID異常
                    {
                        textBox1.Text += "\r\n" + "[" + time + " INFO]: " + str + " 已存在，故無法連線!";
                    }
                    else if (s == "eor04") // 無消防車可派
                    {
                        textBox1.Text += "\r\n" + "[" + time + " INFO]: " + str + " 請求派車=>  目前無消防車可派!";
                    }
                    else if (s == "eor05") // 無救護車可派
                    {
                        textBox1.Text += "\r\n" + "[" + time + " INFO]: " + str + " 請求派車=>  目前無救護車可派!";
                    }
                    else // 出現斷線資訊
                    {
                        textBox1.Text += "\r\n" + "[" + time + " INFO]: " + str + " " + msg;
                    }
                }
            }
            catch
            {
                //MessageBox.Show("執行緒被關閉");
            }
        }

        #region 關閉退出

        // 關閉視窗&執行緒
        private void button2_Click(object sender, EventArgs e)
        {
            // 暫時先用結束程式來關閉執行緒 以後版本在做直接關閉整個執行續 並且釋放資源
            System.Environment.Exit(0);
        }
        #endregion
    }
}
