using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlloyAct_Pro
{
    public partial class UnitConvertFm : Form
    {
        public UnitConvertFm()
        {
            InitializeComponent();
        }

        private void weight_radioButton1_Click(object sender, EventArgs e)
        {
            weight_radioButton1.Checked = true;
            atom_radioButton2.Checked = false;
        }

        private void atom_radioButton2_Click(object sender, EventArgs e)
        {
            weight_radioButton1.Checked = false;
            atom_radioButton2.Checked = true;
        }

        private void Calcu_btn_Click(object sender, EventArgs e)
        {
            string k = k_comboBox1.Text.Trim();
            string i = i_comboBox2.Text.Trim();
            string j = j_comboBox3.Text.Trim();
            string originalData = originalData_textBox1.Text.Trim();
            if (k != string.Empty && i != string.Empty && j != string.Empty)
            {
                double orgn_data;

                if (originalData != string.Empty && double.TryParse(originalData, out orgn_data))
                {
                    if (weight_radioButton1.Checked)
                    {
                        //original data is represented by weight percent
                        atomFractionDisplay.Text = myFunctions.first_order_w2m(orgn_data, new Element(j), new Element(k)).ToString();
                        weightPercentDisplay.Text = orgn_data.ToString();
                        weightPercentDisplay.BackColor = Color.White;
                        atomFractionDisplay.BackColor = Color.Turquoise;
                    }
                    else
                    {
                        //original data is represented by atom fraction
                        weightPercentDisplay.Text = myFunctions.first_order_mTow(orgn_data, new Element(j), new Element(k)).ToString();
                        atomFractionDisplay.Text = orgn_data.ToString();
                        atomFractionDisplay.BackColor = Color.White;
                        weightPercentDisplay.BackColor = Color.Turquoise;
                    }


                }
                else
                {
                    MessageBox.Show("检查输入的待转换的相互作用系数");
                }

            }
            else
            {
                MessageBox.Show("检查输入的元素符号");
            }
        }

        private void Reset_btn_Click(object sender, EventArgs e)
        {
            originalData_textBox1.Clear();

            k_comboBox1.Text = string.Empty;
            i_comboBox2.Text = string.Empty;
            j_comboBox3.Text = string.Empty;
            weightPercentDisplay.Text = string.Empty;
            atomFractionDisplay.Text = string.Empty;
            weightPercentDisplay.BackColor = Color.White;
            atomFractionDisplay.BackColor = Color.White;
        }
    }
}
