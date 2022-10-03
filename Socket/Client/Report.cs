using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Report
    {
        public Report()
        {

        }

        public int Id { get; set; }

        public string cases { get; set; }

        public string address { get; set; }

        public string status { get; set; }

        public string time { get; set; }                      

        public int injured { get; set; }

        public int fire_truck { get; set; }

        public int ambulance { get; set; }

    }
}
