using System.Text.RegularExpressions;

namespace AlloyAct_Pro
{
    public partial class ActivityFm : Form
    {
        private HelpActFm _helpForm;
        private int _row = 0;

        public ActivityFm()
        {
            InitializeComponent();
            InitializeControls();
            SetupEventHandlers();
        }

        /// <summary>
        /// 初始化控件
        /// </summary>
        private void InitializeControls()
        {
            // 初始化基体ComboBox - 允许输入和选择
            UIHelper.InitializeComboBox(k_comboBox2, UIHelper.CommonMetals);
            k_comboBox2.SelectedItem = "Fe";

            // 初始化温度ComboBox - 允许输入和选择
            UIHelper.InitializeComboBox(temp_comboBox4, UIHelper.CommonTemperatures);
            temp_comboBox4.SelectedItem = "1873";

            // 溶质ComboBox - 只能从列表选择（DropDownList样式）
            i_comboBox3.DropDownStyle = ComboBoxStyle.DropDownList;
            i_comboBox3.Items.Clear();
            i_comboBox3.Items.Add("-- 请先输入合金组成 --");
            i_comboBox3.SelectedIndex = 0;
            i_comboBox3.Enabled = false;

            // 默认液态
            checkBox1.Checked = true;
            checkBox2.Checked = false;
        }

        /// <summary>
        /// 设置事件处理
        /// </summary>
        private void SetupEventHandlers()
        {
            // 当合金组成改变时，自动更新溶质列表
            alloy_comboBox1.TextChanged += AlloyComposition_TextChanged;
            alloy_comboBox1.Leave += AlloyComposition_Leave;
            k_comboBox2.TextChanged += Matrix_TextChanged;
        }

        /// <summary>
        /// 合金组成文本改变时更新溶质列表
        /// </summary>
        private void AlloyComposition_TextChanged(object sender, EventArgs e)
        {
            UpdateSoluteList();
        }

        /// <summary>
        /// 离开合金组成输入框时更新
        /// </summary>
        private void AlloyComposition_Leave(object sender, EventArgs e)
        {
            UpdateSoluteList();
        }

        /// <summary>
        /// 基体改变时更新溶质列表
        /// </summary>
        private void Matrix_TextChanged(object sender, EventArgs e)
        {
            UpdateSoluteList();
        }

        /// <summary>
        /// 更新溶质下拉列表
        /// </summary>
        private void UpdateSoluteList()
        {
            string matrix = k_comboBox2.Text.Trim();
            string composition = alloy_comboBox1.Text.Trim();

            i_comboBox3.Items.Clear();

            if (string.IsNullOrEmpty(composition))
            {
                i_comboBox3.Items.Add("-- 请先输入合金组成 --");
                i_comboBox3.SelectedIndex = 0;
                i_comboBox3.Enabled = false;
                i_comboBox3.BackColor = Color.LightGray;
                return;
            }

            // 解析组成，获取所有溶质元素
            Dictionary<string, double> compDict = ParseCompositionElements(composition);

            if (compDict.Count == 0)
            {
                i_comboBox3.Items.Add("-- 无有效溶质 --");
                i_comboBox3.SelectedIndex = 0;
                i_comboBox3.Enabled = false;
                i_comboBox3.BackColor = Color.LightGray;
                return;
            }

            // 添加溶质元素（排除基体）
            foreach (string element in compDict.Keys)
            {
                if (element != matrix)
                {
                    i_comboBox3.Items.Add(element);
                }
            }

            if (i_comboBox3.Items.Count > 0)
            {
                i_comboBox3.Enabled = true;
                i_comboBox3.BackColor = SystemColors.Window;
                i_comboBox3.SelectedIndex = 0;
            }
            else
            {
                i_comboBox3.Items.Add("-- 无可选溶质 --");
                i_comboBox3.SelectedIndex = 0;
                i_comboBox3.Enabled = false;
                i_comboBox3.BackColor = Color.LightGray;
            }
        }

        /// <summary>
        /// 解析组成字符串中的元素
        /// </summary>
        private Dictionary<string, double> ParseCompositionElements(string composition)
        {
            Dictionary<string, double> result = new Dictionary<string, double>();
            Regex re = new Regex(@"([A-Z]{1}[a-z]?)(\d+[\.]?\d*)?");
            MatchCollection matches = re.Matches(composition);

            foreach (Match match in matches)
            {
                string element = match.Groups[1].Value;
                double fraction = 1.0;
                if (!string.IsNullOrEmpty(match.Groups[2].Value))
                {
                    double.TryParse(match.Groups[2].Value, out fraction);
                }

                if (!result.ContainsKey(element))
                {
                    result.Add(element, fraction);
                }
            }
            return result;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myFunctions.saveToExcel(dataGridView1);
        }

        private void checkBox1_Click(object sender, EventArgs e)
        {
            checkBox1.Checked = true;
            checkBox2.Checked = false;
        }

        private void checkBox2_Click(object sender, EventArgs e)
        {
            checkBox1.Checked = false;
            checkBox2.Checked = true;
        }

        private string get_State()
        {
            return checkBox1.Checked ? "liquid" : "solid";
        }

