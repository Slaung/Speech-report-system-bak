using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class RepeatReport
    {
        // v3 重複報案方法
        // 1. 報案資訊處理 將字串分割
        // 2. 先搜尋執行狀態資料表內(狀態為: 待值勤、值勤中)，並取出搜訊到的Rid。
        // 3. 將取出的Rid，一個一個去向報案事件資料表，搜尋指定案件類別的地點。
        // 4. 最後再把取出來的地點，存到MySql_Report_Table_Data，開始比較地點。

        #region 自定義變數

        /// <summary>
        /// 報案資訊 {[0]=傳送方指令,[1]=對話,[2]=時間,[3]=地點,[4]=案件,[5]=受傷人數,[6]=受理機器人ID}
        /// </summary>
        public string[] Report_Information = null;

        /// <summary>
        /// MySQL的report資料表 {[0]=id,[1]=time,[2]=region,[3]=case,[4]=injured,[5]=dialogue}
        /// </summary>
        private List<MysqlConnect> MySql_Report_Table_Data = new List<MysqlConnect>();

        /// <summary>
        /// 存取重複報案過的資料
        /// </summary>
        public List<MysqlConnect> MySql_Repeat_Report_Data = new List<MysqlConnect>();

        /// <summary>
        /// 最後一筆新增報案資訊的ID (供執行編號使用)
        /// </summary>
        public int Last_Inserted_Id = 0;

        #endregion

        /// <summary>
        /// 重複報案資訊驗證 結果回傳至Form1 {true=重複, false=不重複}
        /// </summary>
        public string repeatReportVerification(string str)
        {
            // 1. 報案資訊處理 將字串分割
            segmentText(str);

            // 2. 先搜尋執行狀態資料表內，所有待值勤、值勤中的Rid取出。
            MysqlConnect mc = new MysqlConnect();
            List<int> MySql_Status_Table_Rid = new List<int>(); // Rid存在此變數
            MySql_Status_Table_Rid = mc.getStatusData_Rid();

            // 3. 將取出的所有Rid，去向報案事件資料表，搜尋指定的案件類別，最後再把取出來的data，存到MySql_Report_Table_Data。
            for (int i = 0; i < MySql_Status_Table_Rid.Count; i++)
            {
                mc = mc.getReportData(MySql_Status_Table_Rid[i], Report_Information[4]);
                if (mc.Case != null) // 案件不為空值時，代表有搜尋到指定案件，並且為待值勤或值勤中。
                {
                    MySql_Report_Table_Data.Add(mc);
                }
            }

            // 若搜尋時間與案件的範圍內都沒任何重複資料 直接回傳False
            if (MySql_Report_Table_Data.Count <= 0)
            {
                // 新增報案資訊
                MysqlConnect u = new MysqlConnect(0, Report_Information[2], Report_Information[3], Report_Information[4], Int32.Parse(Report_Information[5]), Report_Information[1]);
                Last_Inserted_Id = u.insertNewReport(Report_Information[6]);

                // 新增此案件的執行狀態為:未分配。
                VehicleDispatching vd = new VehicleDispatching();
                int fnum = vd.setfire_truck(Report_Information[4]);
                int anum = vd.setambulance(Report_Information[5]);
                u.insertStatus("null", fnum, anum, Last_Inserted_Id, "未分配");

                return "false"; // 並沒有重複報案
            }
            else
            {
                // 針對地點去做重複篩選
                Boolean Address_Verification_Var = addressVerification();

                // 若經由地點做完重複篩選後並沒有重複，則存入sql。
                if (Address_Verification_Var == false)
                {
                    // 新增報案資訊
                    MysqlConnect u = new MysqlConnect(0, Report_Information[2], Report_Information[3], Report_Information[4], Int32.Parse(Report_Information[5]), Report_Information[1]);
                    Last_Inserted_Id = u.insertNewReport(Report_Information[6]);

                    // 新增此案件的執行狀態為:未分配。
                    VehicleDispatching vd = new VehicleDispatching();
                    int fnum = vd.setfire_truck(Report_Information[4]);
                    int anum = vd.setambulance(Report_Information[5]);
                    u.insertStatus("null", fnum, anum, Last_Inserted_Id, "未分配");

                    return "false";
                }
                return "true";
            }
        }

        /// <summary>
        /// 報案資訊處理 字串分割
        /// </summary>
        private void segmentText(string str)
        {
            Report_Information = str.Split(',');
            for (int i = 1; i <= 6; i++)
            {
                Report_Information[i] = Report_Information[i].Substring(5, Report_Information[i].Length - 5);
            }
        }

        /// <summary>
        /// 搜尋符合地址規範的Data 回傳{true=重複, false=不重複, uncertain=不確定且SQL與Report的地址一樣精準, uncertainSQL=SQL的地址較精準, uncertainReport=Report的地址較精準}
        /// </summary>
        private Boolean addressVerification()
        {
            // 1. 切割報案資訊的地址，拆成鄉鎮市 街路 段 巷 弄 號 樓。
            // 2. 切割從資料庫取出來可能重複的報案資訊的地址，拆成鄉鎮市 街路 段 巷 弄 號 樓，(這些地址很明確的一定有且沒有錯誤的情況下，比鄉鎮市、街路跟號)。
            // 3. 丟入判斷方法，回傳true=重複、false=不重複。
            // 4. 將重複的報案資訊存放在MySql_Repeat_Report_Data。

            // 切割地址存放的變數 {[0]=鄉鎮市,[1]=街路,[2]=段,[3]=巷,[4]=弄,[5]=號,[6]=樓}
            string[] Report_Address = null;
            string[] MySql_Address = null;

            // user提供的地址
            Report_Address = segmentAddress(Report_Information[3]);

            for (int i = 0; i < MySql_Report_Table_Data.Count; i++)
            {
                // 從SQL取出的第i筆地址
                MySql_Address = segmentAddress(MySql_Report_Table_Data[i].Region);

                // 判斷是否為重複報案
                if (addressJudgment(Report_Address, MySql_Address)) // 重複
                {
                    // 將重複報案的資料存放起來
                    MysqlConnect u = new MysqlConnect(MySql_Report_Table_Data[i].Id, MySql_Report_Table_Data[i].Time, MySql_Report_Table_Data[i].Region, MySql_Report_Table_Data[i].Case, MySql_Report_Table_Data[i].Injured, MySql_Report_Table_Data[i].Dialogue);
                    MySql_Repeat_Report_Data.Add(u);
                    return true;
                }
                else // 非重複
                {
                    if (i + 1 == MySql_Report_Table_Data.Count) return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 判斷鄉鎮市到號的方法，回傳{true=重複, false=不重複}。
        /// </summary>
        /// <param name="count"></param>
        /// <param name="Report_Address"></param>
        /// <param name="MySql_Address"></param>
        /// <returns></returns>
        private Boolean addressJudgment(string[] Report_Address, string[] MySql_Address)
        {
            // 由重複率較低的先判斷: 號=>巷=>街路=>弄=>鄉鎮市=>段
            if (Report_Address[0] == MySql_Address[0]) // 鄉鎮市
            {
                if (Report_Address[1] == MySql_Address[1]) // 街路
                {
                    if (Report_Address[5] == MySql_Address[5]) // 號
                    {
                        if (Report_Address[2] == MySql_Address[2]) // 段
                        {
                            if (Report_Address[3] == MySql_Address[3]) // 巷
                            {
                                if (Report_Address[4] == MySql_Address[4]) // 弄
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 切割地址的方法
        /// </summary>
        private string[] segmentAddress(string address)
        {
            // 切割地址存放的變數 {[0]=鄉鎮市,[1]=街路,[2]=段,[3]=巷,[4]=弄,[5]=號,[6]=樓}
            string[] Segement_Address = new string[7];
            string[] Address_Str = { "段", "巷", "弄", "號", "樓" };

            // 開始切割
            for (int i = 0; i <= 6; i++)
            {
                // 當地址已經切割完畢時跳出迴圈
                if (address == "") break;

                if (i == 0) // 鄉鎮市直接切割
                {
                    Segement_Address[i] = address.Substring(0, 3);
                    address = address.Substring(3, address.Length - 3);
                }
                else if (i == 1) // 街路直接切割
                {
                    int Index_Street = getTextKey(address, "街");
                    int Index_Road = getTextKey(address, "路");
                    if (Index_Street != 0) // 此地址為街
                    {
                        Segement_Address[i] = address.Substring(0, Index_Street) + "街";
                        address = address.Substring(++Index_Street, address.Length - Index_Street);
                    }
                    else // 此地址為路
                    {
                        Segement_Address[i] = address.Substring(0, Index_Road) + "路";
                        address = address.Substring(++Index_Road, address.Length - Index_Road);
                    }
                }
                else // 其餘迴圈切割
                {
                    int index = getTextKey(address, Address_Str[i - 2]);
                    if (index != 0) // 有取到關鍵字才去做切割
                    {
                        Segement_Address[i] = address.Substring(0, index + 1);
                        int nIndex = index + 1;
                        address = address.Substring(nIndex, address.Length - nIndex);
                    }

                    //Segement_Address[i] = address.Substring(0, index) + Address_Str[i - 2];
                    //address = address.Substring(index, address.Length - index);
                }
            }
            return Segement_Address;
        }

        /// <summary>
        /// 取字串中指定的關鍵字 回傳索引
        /// </summary>
        private int getTextKey(string Text, string key)
        {
            for (int i = 0; i < Text.Length; i++)
            {
                if (Text.Substring(i, 1) == key)
                {
                    return i;
                }
            }
            return 0;
        }

        // 顯示重複報案的Data
    }
}
