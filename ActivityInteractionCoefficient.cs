namespace AlloyAct_Pro
{
    public partial class ActivityInteractionCoefficientFm : Form
    {
        private UnitConvertFm _unitConvertFm;
        private Help_activityinteractioncoefficient _helpForm;

        public ActivityInteractionCoefficientFm()
        {
            InitializeComponent();
            InitializeControls();
        }

        /// <summary>
        /// 初始化控件，预填充常用元素
        /// </summary>
        private void InitializeControls()
        {
            // 初始化基体ComboBox
            UIHelper.InitializeComboBox(k_comboBox1, UIHelper.CommonMetals);
            k_comboBox1.SelectedItem = "Fe";

            // 初始化溶质i和j的ComboBox
            UIHelper.InitializeComboBox(i_comboBox2, UIHelper.CommonSolutes);
            UIHelper.InitializeComboBox(j_comboBox3, UIHelper.CommonSolutes);

            // 初始化温度ComboBox
            UIHelper.InitializeComboBox(T_comboBox4, UIHelper.CommonTemperatures);
            T_comboBox4.SelectedItem = "1873";

            // 默认液态
            Solid_checkBox1.Checked = false;
            liquid_checkBox1.Checked = true;
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
            // 输入验证
            if (!UIHelper.ValidateRequiredFields(
                (k_comboBox1, "基体(k)"),
                (i_comboBox2, "溶质(i)"),
                (j_comboBox3, "溶质(j)")))
            {
                return;
            }

            string k = k_comboBox1.Text.Trim();
            string i = i_comboBox2.Text.Trim();
            string j = j_comboBox3.Text.Trim();

            // 温度验证
            if (!UIHelper.ValidateTemperature(T_comboBox4.Text, out double temperature))
            {
                return;
            }

            // 显示各元素的Miedema参数
            display(k, i, j);

            (string phase, bool entropy, double Tem) info = (getState(), entropy_Judge(k, i, j), temperature);
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
            return Solid_checkBox1.Checked ? "solid" : "liquid";
        }


        private void filldata_dgV(string k, string i, string j, (string state, bool entropy, double Tem) info, ref int row)
        {
            double Tem = info.Tem;
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

                sij_UEM1 = wagner_.Activity_Interact_Coefficient_1st(solv, solui, soluj, miedemal.UEM1, "UEM1");
                sij_UEM2 = wagner_.Activity_Interact_Coefficient_1st(solv, solui, soluj, miedemal.UEM2, "UEM2-Adv");



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
                dataGridView1["Remark", row].Value = "";

                dataGridView1["ExperimentalValue", row].Value = sij_exp;
                dataGridView1["state", row].Value = getState();
                dataGridView1["Temperature", row].Value = info.Tem;

                dataGridView1.Update();
            }
        }

        /// <summary>
        /// 对体系是否考虑过剩熵的判断
        /// </summary>
        /// <param name="k"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        private bool entropy_Judge(string k, string i, string j)
        {
            List<string> s = new List<string>() { k, i, j };
            if (s.Contains("O"))
            {
                //O与非金属元素相互作用时，考虑过剩熵
                if (i == "O")
                {
                    if (constant.non_metallst.Contains<string>(j) || constant.non_metallst.Contains<string>(k))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (j == "O")
                {
                    if (constant.non_metallst.Contains<string>(i) || constant.non_metallst.Contains<string>(k))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (constant.non_metallst.Contains<string>(j) || constant.non_metallst.Contains<string>(i))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else if (s.Contains("H") || s.Contains("N"))
            {
                //不含O的体系中，但含气体元素H、N，不考虑过剩熵
                return false;

            }
            else
            {
                //不含O的体系中，且不含气体元素H、N，如果含C、Si、Ge，考虑过剩熵，否则不考虑
                if (s.Contains("C") || s.Contains("Si") || s.Contains("B"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myFunctions.saveToExcel(dataGridView1);
        }

        private void Clear_btn_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            // 重置为默认值
            k_comboBox1.SelectedItem = "Fe";
            i_comboBox2.Text = string.Empty;
            j_comboBox3.Text = string.Empty;
            T_comboBox4.SelectedItem = "1873";

            // 重置控件背景色
            k_comboBox1.BackColor = SystemColors.Window;
            i_comboBox2.BackColor = SystemColors.Window;
            j_comboBox3.BackColor = SystemColors.Window;

            // 清空Miedema参数显示
            iphi.Text = string.Empty;
            inws.Text = string.Empty;
            iV.Text = string.Empty;
            jphi.Text = string.Empty;
            jnws.Text = string.Empty;
            jV.Text = string.Empty;
            kphi.Text = string.Empty;
            knws.Text = string.Empty;
            kV.Text = string.Empty;
        }

        private void unitConversionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UIHelper.ShowOrActivateForm(ref _unitConvertFm);
        }

        private void ActivityInteractionCoefficientFm_FormClosed(object sender, FormClosedEventArgs e)
        {
            UIHelper.SafeCloseForm(_helpForm);
            UIHelper.SafeCloseForm(_unitConvertFm);
            if (Program.F1 != null)
            {
                Program.F1.WindowState = FormWindowState.Normal;
            }
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UIHelper.ShowOrActivateForm(ref _helpForm);
        }

        private void secondorderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SecondorderForm2 secondorderForm2 = new SecondorderForm2();
            secondorderForm2.Show();
        }
    }
}
