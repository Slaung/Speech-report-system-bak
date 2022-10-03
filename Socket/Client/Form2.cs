using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Form2 : Form
    {
        #region 自定義變數

        /// <summary>
        /// 即時消防車數
        /// </summary>
        private int fnum;
        public int Fnum
        {
            set
            {
                fnum = value;
            }
        }

        /// <summary>
        /// 即時救護車數
        /// </summary>
        private int anum;
        public int Anum
        {
            set
            {
                anum = value;
            }
        }

        /// <summary>
        /// 維修中消防車數
        /// </summary>
        private int fservicing;
        public int Fservicing
        {
            set
            {
                fservicing = value;
            }
        }

        /// <summary>
        /// 維修中救護車數
        /// </summary>
        private int aservicing;
        public int Aservicing
        {
            set
            {
                aservicing = value;
            }
        }

        /// <summary>
        /// 設置 進場維修消防車和救護車下拉選單的值為 0 ~ 現有車數，維修歸隊消防車和救護車下拉選單的值為 0 ~ 維修中車數，防止消防員輸入大於現有車數和維修中車數。
        /// </summary>
        public void SetValue()
        {
            for (int fn = 0; fn <= fnum; fn++)
            {
                this.comboBox1.Items.Add(fn);
            }
            for (int an = 0; an <= anum; an++)
            {
                this.comboBox2.Items.Add(an);
            }

            for (int fn2 = 0; fn2 <= fservicing; fn2++)
            {
                this.comboBox3.Items.Add(fn2);
            }
            for (int an2 = 0; an2 <= aservicing; an2++)
            {
                this.comboBox4.Items.Add(an2);
            }
        }

        /// <summary>
        /// 進場維修消防車數
        /// </summary>
        public int serviceFire_truck = 0;

        /// <summary>
        /// 進場維修救護車數
        /// </summary>
        public int serviceAmbulance = 0;

        /// <summary>
        /// 維修歸隊消防車數
        /// </summary>
        public int backFire_truck = 0;

        /// <summary>
        /// 維修歸隊救護車數
        /// </summary>
        public int backAmbulance = 0;

        #endregion

        public Form2()
        {
            InitializeComponent();
            button1.DialogResult = System.Windows.Forms.DialogResult.OK; // 設定button1為OK  
        }

        /// <summary>
        /// 回傳進廠維修車數和維修回歸車數給From1
        /// </summary>
        private void returnServiceCarsNum()
        {
            if (comboBox1.SelectedItem != null)
            {
                serviceFire_truck = Convert.ToInt32(comboBox1.SelectedItem.ToString());
            }
            else
            {
                serviceFire_truck = 0;
            }

            if (comboBox2.SelectedItem != null)
            {
                serviceAmbulance = Convert.ToInt32(comboBox2.SelectedItem.ToString());
            }
            else
            {
                serviceAmbulance = 0;
            }

            if (comboBox3.SelectedItem != null)
            {
                backFire_truck = Convert.ToInt32(comboBox3.SelectedItem.ToString());
            }
            else
            {
                backFire_truck = 0;
            }

            if (comboBox4.SelectedItem != null)
            {
                backAmbulance =  Convert.ToInt32(comboBox4.SelectedItem.ToString());
            }
            else
            {
                backAmbulance = 0;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            returnServiceCarsNum();
        }
    }
    
}
