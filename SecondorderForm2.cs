namespace AlloyAct_Pro
{
    public partial class SecondorderForm2 : Form
    {
        public SecondorderForm2()
        {
            InitializeComponent();
        }

        int row = 0;
        private void Cal_btn_Click(object sender, EventArgs e)
        {
            string m = m_comboBox1.Text.Trim();
            bool t1;
            double Tem;

            t1 = double.TryParse(T_comboBox4.Text, out Tem);
            if (!t1) { Tem = 1873.0; }

            if (m == string.Empty)
            {
                m = "Fe";
            }
            string i, j, k;
            i = i_comboBox2.Text.Trim();
            j = j_comboBox3.Text.Trim();
            k = k_comboBox2.Text.Trim();
            display(m, i, j, k);//显示各元素的Miedema参数
            (string phase, bool entropy, double Tem) info = (getState(), entropy_Judge(m, i, j, k), Tem);
            filldata_dgV(m, i, j, k, info, ref row);


        }

        private void liquid_checkBox1_Click(object sender, EventArgs e)
        {
            Solid_checkBox1.Checked = false;
            liquid_checkBox1.Checked = true;
        }

        private void Solid_checkBox1_Click(object sender, EventArgs e)
        {
            Solid_checkBox1.Checked = true;
            liquid_checkBox1.Checked = false;
        }

        private void display(string m, string i, string j, string k)
        {
            Element Em = new Element(m);
            Element Ei = new Element(i);
            Element Ej = new Element(j);
            Element Ek = new Element(k);

            iphi.Text = Ei.Phi.ToString();
            inws.Text = Ei.N_WS.ToString();
            iV.Text = Ei.V.ToString();


            jphi.Text = Ej.Phi.ToString();
            jnws.Text = Ej.N_WS.ToString();
            jV.Text = Ej.V.ToString();

            //m为基体
            kphi.Text = Em.Phi.ToString();
            knws.Text = Em.N_WS.ToString();
            kV.Text = Em.V.ToString();

            //组分k
            k_btn.Text = Ek.Phi.ToString();
            k_nws.Text = Ek.N_WS.ToString();
            k_V.Text = Ek.V.ToString();

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

        private void filldata_dgV(string m, string i, string j, string k, (string state, bool entropy, double Tem) info, ref int row)
        {
            double Tem = info.Tem;
            if (m != string.Empty && i != string.Empty && j != string.Empty && k != string.Empty)
            {
                Element solv = null, solui = null, soluj = null, soluk = null;
                solv = new Element(m);
                solui = new Element(i);
                soluj = new Element(j);
                soluk = new Element(k);
                Ternary_melts wagner_ = null;
                wagner_ = new Ternary_melts(Tem, info.state, info.entropy);

                Binary_model miedemal = null;
                miedemal = new Binary_model();
                miedemal.setState(info.state);
                miedemal.setTemperature(info.Tem);
                miedemal.setEntropy(info.entropy);

                double rii = 0, rij = 0, rjj = 0, rjk = 0;

                rii = wagner_.Roui_ii(solv, solui, miedemal.UEM1);
                rij = wagner_.Roui_ij(solv, solui, soluj, miedemal.UEM1);
                rjj = wagner_.Roui_jj(solv, solui, soluj, miedemal.UEM1);
                rjk = wagner_.Roui_jk(solv, solui, soluj, soluk, miedemal.UEM1);

                Melt m1 = new Melt(m, i, j, Tem);


                row = +dataGridView1.Rows.Add();
                dataGridView1["compositions", row].Value = m + "-" + i + "-" + j + "-" + k;
                dataGridView1["ri_ii", row].Value = Math.Round(rii, 3);
                dataGridView1["ri_ij", row].Value = Math.Round(rij, 3);
                dataGridView1["ri_jj", row].Value = Math.Round(rjj, 3);
                dataGridView1["ri_jk", row].Value = Math.Round(rjk, 3);
                dataGridView1["ExperimentalValue", row].Value = double.NaN;
                dataGridView1["state", row].Value = getState();
                dataGridView1["Temperature", row].Value = info.Tem;

                dataGridView1.Update();
            }

        }

        /// <summary>
        /// 对体系是否考虑过剩熵的判断
        /// </summary>
        /// <param name="m"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        private bool entropy_Judge(string m, string i, string j, string k)
        {
            List<string> s = new List<string>() { m, i, j, k };
            if (s.Contains("O"))
            {
                //O与非金属元素相互作用时，考虑过剩熵
                if (i == "O")
                {
                    if (constant.non_metallst.Contains<string>(j) || constant.non_metallst.Contains<string>(m))
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
                    if (constant.non_metallst.Contains<string>(i) || constant.non_metallst.Contains<string>(m))
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
                return true;
            }

        }


    }
}
