using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// 使用GoogleMap服務
    /// </summary>
    class GoogleMapServices
    {
        string MapUrl = "https://maps.googleapis.com/maps/api/geocode/json?";   // 查詢經緯度網址
        /// <summary>
        /// 以住址查詢,回傳經緯度
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public location GetLatLngByAddr(string addr)
        {
            location _result = new location();
            GoogleGeoCodeResponse _mapdata = new GoogleGeoCodeResponse();
            _mapdata = ConvertAddressToLatLng(addr);
            if (_mapdata.status == "OK")
            {
                _result.lat = _mapdata.results[0].geometry.location.lat;
                _result.lng = _mapdata.results[0].geometry.location.lng;
            }
            return _result;
        }

        /// <summary>
        /// 以住址去取得Google Maps API Json results
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public GoogleGeoCodeResponse ConvertAddressToLatLng(string addr)
        {
            string result = string.Empty;
            string googlemapkey = "AIzaSyA8XFoUhlmfGu-rv_bhQB99suJsBoLUO_w";    //GoogleMapAPI金鑰，試用期三個月過了記得要去重辦
            string url = MapUrl + "&address={0}";
            url = string.Format(url, addr);
            url += "&key=" + googlemapkey;

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            using (var response = request.GetResponse())
            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                result = sr.ReadToEnd();
            }
            return JsonConvert.DeserializeObject<GoogleGeoCodeResponse>(result);
        }

        public class GoogleGeoCodeResponse
        {
            public string status { get; set; }
            public results[] results { get; set; }
        }
        public class results
        {
            public string formatted_address { get; set; }
            public geometry geometry { get; set; }
            public string[] types { get; set; }
            public address_component[] address_components { get; set; }
        }
        public class geometry
        {
            public string location_type { get; set; }
            public location location { get; set; }
        }
        public class location
        {
            public double lat { get; set; }
            public double lng { get; set; }
        }
        public class address_component
        {
            public string long_name { get; set; }
            public string short_name { get; set; }
            public string[] types { get; set; }
        }
    }
}
