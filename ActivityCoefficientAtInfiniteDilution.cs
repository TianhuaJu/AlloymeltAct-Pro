namespace AlloyAct_Pro
{
    public partial class ActivityCoefficientAtInfiniteDilution : Form
    {
        public ActivityCoefficientAtInfiniteDilution()
        {
            InitializeComponent();
        }

        private void L_checkBox_Click(object sender, EventArgs e)
        {
            L_checkBox.Checked = true;
            liquidToolStripMenuItem.Checked = true;
            solidToolStripMenuItem.Checked = false;
            S_checkBox.Checked = false;
        }

        private void S_checkBox_Click(object sender, EventArgs e)
        {
            L_checkBox.Checked = false;
            liquidToolStripMenuItem.Checked = false;
            solidToolStripMenuItem.Checked = true;
            S_checkBox.Checked = true;
        }

        private void liquidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            L_checkBox.Checked = true;
            liquidToolStripMenuItem.Checked = true;
            solidToolStripMenuItem.Checked = false;
            S_checkBox.Checked = false;
        }

        private void solidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            L_checkBox.Checked = false;
            liquidToolStripMenuItem.Checked = false;
            solidToolStripMenuItem.Checked = true;
            S_checkBox.Checked = true;
        }

        private void fill_data(string solvent, string solute_i, double Tem, string State, ref int row)
        {
            Element Ek = new Element(solvent);
            Element Ei = new Element(solute_i);
            Ternary_melts ternary_Melts = new Ternary_melts();
            ternary_Melts.setState(State);
            ternary_Melts.setTemperature(Tem);

            List<string> non_Meta = new List<string>() { "C", "Si", "S", "B", "P" };
            if (non_Meta.Contains(solute_i))
            {
                ternary_Melts.setEntropy(true);
            }
            else
            {
                ternary_Melts.setEntropy(false);
            }


            double lnyi0 = ternary_Melts.lnY0(Ek, Ei);

            Melt melt = new Melt(solvent, solute_i, Tem);
            double lnYi_exp = melt.lnYi;

            row = +dataGridView1.Rows.Add();
            dataGridView1["melts", row].Value = solvent + '-' + solute_i;
            dataGridView1["lnYi", row].Value = lnyi0;
            dataGridView1["exp", row].Value = lnYi_exp;
            dataGridView1["Tem", row].Value = Tem;
            dataGridView1["state", row].Value = State;
            dataGridView1["Remark", row].Value = "";
            dataGridView1.Update();



        }
        int row = 0;
        private void calc_btn_Click(object sender, EventArgs e)
        {
            //计算无限稀活度系数
            string matrix = k_combox.Text.Trim();
            string solute_i = i_combox.Text.Trim();
            double T;
            double.TryParse(T_combox.Text.Trim(), out T);

            if (matrix != string.Empty && solute_i != string.Empty)
            {
                fill_data(matrix, solute_i, T, get_State(), ref row);
            }
            else
            {
                MessageBox.Show("检查输入元素符号及温度值");
            }



        }
        private string get_State()

        {
            if (L_checkBox.Checked)
            {
                return "liquid";
            }
            else
            {
                return "solid";
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myFunctions.saveToExcel(dataGridView1);
        }

        private void Reset_btn_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            k_combox.Text = string.Empty;
            i_combox.Text = string.Empty;
            T_combox.Text = string.Empty;
        }
        HelpActvtyInfiniteFM helpActInfiniteFM = new HelpActvtyInfiniteFM();
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (helpActInfiniteFM.IsDisposed)
            {
                HelpActvtyInfiniteFM helpActInfiniteFM = new HelpActvtyInfiniteFM();
                helpActInfiniteFM.Show();
            }
            else
            {
                if (helpActInfiniteFM.Visible == false)
                {
                    helpActInfiniteFM.Visible = true;
                    helpActInfiniteFM.Show();
                }
                if (helpActInfiniteFM.WindowState == FormWindowState.Minimized)
                {
                    helpActInfiniteFM.WindowState = FormWindowState.Normal;
                }
            }
        }

        private void ActivityCoefficientAtInfiniteDilution_FormClosed(object sender, FormClosedEventArgs e)
        {
            helpActInfiniteFM.Close();
        }
    }
}
