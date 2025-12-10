using System.Text.RegularExpressions;
using Match = System.Text.RegularExpressions.Match;

namespace AlloyAct_Pro
{
    public partial class ActivityCoefficientFm : Form
    {
        private HelpActCoeffFM _helpForm;

        public ActivityCoefficientFm()
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
            UIHelper.InitializeComboBox(k_comboBox2, UIHelper.CommonMetals);
            k_comboBox2.SelectedItem = "Fe";

            // 初始化温度ComboBox
            UIHelper.InitializeComboBox(temp_comboBox4, UIHelper.CommonTemperatures);
            temp_comboBox4.SelectedItem = "1873";

            // 初始化溶质ComboBox
            UIHelper.InitializeComboBox(i_comboBox3, UIHelper.CommonSolutes);

            // 默认液态
            checkBox1.Checked = true;
            checkBox2.Checked = false;
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {
        }
        private void filldata_gv(string matrix, string composition, string solutei, double Tem, string state, Geo_Model geo_Model, string GeoModel, ref int row)
        {

            double Darken_acf, Wagner_acf, Elloit_acf;
            string alloy_melts = matrix + composition;

            Dictionary<string, double> comp_dict = get_Compositions(matrix, alloy_melts);
            Activity_Coefficient activity_ = new Activity_Coefficient();//活度系数计算模块



            Wagner_acf = activity_.activity_Coefficient_Wagner(comp_dict, matrix, solutei, geo_Model, GeoModel, (state, Tem));

            Darken_acf = activity_.activity_coefficient_Pelton(comp_dict, solutei, matrix, Tem, geo_Model, GeoModel, state);
            Elloit_acf = activity_.activity_coefficient_Elloit(comp_dict, solutei, matrix, Tem, geo_Model, GeoModel, state);

            string compostion_new = "";

            comp_dict.Remove(matrix);
            foreach (var item in comp_dict.Keys)
            {
                compostion_new += item + Math.Round(comp_dict[item], 3);
            }

            row = +dataGridView1.Rows.Add();
            dataGridView1["Melt_composition", row].Value = compostion_new;
            dataGridView1["solute_i", row].Value = solutei;
            dataGridView1["Tem", row].Value = Tem;

            dataGridView1["state", row].Value = state;

            dataGridView1["activityCoefficient", row].Value = Math.Round(Darken_acf, 3);
            dataGridView1["acf_wagner", row].Value = Math.Round(Wagner_acf, 3);
            dataGridView1["acf_elloit", row].Value = Math.Round(Elloit_acf, 3);

            dataGridView1["k_name", row].Value = matrix;
            dataGridView1.Update();

        }

        /// <summary>
        /// 以键对形式存储熔体的组成，1mol的熔体
        /// </summary>
        /// <param name="alloyComposition">从输入读取的组成 </param>
        /// <returns></returns>
        private Dictionary<string, double> get_Compositions(string solv, string alloyComposition)
        {
            //以键对形式存储熔体的组成，1mol的形式
            Dictionary<string, double> compo_dict = new Dictionary<string, double>();
            Regex re = new Regex(@"([A-Z]{1}[a-z]?)(\d+[\.]?\d*)?");

            string composition = solv + alloyComposition;
            MatchCollection matchs = re.Matches(composition);

            foreach (Match match in matchs)
            {
                double x = 1.0;
                string A = "";
                GroupCollection groups = match.Groups;
                A = groups[1].Value;
                if (double.TryParse(groups[2].Value, out x))
                {
                    double.TryParse(groups[2].Value, out x);
                }
                else { x = 1.0; }
                if (compo_dict.ContainsKey(A))
                {
                    compo_dict[A] = x;
                }
                else
                {
                    compo_dict.Add(A, x);
                }

            }

            double sumx = 0;
            foreach (var item in compo_dict.Keys)
            {
                sumx += compo_dict[item];
            }

            foreach (var item in compo_dict.Keys)
            {
                compo_dict[item] = compo_dict[item] / sumx;
            }

            return compo_dict;





        }

        int row = 0;
        private void Cal_btn_Click(object sender, EventArgs e)
        {
            // 输入验证
            if (!UIHelper.ValidateRequiredFields(
                (k_comboBox2, "基体(k)"),
                (alloy_comboBox1, "合金组成"),
                (i_comboBox3, "溶质(i)")))
            {
                return;
            }

            string solvent = k_comboBox2.Text.Trim();
            string alloyComposition = alloy_comboBox1.Text.Trim();
            string soluteI = i_comboBox3.Text.Trim();

            // 温度验证
            if (!UIHelper.ValidateTemperature(temp_comboBox4.Text, out double temperature))
            {
                return;
            }

            // 解析组成
            Dictionary<string, double> compositionsDict = UIHelper.ParseComposition(solvent, alloyComposition);
            string state = get_State();

            // 验证溶质是否在组成中
            if (!compositionsDict.ContainsKey(soluteI))
            {
                MessageBox.Show(
                    $"溶质 {soluteI} 不在合金组成中，请检查输入。\n" +
                    $"当前组成包含：{string.Join(", ", compositionsDict.Keys)}",
                    "溶质验证",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (solvent == soluteI)
            {
                MessageBox.Show(
                    "溶质不能与基体相同，请重新选择溶质。",
                    "输入错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // 执行计算
            Binary_model binaryModel = new Binary_model();
            binaryModel.setState(state);
            binaryModel.setTemperature(temperature);
            filldata_gv(solvent, alloyComposition, soluteI, temperature, state, binaryModel.UEM1, "UEM1", ref row);
        }

        private string get_State()
        {
            return checkBox1.Checked ? "liquid" : "solid";
        }

        private void checkBox1_Click(object sender, EventArgs e)
        {
            // 液态
            checkBox1.Checked = true;
            checkBox2.Checked = false;
        }

        private void checkBox2_Click(object sender, EventArgs e)
        {
            // 固态
            checkBox1.Checked = false;
            checkBox2.Checked = true;
        }

        private void i_comboBox3_Click(object sender, EventArgs e)
        {
            string text = alloy_comboBox1.Text;
            Dictionary<string, double> dict = get_Compositions(k_comboBox2.Text, text);
            if (dict.Count >= 1)
            {
                foreach (string item in dict.Keys)
                {
                    if (!i_comboBox3.Items.Contains(item))
                    {

                        if (item != k_comboBox2.Text)
                        {
                            i_comboBox3.Items.Add(item);

                        }


                    }


                }
            }

        }

        private void reset_btn_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            // 重置为默认值
            k_comboBox2.SelectedItem = "Fe";
            i_comboBox3.Text = string.Empty;
            temp_comboBox4.SelectedItem = "1873";
            alloy_comboBox1.Text = string.Empty;

            // 重置控件背景色
            k_comboBox2.BackColor = SystemColors.Window;
            i_comboBox3.BackColor = SystemColors.Window;
            alloy_comboBox1.BackColor = SystemColors.Window;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myFunctions.saveToExcel(dataGridView1);
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UIHelper.ShowOrActivateForm(ref _helpForm);
        }

        private void ActivityCoefficientFm_FormClosed(object sender, FormClosedEventArgs e)
        {
            UIHelper.SafeCloseForm(_helpForm);
            if (Program.F1 != null)
            {
                Program.F1.WindowState = FormWindowState.Normal;
            }
        }
    }
}
