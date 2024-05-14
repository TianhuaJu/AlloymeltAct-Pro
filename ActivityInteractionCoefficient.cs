using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static NPOI.HSSF.Util.HSSFColor;

namespace AlloyAct_Pro
{
    public partial class ActivityInteractionCoefficientFm : Form
    {
        UnitConvertFm unit_conversionFm = new UnitConvertFm();


        public ActivityInteractionCoefficientFm()
        {
            InitializeComponent();
        }





        private void Solid_checkBox1_Click_1(object sender, EventArgs e)
        {
            Solid_checkBox1.Checked = true;
            liquid_checkBox1.Checked = false;
        }

        private void liquid_checkBox1_Click(object sender, EventArgs e)
        {
            Solid_checkBox1.Checked = false;
            liquid_checkBox1.Checked = true;
        }

        int row = 0;
        private void Cal_btn_Click(object sender, EventArgs e)
        {
            string k = k_comboBox1.Text.Trim();
            bool t1;
            double Tem;

            t1 = double.TryParse(T_comboBox4.Text, out Tem);
            if (!t1) { Tem = 1873.0; }

            if (k == string.Empty)
            {
                k = "Fe";
            }
            string i, j;
            i = i_comboBox2.Text.Trim();
            j = j_comboBox3.Text.Trim();

            display(k, i, j);//显示各元素的Miedema参数
            (string phase, bool entropy, double Tem) info = (getState(), entropy_Judge(k, i, j), Tem);
            filldata_dgV(k, i, j, info, ref row);

        }
        private void display(string k, string i, string j)
        {
            Element Ei = new Element(i);
            Element Ej = new Element(j);
            Element Ek = new Element(k);

            iphi.Text = Ei.Phi.ToString();
            inws.Text = Ei.N_WS.ToString();
            iV.Text = Ei.V.ToString();


            jphi.Text = Ej.Phi.ToString();
            jnws.Text = Ej.N_WS.ToString();
            jV.Text = Ej.V.ToString();

            kphi.Text = Ek.Phi.ToString();
            knws.Text = Ek.N_WS.ToString();
            kV.Text = Ek.V.ToString();

        }
        private string getState()
        {
            if (Solid_checkBox1.Checked)
            {
                return "solid";
            }
            else
            {
                return "liquid";
            }

        }


        private void filldata_dgV(string k, string i, string j, (string state, bool entropy, double Tem) info, ref int row)
        {
            double Tem = info.Tem;
            bool t = true;
            if (k != string.Empty && i != string.Empty && j != string.Empty)
            {
                Element solv = null, solui = null, soluj = null;
                solv = new Element(k);
                solui = new Element(i);
                soluj = new Element(j);
                Ternary_melts wagner_ = null;
                wagner_ = new Ternary_melts(Tem, info.state, info.entropy);

                Binary_model miedemal = null;
                miedemal = new Binary_model();
                miedemal.setState(info.state);
                miedemal.setTemperature(info.Tem);
                miedemal.setEntropy(info.entropy);

                double sij_UEM1 = 0, sij_UEM2 = 0, sij_exp;
                string flag = "";

                sij_UEM1 = wagner_.Activity_Interact_Coefficient_Model(solv, solui, soluj, miedemal.UEM1, "UEM1");
                sij_UEM2 = wagner_.Activity_Interact_Coefficient_Model(solv, solui, soluj, miedemal.UEM2, "UEM2-Adv");



                Melt m1 = new Melt(k, i, j, Tem);
                if (info.state == "liquid")
                {
                    sij_exp = m1.sji;
                }
                else
                {
                    sij_exp = double.NaN;
                }

                row = +dataGridView1.Rows.Add();
                dataGridView1["compositions", row].Value = k + "-" + i + "-" + j;
                dataGridView1["CalculatedResult", row].Value = sij_UEM1;
                dataGridView1["Remark", row].Value = sij_UEM2;

                dataGridView1["ExperimentalValue", row].Value = sij_exp;
                dataGridView1["state", row].Value = getState();
                dataGridView1["Temperature", row].Value = info.Tem;

                dataGridView1.Update();

                m1 = null;
                solv = null;
                solui = null;
                soluj = null;
                System.GC.Collect();

            }

        }

        private bool entropy_Judge(string k, string i, string j)
        {
            List<string> s = new List<string>() { k, i, j };
            if (s.Contains("C") || s.Contains("Si") || s.Contains("B"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myFunctions.saveToExcel(dataGridView1);
        }

        private void Clear_btn_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            k_comboBox1.Text = string.Empty;
            i_comboBox2.Text = string.Empty;
            T_comboBox4.Text = string.Empty;
            j_comboBox3.Text = string.Empty;
        }

        private void unitConversionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (unit_conversionFm.IsDisposed)
            {
                UnitConvertFm unit_conversionFm = new UnitConvertFm();
                unit_conversionFm.Show();

            }
            else
            {
                if (unit_conversionFm.Visible == false)
                {
                    unit_conversionFm.Visible = true;
                    unit_conversionFm.Show();
                }
                if (unit_conversionFm.WindowState == FormWindowState.Minimized)
                {
                    unit_conversionFm.WindowState = FormWindowState.Normal;
                }
            }
        }

        private void ActivityInteractionCoefficientFm_FormClosed(object sender, FormClosedEventArgs e)
        {
            unit_conversionFm.Close();
        }
    }
}