        private void filldata_gv(string matrix, string composition, string solutei, double Tem, string state, Geo_Model geo_Model, string GeoModel, ref int row)
        {
            double Pelton_acf, xi, Wagner_act, Elloit_act;
            string alloy_melts = matrix + composition;

            Dictionary<string, double> comp_dict = get_Compositions(matrix, alloy_melts);
            Activity_Coefficient activity_ = new Activity_Coefficient();

            // 计算活度系数 (lnγi)
            Pelton_acf = activity_.activity_coefficient_Pelton(comp_dict, solutei, matrix, Tem, geo_Model, GeoModel, state);
            Wagner_act = activity_.activity_Coefficient_Wagner(comp_dict, matrix, solutei, geo_Model, GeoModel, (state, Tem));
            Elloit_act = activity_.activity_coefficient_Elloit(comp_dict, solutei, matrix, Tem, geo_Model, GeoModel, state);

            xi = comp_dict[solutei];

            // 活度计算: ai = γi × xi (纯物质标态，理想溶液参考态)
            double activity_Pelton = Math.Exp(Pelton_acf) * xi;
            double activity_Wagner = Math.Exp(Wagner_act) * xi;
            double activity_Elloit = Math.Exp(Elloit_act) * xi;

            // 格式化组成显示
            string compostion_new = "";
            comp_dict.Remove(matrix);
            foreach (var item in comp_dict.Keys)
            {
                compostion_new += item + Math.Round(comp_dict[item], 3);
            }

            row = dataGridView1.Rows.Add();
            dataGridView1["k_name", row].Value = matrix;
            dataGridView1["Melt_composition", row].Value = compostion_new;
            dataGridView1["solute_i", row].Value = solutei;
            dataGridView1["xi", row].Value = Math.Round(xi, 4);
            dataGridView1["activity", row].Value = Math.Round(activity_Pelton, 4);
            dataGridView1["ai_wagner", row].Value = Math.Round(activity_Wagner, 4);
            dataGridView1["ai_elloit", row].Value = Math.Round(activity_Elloit, 4);
            dataGridView1["Tem", row].Value = Tem;
            dataGridView1["state", row].Value = state;

            dataGridView1.Update();
        }

        /// <summary>
        /// 以键对形式存储熔体的组成，标准化为1mol
        /// </summary>
        private Dictionary<string, double> get_Compositions(string solv, string alloyComposition)
        {
            Dictionary<string, double> compo_dict = new Dictionary<string, double>();
            Regex re = new Regex(@"([A-Z]{1}[a-z]?)(\d+[\.]?\d*)?");

            string composition = solv + alloyComposition;
            MatchCollection matchs = re.Matches(composition);

            foreach (Match match in matchs)
            {
                double x = 1.0;
                string A = match.Groups[1].Value;
                if (double.TryParse(match.Groups[2].Value, out x))
                {
                    // 成功解析
                }
                else
                {
                    x = 1.0;
                }

                if (compo_dict.ContainsKey(A))
                {
                    compo_dict[A] = x;
                }
                else
                {
                    compo_dict.Add(A, x);
                }
            }

            // 标准化为摩尔分数
            double sumx = compo_dict.Values.Sum();
            if (sumx > 0)
            {
                foreach (var key in compo_dict.Keys.ToList())
                {
                    compo_dict[key] = compo_dict[key] / sumx;
                }
            }

            return compo_dict;
        }

        private void Cal_btn_Click(object sender, EventArgs e)
        {
            // 输入验证
            string matrix = k_comboBox2.Text.Trim();
            string composition = alloy_comboBox1.Text.Trim();

            if (string.IsNullOrEmpty(matrix))
            {
                MessageBox.Show("请输入基体元素", "输入验证", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                k_comboBox2.Focus();
                return;
            }

            if (string.IsNullOrEmpty(composition))
            {
                MessageBox.Show("请输入合金组成", "输入验证", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                alloy_comboBox1.Focus();
                return;
            }

            if (!i_comboBox3.Enabled || i_comboBox3.SelectedItem == null ||
                i_comboBox3.SelectedItem.ToString().StartsWith("--"))
            {
                MessageBox.Show("请从列表中选择溶质元素", "输入验证", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string soluteI = i_comboBox3.SelectedItem.ToString();

            // 温度验证
            if (!UIHelper.ValidateTemperature(temp_comboBox4.Text, out double temperature))
            {
                temp_comboBox4.Focus();
                return;
            }

            string state = get_State();

            // 执行计算
            Binary_model binaryModel = new Binary_model();
            binaryModel.setState(state);
            binaryModel.setTemperature(temperature);
            filldata_gv(matrix, composition, soluteI, temperature, state, binaryModel.UEM1, "UEM1", ref _row);
        }

        private void i_comboBox3_Click(object sender, EventArgs e)
        {
            // 点击时如果未启用，提示用户
            if (!i_comboBox3.Enabled)
            {
                MessageBox.Show("请先输入合金组成，溶质将自动从组成中提取", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                alloy_comboBox1.Focus();
            }
        }

        private void reset_btn_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            _row = 0;

            // 重置为默认值
            k_comboBox2.SelectedItem = "Fe";
            temp_comboBox4.SelectedItem = "1873";
            alloy_comboBox1.Text = string.Empty;

            // 重置溶质列表
            i_comboBox3.Items.Clear();
            i_comboBox3.Items.Add("-- 请先输入合金组成 --");
            i_comboBox3.SelectedIndex = 0;
            i_comboBox3.Enabled = false;
            i_comboBox3.BackColor = Color.LightGray;

            // 重置控件背景色
            k_comboBox2.BackColor = SystemColors.Window;
            alloy_comboBox1.BackColor = SystemColors.Window;
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UIHelper.ShowOrActivateForm(ref _helpForm);
        }

        private void ActivityFm_FormClosed(object sender, FormClosedEventArgs e)
        {
            UIHelper.SafeCloseForm(_helpForm);
            if (Program.F1 != null)
            {
                Program.F1.WindowState = FormWindowState.Normal;
            }
        }
    }
}
