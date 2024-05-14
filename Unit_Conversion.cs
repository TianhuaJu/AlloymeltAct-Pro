namespace AlloyAct_Pro
{
    public partial class Unit_Conversion : Form
    {
        public Unit_Conversion()
        {
            InitializeComponent();


        }

        private void weight_radioButton1_Click(object sender, EventArgs e)
        {
            weight_radioButton1.Checked = true;
            atomFraction_radio.Checked = false;
            converted_Object();
        }

        private void atomFraction_radio_Click(object sender, EventArgs e)
        {
            weight_radioButton1.Checked = false;
            atomFraction_radio.Checked = true;
            converted_Object();
        }

        private void converted_Object()
        {
            if (atomFraction_radio.Checked)
            {
                weight_btn.BackColor = Color.White;
                atomFraction_btn.BackColor = Color.Blue;
            }
            else
            {
                weight_btn.BackColor = Color.Blue;
                atomFraction_btn.BackColor = Color.White;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string k = k_comboBox1.Text.Trim();
            string i = i_comboBox2.Text.Trim();
            string j = j_comboBox3.Text.Trim();
            string originalData = originalData_Text.Text.Trim();
            if (k != string.Empty && i != string.Empty && j != string.Empty)
            {
                double orgn_data;

                if (originalData != string.Empty && double.TryParse(originalData, out orgn_data))
                {
                    if (weight_radioButton1.Checked)
                    {
                        textBox2.Text = myFunctions.first_order_w2m(orgn_data, new Element(j), new Element(k)).ToString();
                    }
                    else
                    {
                        textBox2.Text = myFunctions.first_order_mTow(orgn_data, new Element(j), new Element(k)).ToString();
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

        private void button4_Click(object sender, EventArgs e)
        {
            originalData_Text.Clear();
            textBox2.Clear();
            k_comboBox1.Text = string.Empty;
            i_comboBox2.Text = string.Empty;
            j_comboBox3.Text = string.Empty;
        }
    }
}
