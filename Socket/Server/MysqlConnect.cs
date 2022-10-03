using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient; // 需設定參考組件MySql.Data

namespace Server
{
    class MysqlConnect
    {
        #region 定義變數

        /// <summary>
        /// MySQL的Connection
        /// </summary>
        private static MySqlConnection DbConn;

        // Report資料表 之後額外對此做新類別
        public int Id { get; private set; }
        public string Time { get; private set; }
        public string Region { get; private set; }
        public string Case { get; private set; }
        public int Injured { get; private set; }
        public string Dialogue { get; private set; }
        public MysqlConnect() // 派車類別會初始化sqlConnect
        {
        }
        public MysqlConnect(int id, string time, string region, string cases, int injured, string dialogue)
        {
            Id = id;
            Time = time;
            Region = region;
            Case = cases;
            Injured = injured;
            Dialogue = dialogue;
        }

        #endregion

        /// <summary>
        /// 與MySQL連結
        /// </summary>
        public static void InitializeDB(string server, string userId, string password, string database)
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
            builder.Server = server;
            builder.UserID = userId;
            builder.Password = password;
            builder.Database = database;

            // MySQL的連線字串
            String connString = builder.ToString();

            builder = null;

            Console.WriteLine(connString);

            DbConn = new MySqlConnection(connString);

        }

        /// <summary>
        /// 取出report表的指定的資料(根據指定的時間 案件)
        /// </summary>
        public static List<MysqlConnect> getReportData(string query)
        {
            List<MysqlConnect> mysqlConnects = new List<MysqlConnect>();

            //string query = "SELECT * FROM report";

            MySqlCommand cmd = new MySqlCommand(query, DbConn);

            DbConn.Open();

            MySqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                int id = (int)reader["id"];
                string time = reader["time"].ToString();
                string region = reader["region"].ToString();
                string cases = reader["cases"].ToString();
                int injured = (int)reader["injured"];
                string dialogue = reader["dialogue"].ToString();

                MysqlConnect u = new MysqlConnect(id, time, region, cases, injured, dialogue);

                mysqlConnects.Add(u);
            }

            reader.Close();

            DbConn.Close();

