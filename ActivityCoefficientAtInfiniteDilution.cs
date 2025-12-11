namespace AlloyAct_Pro
{
    public partial class ActivityCoefficientAtInfiniteDilution : Form
    {
        private HelpActvtyInfiniteFM _helpForm;

        public ActivityCoefficientAtInfiniteDilution()
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
            UIHelper.InitializeComboBox(k_combox, UIHelper.CommonMetals);
            k_combox.SelectedItem = "Fe";

            // 初始化溶质ComboBox
            UIHelper.InitializeComboBox(i_combox, UIHelper.CommonSolutes);

            // 初始化温度ComboBox
            UIHelper.InitializeComboBox(T_combox, UIHelper.CommonTemperatures);
            T_combox.SelectedItem = "1873";

            // 默认液态
            SetLiquidState();
        }

        /// <summary>
        /// 设置为液态
        /// </summary>
        private void SetLiquidState()
        {
            L_checkBox.Checked = true;
            liquidToolStripMenuItem.Checked = true;
            solidToolStripMenuItem.Checked = false;
            S_checkBox.Checked = false;
        }

        /// <summary>
        /// 设置为固态
        /// </summary>
        private void SetSolidState()
        {
            L_checkBox.Checked = false;
            liquidToolStripMenuItem.Checked = false;
            solidToolStripMenuItem.Checked = true;
            S_checkBox.Checked = true;
        }

        private void L_checkBox_Click(object sender, EventArgs e)
        {
            SetLiquidState();
        }

        private void S_checkBox_Click(object sender, EventArgs e)
        {
            SetSolidState();
        }

        private void liquidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetLiquidState();
        }

        private void solidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetSolidState();
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
            // 输入验证
            if (!UIHelper.ValidateRequiredFields(
                (k_combox, "基体(k)"),
                (i_combox, "溶质(i)")))
            {
                return;
            }

            string matrix = k_combox.Text.Trim();
            string soluteI = i_combox.Text.Trim();

            // 温度验证
            if (!UIHelper.ValidateTemperature(T_combox.Text, out double temperature))
            {
                return;
            }

            // 验证溶质不能与基体相同
            if (matrix == soluteI)
            {
                MessageBox.Show(
                    "溶质不能与基体相同，请重新选择。",
                    "输入错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            fill_data(matrix, soluteI, temperature, get_State(), ref row);
        }

        private string get_State()
        {
            return L_checkBox.Checked ? "liquid" : "solid";
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myFunctions.saveToExcel(dataGridView1);
        }

        private void Reset_btn_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            // 重置为默认值
            k_combox.SelectedItem = "Fe";
            i_combox.Text = string.Empty;
            T_combox.SelectedItem = "1873";

            // 重置控件背景色
            k_combox.BackColor = SystemColors.Window;
            i_combox.BackColor = SystemColors.Window;
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UIHelper.ShowOrActivateForm(ref _helpForm);
        }

        private void ActivityCoefficientAtInfiniteDilution_FormClosed(object sender, FormClosedEventArgs e)
        {
            UIHelper.SafeCloseForm(_helpForm);
            if (Program.F1 != null)
            {
                Program.F1.WindowState = FormWindowState.Normal;
            }
        }
    }
}
