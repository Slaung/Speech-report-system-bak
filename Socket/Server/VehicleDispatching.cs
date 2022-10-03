using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    // 派車類別 
    class VehicleDispatching
    {
        #region 自定義變數

        /// <summary>
        /// 需派遣的消防車數
        /// </summary>
        public int Get_Output_fire_branchcol = 0;

        /// <summary>
        /// 需派遣的救護車數
        /// </summary>
        public int Get_Output_ambulance = 0;

        /// <summary>
        /// 取得派遣分局
        /// </summary>
        public string Get_Output_Branch = null;

        /// <summary>
        /// 需派遣分局和消防車數　Dictionary A<string region,int fnum> => [0]={頭份,1}
        /// </summary>
        Dictionary<string, int> Fire_truck_Dic = new Dictionary<string, int>();

        /// <summary>
        /// 需派遣分局和消防車數　Dictionary B<string region,int bnum> => [0]={頭份,3} ,[1]={竹南,2}, [2]={造橋,1}
        /// </summary>
        Dictionary<string, int> Ambulance_Dic = new Dictionary<string, int>();

        #endregion

        /// <summary>
        /// 傳入需要的消防車數、救護車數、地址、案件類別，設置派遣任務 包含車輛以及分局
        /// </summary>
        public void setVehicleDispatchingTask(int fnum, int anum, string addr, string cases)
        {
            Get_Output_fire_branchcol = fnum;
            Get_Output_ambulance = anum;
            setBranchDic(addr,cases);//派遣字典
        }


        /// <summary>
        /// 設置派遣消防車數
        /// </summary>
        public int setfire_truck(string cases)
        {
            int fire_truck = 0;
            if (cases == "火災")
            {
                fire_truck = 1;
            }
            return fire_truck;
        }

        /// <summary>
        /// 設置派遣救護車數
        /// </summary>
        public int setambulance(string injured)
        {
            if (injured != "")
            {
                int ambulance = 0;
                int injure = Convert.ToInt32(injured);
                ambulance = injure;
                return ambulance;
            }
            else
            {
                return 0;
            }
        }


        /// <summary>
        /// 設置派遣分局字典
        /// </summary>
        public void setBranchDic(string Addr, string cases)
        {
            MysqlConnect mc = new MysqlConnect();
            string[] branches = { "苗栗", "頭屋", "公館", "銅鑼", "三義", "竹南", "後龍", "造橋", "頭份", "三灣", "南庄", "西湖", "通霄", "苑裡", "獅潭", "大湖", "卓蘭", "泰安", "象鼻" };//所有分局
            int bindex = 0;//負責該轄區分局索引值 
            int fnum = Get_Output_fire_branchcol;//消防車數量
            int anum = Get_Output_ambulance;//救護車數量
            int[] firesql = new int[20];
            int[] amsql = new int[20];
            for (int id = 0; id < 19; id++)//從SQL抓各分局現有消防車數，SQL車數是在消防員介面按下派車時才扣除，這邊只是用來判斷需要多少分局支援
            {
                firesql[id] = mc.getSQLfire_truck2(id + 1);
                amsql[id] = mc.getSQLambulance2(id + 1);
            }

            //該局自己派車
            for (int i = 0; i < 19; i++)
            {
                bool b = Addr.Contains(branches[i]);    //比對地址(Addr)與分局名稱，取得負責該轄區之分局
                if (b)
                {
                    bindex = i;                         //負責該轄區分局索引值                
                }
            }

            if (fnum != 0)
            {                                           //如果需要消防車
                if (firesql[bindex] >= fnum)            //如果最近分局車數 >= 所需支援車數
                {
                    firesql[bindex] -= fnum;            //所需消防車數扣除該分局可派遣車數
                    Fire_truck_Dic.Add(branches[bindex], fnum);
                    fnum = 0;                           //消防車所需歸零
                }
                else if (firesql[bindex] != 0)
                {
                    fnum -= firesql[bindex];
                    Fire_truck_Dic.Add(branches[bindex], firesql[bindex]);
                    firesql[bindex] = 0;                //全派
                }
            }
            if (anum != 0)                              //同上
            {
                if (amsql[bindex] >= anum)
                {
                    amsql[bindex] -= anum;
                    Ambulance_Dic.Add(branches[bindex], anum);
                    anum = 0;
                }
                else if (amsql[bindex] != 0)
                {
                    anum -= amsql[bindex];
                    Ambulance_Dic.Add(branches[bindex], amsql[bindex]);
                    amsql[bindex] = 0;
                }
            }
            if( anum == 0 && fnum == 0 && cases == "抓動物")
            {
                Fire_truck_Dic.Add(branches[bindex], 0);
                Ambulance_Dic.Add(branches[bindex], 0);
            }

            //如果需要其他分局支援，方法同上
            int sf = 0;//第N個支援消防車的分局
            int sa = 0;//第N個支援救護車的分局
            GoogleMapServices _services = new GoogleMapServices();      //引用GoogleMapServices
            GoogleMapServices.location _mapLocation = _services.GetLatLngByAddr(Addr);
            double lat = _mapLocation.lat;//案發地點緯度
            double lng = _mapLocation.lng;//案發地點經度
            while (fnum != 0)
            {
                int Nbranch = getNbranch(lat, lng, sf); //取出第N個支援消防車的分局， getNearestbranch(案發地點緯度 , 案發地點經度 , 取第n近)會回傳分局index
                //getNearestbranch(案發地點緯度 , 案發地點經度 , 取第幾近) 一樣回傳分局index
                if (firesql[Nbranch] >= fnum)
                {
                    firesql[Nbranch] -= fnum;
                    Fire_truck_Dic.Add(branches[Nbranch], fnum);
                    fnum = 0;
                }
                else if (firesql[Nbranch] != 0)
                {
                    fnum -= firesql[Nbranch];
                    Fire_truck_Dic.Add(branches[Nbranch], firesql[Nbranch]);
                    firesql[Nbranch] = 0;
                }
                if (sf<18)
                {
                    sf++;
                }
                else
                {
                    break;
                }
                
            }
            while (anum != 0)
            {
                int Nbranch = getNbranch(lat, lng, sa); //取出第N個支援救護車的分局， getNearestbranch(案發地點緯度 , 案發地點經度 , 取第n近)會回傳分局index
                if (amsql[Nbranch] >= anum)//如果分局車數 >= 所需支援車數
                {
                    amsql[Nbranch] -= anum;//所需車數扣除該分局可派遣車數
                    Ambulance_Dic.Add(branches[Nbranch], anum);
                    anum = 0;//所需歸零
                }
                else if (amsql[Nbranch] != 0)
                {
                    anum -= amsql[Nbranch];
                    Ambulance_Dic.Add(branches[Nbranch], amsql[Nbranch]);
                    amsql[Nbranch] = 0;//全派
                }
                if (sa<18)
                {
                    sa++;
                }
                else
                {
                    break;
                }

            }
        }

        /// <summary>
        /// 取得字典裡的資料,回傳所有需要派遣的消防局名稱
        /// </summary>
        public string getAllBranch()
        {
            string keys = "";//所有需要派遣的消防局ID
            Dictionary<string, int>.KeyCollection keyF = Fire_truck_Dic.Keys;
            Dictionary<string, int>.KeyCollection keyA = Ambulance_Dic.Keys;
            foreach (string s in keyF)
            {
                keys += s + "/";
            }
            foreach (string s in keyA)
            {
                bool b = keys.Contains(s);  //如果keys已經包含派救護車的分局，則跳過
                if (!b)                     //防止派消防車和救護車的分局名稱重複輸出
                {
                    keys += s + "/";
                }
            }
            return keys;//所有需要派遣的分局名稱
        }

        /// <summary>
        /// 取得字典裡的資料,輸入查詢分局名稱和車種(消防車還是救護車)ambulance 或是 fire_truck,返回車數
        /// </summary>
        public int getBranchCarNum(string branchName, string car)
        {
            int fnum = 0;
            int anum = 0;
            Dictionary<string, int>.KeyCollection keyF = Fire_truck_Dic.Keys;
            Dictionary<string, int>.KeyCollection keyA = Ambulance_Dic.Keys;

            foreach (string s in keyF)
            {
                bool b = branchName.Contains(s);
                if (b)
                {
                    fnum = Fire_truck_Dic[branchName];
                }
            }

            foreach (string s in keyA)
            {
                bool b = branchName.Contains(s);
                if (b)
                {
                    anum = Ambulance_Dic[branchName];
                }
            }
            if (car == "fire_truck")
            {
                return fnum;//返回車數
            }
            else
            {
                return anum;//返回車數
            }
        }


        /// <summary>
        /// 取得最近分局
        /// </summary>
        public int getNbranch(double lat1, double lng1, int n)// getNearestbranch(double 案發地點經度 , double 案發地點緯度 , int 取第n近的分局)
        {
            int bindex = 0;
            Double[] lat = { 24.541992076661103, 24.574187861849207, 24.50308494806471, 24.487121798140446, 24.382090399865103, 24.690795214474655, 24.611056426932418, 24.64521465579699, 24.679353689467593, 24.654747751868914, 24.59787551827448, 24.554670207626526, 24.48942729610244, 24.428783678730703, 24.541762985106455, 24.4253107320872, 24.313602598905554, 24.46582244649026, 24.360602718378264 };
            Double[] lng = { 120.82146112848827, 120.8473488784035, 120.82285884862124, 120.7874613905697, 120.75189203126817, 120.86214025565829, 120.78617256629032, 120.87147527387621, 120.91049336361753, 120.95545235547206, 120.99921876902252, 120.7557147929713, 120.6860813775828, 120.65925151117312, 120.92153573197501, 120.86705597569366, 120.81532312388374, 120.94297205405735, 120.94673831763164 };
            double[] save = new double[19];
            double[] darr = new double[19];

            for (int i = 0; i < 19; i++)
            {
                double lat2 = lat[i], lng2 = lng[i];
                double factor = Math.PI / 180;
                double dLat = (lat2 - lat1) * factor;
                double dLon = (lng2 - lng1) * factor;
                double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * factor) * Math.Cos(lat2 * factor) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                double d = 6371 * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));//6371 = earthRadius   d=距離(公里)
                save[i] = d;//比對用
                darr[i] = d;//排序用
            }
            Array.Sort(darr);//小到大排序
            for (int i = 0; i < 19; i++)
            {
                if (darr[n] == save[i])
                { //第n近的距離 == 儲存陣列(未排序)
                    bindex = i;
                }
            }
            return bindex;//回傳第n近分局index
        }

    }
}