            return mysqlConnects;
        }

        /// 指令 INSERT INTO `reportdb`.`report` (`id`, `time`, `region`, `cases`, `injured`, `dialogue`) VALUES ('10', '2020-12-07 14:49:23', '苑裡鎮世界路一段8巷29號', '火災', '0', '沒人受傷');
        /// <summary>
        /// 插入新的報案資料
        /// </summary>
        public int insertNewReport(string robotId)
        {
            // Report資料表的id設為AUTO_INCREMENT 所以不須插入id 他將自動生成id

            string query = string.Format("INSERT INTO `reportdb`.`report` (`robotId`,`time`, `region`, `cases`, `injured`, `dialogue`) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}')", robotId, Time, Region, Case, Injured, Dialogue);

            MySqlCommand cmd = new MySqlCommand(query, DbConn);

            DbConn.Open();

            cmd.ExecuteNonQuery();
            int id = (int)cmd.LastInsertedId; // 給其他需要Rid的資料表做使用

            DbConn.Close();

            return id;
        }

        /// <summary>
        /// 查詢狀態資料表，欄位為未分配、待值勤、值勤中、的Data，回傳Rid(不會有重複Rid出現)。
        /// </summary>
        /// <returns></returns>
        public List<int> getStatusData_Rid()
        {
            List<int> statusDataRid = new List<int>();

            string query = "SELECT Rid FROM reportdb.execute_status WHERE(status = '未分配') OR (status = '待值勤') OR (status = '值勤中');";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);

            DbConn.Open();

            MySqlDataReader reader = cmd.ExecuteReader();

            int rid = 0;

            while (reader.Read())
            {
                rid = (int)reader.GetValue(0);
                if (!statusDataRid.Contains(rid)) //　過略重複存過的rid
                {
                    statusDataRid.Add(rid);
                }
            }

            reader.Close();
            DbConn.Close();

            return statusDataRid;
        }

        /// <summary>
        /// 向report資料表取出指定報案事件Data。第一參數: 所有Rid、第二參數: 指定案件類別。
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public MysqlConnect getReportData(int rid, string Report_Cases)
        {
            int id = 0;
            string time = null;
            string region = null;
            string cases = null;
            int injured = 0;
            string dialogue = null;

            //SELECT * FROM reportdb.report WHERE(id = 10) AND (cases = '車禍');
            string query = "SELECT * FROM reportdb.report WHERE(id = " + rid + ") AND (cases = '" + Report_Cases + "');";

            MySqlCommand cmd = new MySqlCommand(query, DbConn);

            DbConn.Open();

            MySqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                id = (int)reader["id"];
                time = reader["time"].ToString();
                region = reader["region"].ToString();
                cases = reader["cases"].ToString();
                injured = (int)reader["injured"];
                dialogue = reader["dialogue"].ToString();
            }
            else
            {

            }
            MysqlConnect u = new MysqlConnect(id, time, region, cases, injured, dialogue);
            reader.Close();
            DbConn.Close();
            return u;
        }

        /// <summary>
        /// 查詢狀態資料表，欄位為未分配的Data，回傳Rid(不會有重複Rid出現)。
        /// </summary>
        /// <returns></returns>
        public List<int> get_UnassignedStatusData_Rid()
        {

            List<int> statusDataRid = new List<int>();

            string query = "SELECT Rid FROM reportdb.execute_status WHERE(status = '未分配');";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);

            DbConn.Open();

            MySqlDataReader reader = cmd.ExecuteReader();

            int rid = 0;

            while (reader.Read())
            {
                rid = (int)reader.GetValue(0);
                if (!statusDataRid.Contains(rid)) //　過略重複存過的rid
                {
                    statusDataRid.Add(rid);
                }
            }

            reader.Close();
            DbConn.Close();

            return statusDataRid;
        }

        /// <summary>
        /// 接線員請求查詢'未處理'的所有案件(用字串回傳，每個案件用,分開)。
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="Report_Cases"></param>
        /// <returns></returns>
        public string getReportDataForOperator(string robotId, int Rid) // 此Rid為未分配的Rid
        {
            int id = 0;
            string time = null;
            string region = null;
            string cases = null;
            int injured = 0;
            string dialogue = null;

            //SELECT * FROM reportdb.report WHERE(id = 10) AND (cases = '車禍');
            string query = "SELECT * FROM reportdb.report WHERE(id = " + Rid + ") AND (robotId = '" + robotId + "');";

            // 回傳的字串
            string str = null;

            MySqlCommand cmd = new MySqlCommand(query, DbConn);

            DbConn.Open();

            MySqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                id = (int)reader["id"];
                time = reader["time"].ToString();
                region = reader["region"].ToString();
                cases = reader["cases"].ToString();
                injured = (int)reader["injured"];
                dialogue = reader["dialogue"].ToString();
                str += id + "/" + time + "/" + region + "/" + cases + "/" + injured + "/" + dialogue + ",";
            }
            reader.Close();
            DbConn.Close();
            return str;
        }

        /// <summary>
        /// 讀取MySQL fire_branch裡現有消防車數量，用於即時更新車數
        /// </summary>
        public int getSQLfire_truck(int id)
        {
            int fire_truck = 0;
            string query = "SELECT fire_truck FROM reportdb.fire_branch WHERE(id = '" + id + "') ;";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);
            DbConn.Open();
            MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                fire_truck = (int)reader.GetValue(0);
            }
            reader.Close();
            DbConn.Close();
            return fire_truck;
        }

        /// <summary>
        /// 讀取MySQL fire_branch裡現有救護車數量，用於即時更新車數
        /// </summary>
        public int getSQLambulance(int id)
        {
            int ambulance = 0;
            string query = "SELECT ambulance FROM reportdb.fire_branch WHERE(id = '" + id + "') ;";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);
            DbConn.Open();
            MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ambulance = (int)reader.GetValue(0);
            }
            reader.Close();
            DbConn.Close();
            return ambulance;
        }


        /// <summary>
        /// 讀取MySQL fire_branch裡消防車數量減去待值勤車數，用於分配車數時，因為分配時必須先減去待值勤的，不然會沒算到
        /// </summary>
        public int getSQLfire_truck2(int id)
        {
            int fire_truck = 0;
            string query = "SELECT fire_truck FROM reportdb.fire_branch WHERE(id = '" + id + "') ;";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);
            DbConn.Open();
            MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                fire_truck = (int)reader.GetValue(0);
            }
            reader.Close();
            DbConn.Close();
            string[] WaitDispatchedfnum = getBranchWaitDispatchedCarNum(id).Split(',');
            fire_truck -= Convert.ToInt32(WaitDispatchedfnum[0]); // 減去待值勤車數
            return fire_truck;
        }

        /// <summary>
        /// 讀取MySQL fire_branch裡救護車數量減去待值勤車數，用於分配車數時，因為分配時必須先減去待值勤的，不然會沒算到
        /// </summary>
        public int getSQLambulance2(int id)
        {
            int ambulance = 0;
            string query = "SELECT ambulance FROM reportdb.fire_branch WHERE(id = '" + id + "') ;";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);
            DbConn.Open();
            MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ambulance = (int)reader.GetValue(0);
            }
            reader.Close();
            DbConn.Close();
            string[] WaitDispatchedfnum = getBranchWaitDispatchedCarNum(id).Split(',');
            ambulance -= Convert.ToInt32(WaitDispatchedfnum[1]); // 減去待值勤車數
            return ambulance;
        }

        /// 指令 UPDATE `reportdb`.`fire_branch` SET `fire_truck` = '2' WHERE(`id` = '1');
        /// <summary>
        /// 更新消防車數
        /// </summary>
        public void updateFire_truck_Num(string branch, int fnum, char c)
        {
            //找分局編號(id)
            string[] branches = { "苗栗", "頭屋", "公館", "銅鑼", "三義", "竹南", "後龍", "造橋", "頭份", "三灣", "南庄", "西湖", "通霄", "苑裡", "獅潭", "大湖", "卓蘭", "泰安", "象鼻" };//所有分局
            int branchid = 0;
            for (int i = 0; i < 19; i++)
            {
                if (branch == branches[i])
                {
                    branchid = i + 1;
                }
            }
            int fsqlnum = getSQLfire_truck(branchid);//先取得目前車數

            if (c == '-')
            {
                fsqlnum -= fnum;//減去派出車數
            }
            else if (c == '+')
            {
                fsqlnum += fnum;//加回派出車數
            }

            string query = "SET SQL_SAFE_UPDATES = 0;UPDATE `reportdb`.`fire_branch` SET `fire_truck` = '" + fsqlnum + "' WHERE(`name` = '" + branch + "分局');SET SQL_SAFE_UPDATES = 1;";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);
            DbConn.Open();
            cmd.ExecuteNonQuery();
            DbConn.Close();
        }

        /// 指令 UPDATE `reportdb`.`fire_branch` SET `ambulance` = '2' WHERE(`id` = '1');
        /// <summary>
        /// 更新救護車數
        /// </summary>
        public void updateAmbulance_Num(string branch, int anum, char c)
        {
            //找分局編號(id)
            string[] branches = { "苗栗", "頭屋", "公館", "銅鑼", "三義", "竹南", "後龍", "造橋", "頭份", "三灣", "南庄", "西湖", "通霄", "苑裡", "獅潭", "大湖", "卓蘭", "泰安", "象鼻" };//所有分局
            int branchid = 0;
            for (int i = 0; i < 19; i++)
            {
                if (branch == branches[i])
                {
                    branchid = i + 1;
                }
            }
            int asqlnum = getSQLambulance(branchid);//先取得目前車數

            if (c == '-')
            {
                asqlnum -= anum;//減去派出車數
            }
            else if (c == '+')
            {
                asqlnum += anum;//加回派出車數
            }

            string query = "SET SQL_SAFE_UPDATES = 0;UPDATE `reportdb`.`fire_branch` SET `ambulance` = '" + asqlnum + "' WHERE(`name` = '" + branch + "分局');SET SQL_SAFE_UPDATES = 1;";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);
            DbConn.Open();
            cmd.ExecuteNonQuery();
            DbConn.Close();
        }

        /// 指令 INSERT INTO `reportdb`.`execute_status` (`id`, `branch`, `fire_truck`, `ambulance`, `status`, `Rid`) VALUES  ('1', '苗栗分局', '0', '1', '值勤中', '1'); 
        /// <summary>
        /// 新增出勤狀態
        /// </summary>
        public void insertStatus(string branch, int fnum, int anum, int rid, string status) // 記得傳入branch+"分局"
        {
            string query = string.Format("INSERT INTO `reportdb`.`execute_status` (`branch`, `fire_truck`, `ambulance`, `status`, `rId`) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", branch, fnum, anum, status, rid);
            MySqlCommand cmd = new MySqlCommand(query, DbConn);
            DbConn.Open();
            cmd.ExecuteNonQuery();
            DbConn.Close();
        }

        /// 指令 SET SQL_SAFE_UPDATES=0;
        /// 指令 UPDATE `reportdb`.`execute_status` SET `status` = '已歸隊' WHERE(rid= 10 and branch = '苗栗分局');
        /// 指令 SET SQL_SAFE_UPDATES = 1;
        /// <summary>
        /// 更新出勤狀態
        /// </summary>
        public void updateStatus(string rid, string branch, string status)
        {
            /*  更新出勤狀態
            mysql有個叫SQL_SAFE_UPDATES的變數，沒有 KEY column 的 WHERE 條件會拒絕執行。  //在這邊因為WHERE條件沒有eid
            所以要先關閉功能，UPDATE完再打開
            SET SQL_SAFE_UPDATES=0;
            UPDATE XXXXXX SET XXXXXX WHERE XXXXXXX;
            SET SQL_SAFE_UPDATES=1;
             */
            string query = "SET SQL_SAFE_UPDATES = 0; UPDATE `reportdb`.`execute_status` SET `status` = '" + status + "' WHERE(rid = " + rid + "  and branch = '" + branch + "分局" + "'); SET SQL_SAFE_UPDATES = 1; ";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);
            DbConn.Open();
            cmd.ExecuteNonQuery();
            DbConn.Close();

        }

        /// 指令 
        /// <summary>
        /// 取得該分局在狀況表的資料 ,輸入分局名稱會回傳該分局要處理案件的  [0] = 案件時間: [1] =案件地點: [2] = 案件類別: [3] = 受傷人數:  [4] = 報案編號: [5] = 消防車數: [6] = 救護車數: [7] = 案件狀態:   
        /// </summary>
        public string getStatus(string clientId)
        {
            string str = "";

            if (getBranchAllrId(clientId) != "")
            {
                string[] rid_status = getBranchAllrId(clientId).Split(',');
                for (int i = 0; i < rid_status.Length; i++)
                {
                    if (i % 4 == 0)
                    {
                        int rid = Convert.ToInt32(rid_status[i].Substring(5, rid_status[i].Length - 5));//8
                        string fnum = rid_status[i + 1].Substring(5, rid_status[i + 1].Length - 5);//1
                        string anum = rid_status[i + 2].Substring(5, rid_status[i + 2].Length - 5);//0
                        string status = rid_status[i + 3].Substring(5, rid_status[i + 3].Length - 5);//值勤中
                        string query = "SELECT * FROM reportdb.report WHERE id=" + rid.ToString();//report中取出該案件編號的資訊
                        MySqlCommand cmd = new MySqlCommand(query, DbConn);

                        DbConn.Open();
                        MySqlDataReader reader = cmd.ExecuteReader();
                        int count = 0;
                        while (reader.Read())
                        {
                            string time = reader["time"].ToString();
                            string region = reader["region"].ToString();
                            string cases = reader["cases"].ToString();
                            string injured = reader["injured"].ToString();

                            if (count == 0) //第一筆前面不用逗號
                            {
                                str += "案件時間:" + time + ",案件地點:" + region + ",案件類別:" + cases + ",受傷人數:" + injured + ",報案編號:" + rid.ToString() + ",消防車數:" + fnum + ",救護車數:" + anum + ",案件狀態:" + status + "!";//用!分割案件
                            }
                            else
                            {
                                str += ",案件時間:" + time + ",案件地點:" + region + ",案件類別:" + cases + ",受傷人數:" + injured + ",報案編號:" + rid.ToString() + ",消防車數:" + fnum + ",救護車數:" + anum + ",案件狀態:" + status + "!";
                            }
                            count++;
                        }

                        reader.Close();
                        DbConn.Close();
                    }
                }
            }
            return "," + str;
        }

        /// 指令 
        /// <summary>
        /// 取得該分局所有執行過的報案編號和狀態
        /// </summary>
        public string getBranchAllrId(string clientId)
        {
            string rid_status = "";
            string query = "SELECT * FROM reportdb.execute_status WHERE branch='" + clientId + "分局'";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);
            DbConn.Open();
            MySqlDataReader reader = cmd.ExecuteReader();
            int count = 0;
            while (reader.Read())
            {
                if (count == 0) //第一筆前面不用逗號
                {
                    rid_status += "報案編號:" + reader["rId"].ToString() + ",消防車數:" + reader["fire_truck"].ToString() + ",救護車數:" + reader["ambulance"].ToString() + ",案件狀態:" + reader["status"].ToString(); //rid_status = 報案編號:8,消防車數:0,救護車數:1,案件狀態:值勤中,報案編號:10,案件狀態:值勤中
                }
                else
                {
                    rid_status += ",報案編號:" + reader["rId"].ToString() + ",消防車數:" + reader["fire_truck"].ToString() + ",救護車數:" + reader["ambulance"].ToString() + ",案件狀態:" + reader["status"].ToString();  //rid_status = ,報案編號:8,消防車數:0,救護車數:1,案件狀態:值勤中,報案編號:10,案件狀態:值勤中
                }
                count++;
            }
            reader.Close();
            DbConn.Close();
            return rid_status;
        }

        /// 指令 SELECT fire_truck,ambulance FROM reportdb.execute_status WHERE (branch='苗栗分局' and rId=8);
        /// <summary>
        /// 用分局名稱跟報案編號去execute_status查派出車數
        /// </summary>
        public string getDispatchCarNum(string clientId, string rid)
        {
            int fnum = 0;
            int anum = 0;

            string query = "SELECT fire_truck,ambulance FROM reportdb.execute_status WHERE (branch='" + clientId + "分局' and rId= " + rid + " )";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);

            DbConn.Open();
            MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                fnum = (int)reader.GetValue(0);
                anum = (int)reader.GetValue(1);
            }
            reader.Close();
            DbConn.Close();
            return fnum + "," + anum;
        }

        /// <summary>
        /// 查詢整個苗栗所有分局的消防車總數Fsum、救護車總數Asum減去所有待值勤車數
        /// </summary>
        public string getAllBranchCarNum() // 取得整個苗栗所有分局的消防車總數Fsum、救護車總數Asum
        {
            string Fsum = null;
            string Asum = null;
            string query = "SELECT SUM(fire_truck),SUM(ambulance) FROM reportdb.fire_branch ";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);

            DbConn.Open();
            MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Fsum = reader["SUM(fire_truck)"].ToString();
                Asum = reader["SUM(ambulance)"].ToString();
            }

            reader.Close();
            DbConn.Close();
            string[] AllWaitDispatchedCarNum = getAllWaitDispatchedCarNum().Split(',');
            int fsum = Convert.ToInt32(Fsum) - Convert.ToInt32(AllWaitDispatchedCarNum[0]);
            int asum = Convert.ToInt32(Asum) - Convert.ToInt32(AllWaitDispatchedCarNum[1]);
            return fsum + "," + asum; // 回傳消防車總數Fsum、救護車總數Asum
        }

        /// <summary>
        /// 利用rid搜尋執行狀況資料表(execute)中狀態為'未分配’取得該案件目前所需的消防車數needFnum和救護車數needAnum，判斷有現有車數夠不夠
        /// </summary>
        public string getNeedCarNum(int rid)
        {
            int needFnum = 0;
            int needAnum = 0;
            int eid = 0;
            string query = "SELECT id,fire_truck,ambulance FROM reportdb.execute_status Where  ( status = '" + "未分配" + "' and rId= " + rid + " );";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);
            DbConn.Open();
            MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                eid = (int)reader.GetValue(0);
                needFnum = (int)reader.GetValue(1);
                needAnum = (int)reader.GetValue(2);
            }

            reader.Close();
            DbConn.Close();
            return eid + "," + needFnum + "," + needAnum; // 回傳執行編號、消防車總數Fsum、救護車總數Asum
        }

        /// <summary>
        /// 將還沒被分派的車數存回該eid執行列，修改eid其消防車數、救護車數。 //UPDATE `reportdb`.`execute_status` SET `fire_truck` = '0', `ambulance` = '3' WHERE(`id` = '15')
        /// </summary>
        public void updateNeedCarNum(int needFnum, int needAnum, int eid)
        {
            string query = "SET SQL_SAFE_UPDATES = 0; UPDATE `reportdb`.`execute_status` SET `fire_truck` = '" + needFnum + "', `ambulance` = '" + needAnum + "' WHERE(`id` = '" + eid + "'); SET SQL_SAFE_UPDATES = 1; ";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);
            DbConn.Open();
            cmd.ExecuteNonQuery();
            DbConn.Close();
        }

        /// <summary>
        /// 該案件需要的車都派完了，刪除該列。 //DELETE FROM `reportdb`.`execute_status` WHERE(`id` = '15');
        /// </summary>
        public void deleteExecuteRow(int eid)
        {
            string query = "DELETE FROM `reportdb`.`execute_status` WHERE(`id` = '" + eid + "');";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);
            DbConn.Open();
            cmd.ExecuteNonQuery();
            DbConn.Close();
        }

        /// <summary>
        /// 利用rid找報案資訊(派車用) [0]=案件時間 [1]=案件地點 [2]=案件類別 [3]=受傷人數 [4]=案件對話 
        /// </summary>
        public string getReportInformation(int rid)
        {
            string query = "SELECT * FROM reportdb.report Where id = '" + rid + "';;";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);
            DbConn.Open();
            MySqlDataReader reader = cmd.ExecuteReader();
            string str = "";
            while (reader.Read())
            {
                string time = reader["time"].ToString();
                string region = reader["region"].ToString();
                string cases = reader["cases"].ToString();
                int injured = (int)reader["injured"];
                string dialogue = reader["dialogue"].ToString();
                str += "案件時間:" + time + ",案件地點:" + region + ",案件類別:" + cases + ",受傷人數:" + injured + ",案件對話:" + dialogue;
            }

            reader.Close();
            DbConn.Close();
            return str; // 回傳執行編號、消防車總數Fsum、救護車總數Asum
        }

        /// <summary>
        /// 查詢該消防局待值勤的車數，用於查詢消防局現有車數 //SELECT fire_truck,ambulance FROM reportdb.execute_status Where ( branch = '苗栗分局' and status = '待值勤') ;
        /// </summary>
        public string getBranchWaitDispatchedCarNum(int branchId)
        {
            int fnum = 0;
            int anum = 0;
            // 分局id轉名稱
            string[] branches = { "苗栗", "頭屋", "公館", "銅鑼", "三義", "竹南", "後龍", "造橋", "頭份", "三灣", "南庄", "西湖", "通霄", "苑裡", "獅潭", "大湖", "卓蘭", "泰安", "象鼻" };//所有分局
            string branch = branches[branchId - 1] + "分局";
            string query = "SELECT fire_truck,ambulance FROM reportdb.execute_status Where ( branch = '" + branch + "' and status = '待值勤') ;";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);
            DbConn.Open();
            MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetValue(0).ToString() != "")
                {
                    fnum = (int)reader.GetValue(0);
                }
                if (reader.GetValue(0).ToString() != "")
                {
                    anum = (int)reader.GetValue(1);
                }

            }

            reader.Close();
            DbConn.Close();
            return fnum + "," + anum; // 回傳執行編號、消防車總數Fsum、救護車總數Asum
        }

        /// <summary>
        /// 查詢execute表裡面所有消防局待值勤的車數 //SELECT SUM(fire_truck),SUM(ambulance) FROM reportdb.execute_status Where status ='待值勤' ; 
        /// </summary>
        public string getAllWaitDispatchedCarNum()
        {
            string Fsum = null;
            string Asum = null;
            // 分局id轉名稱
            string query = "SELECT SUM(fire_truck),SUM(ambulance) FROM reportdb.execute_status Where status ='待值勤' ; ";
            MySqlCommand cmd = new MySqlCommand(query, DbConn);

            DbConn.Open();
            MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Fsum = reader["SUM(fire_truck)"].ToString();
                Asum = reader["SUM(ambulance)"].ToString();
            }

            reader.Close();
            DbConn.Close();
            if (Fsum == "")
            {
                Fsum = "0";
            }
            if (Asum == "")
            {
                Asum = "0";
            }
            return Fsum + "," + Asum; // 回傳消防車總數Fsum、救護車總數Asum
        }
    }
}