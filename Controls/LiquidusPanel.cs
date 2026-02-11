using System.Text.RegularExpressions;

namespace AlloyAct_Pro.Controls
{
    public partial class LiquidusPanel : UserControl
    {
        public string PageTitle => "Liquidus Temperature";

        public LiquidusPanel()
        {
            InitializeComponent();
        }

        public void ExportToExcel()
        {
            myFunctions.saveToExcel(dataGridView1);
        }

        public void ResetAll()
        {
            dataGridView1.Rows.Clear();
            cboMatrix.Text = string.Empty;
            txtComposition.Text = string.Empty;
            cboTemp.Text = string.Empty;
        }

        private string GetState() => rbLiquid.Checked ? "liquid" : "solid";

        private Dictionary<string, double> GetCompositions(string solvent, string alloyComposition)
        {
            Dictionary<string, double> compo_dict = new Dictionary<string, double>();
            Regex re = new Regex(@"([A-Z]{1}[a-z]?)(\d+[\.]?\d*)?");
            string composition = solvent + alloyComposition;
            MatchCollection matches = re.Matches(composition);

            foreach (Match match in matches)
            {
                double x = 1.0;
                GroupCollection groups = match.Groups;
                string element = groups[1].Value;
                if (!string.IsNullOrEmpty(groups[2].Value))
                {
                    double.TryParse(groups[2].Value, out x);
                }
                if (compo_dict.ContainsKey(element))
                    compo_dict[element] = x;
                else
                    compo_dict.Add(element, x);
            }

            // 标准化为摩尔分数
            double sumx = compo_dict.Values.Sum();
            if (sumx > 0)
            {
                var keys = compo_dict.Keys.ToList();
                foreach (var key in keys)
                    compo_dict[key] = compo_dict[key] / sumx;
            }
            return compo_dict;
        }

        private void Cal_btn_Click(object sender, EventArgs e)
        {
            string matrix = cboMatrix.Text.Trim();
            string composition = txtComposition.Text.Trim();

            if (string.IsNullOrEmpty(matrix))
            {
                MessageBox.Show("Please select a matrix element.", "Input Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(composition))
            {
                MessageBox.Show("Please enter the alloy composition.\nExample: Mn0.02Si0.01C0.005",
                    "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double Tref = 1873.0;
            if (!string.IsNullOrEmpty(cboTemp.Text.Trim()))
            {
                if (!double.TryParse(cboTemp.Text.Trim(), out Tref) || Tref <= 0)
                {
                    MessageBox.Show("Please enter a valid reference temperature (K).",
                        "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            string state = GetState();
            Dictionary<string, double> comp_dict = GetCompositions(matrix, composition);

            if (!comp_dict.ContainsKey(matrix))
            {
                MessageBox.Show($"Matrix element '{matrix}' not found in composition.",
                    "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 设置 Binary_model 用于 UEM1 几何模型
            Binary_model bm = new Binary_model();
            bm.setState(state);
            bm.setTemperature(Tref);

            try
            {
                Cursor = Cursors.WaitCursor;

                LiquidusCalculator calc = new LiquidusCalculator();
                double userDeltaHf = double.NaN;

                // 第一次尝试计算（不提供用户 ΔHf）
                var result = calc.CalculateLiquidus(matrix, comp_dict, state, bm.UEM1, "UEM1");

                // 检查是否需要用户输入 ΔHf
                if (!result.Converged && result.ErrorMessage != null
                    && result.ErrorMessage.StartsWith("NEED_DELTAHF:"))
                {
                    Cursor = Cursors.Default;

                    // 解析 sentinel: "NEED_DELTAHF:元素:Tm"
                    string[] parts = result.ErrorMessage.Split(':');
                    string missingElement = parts.Length > 1 ? parts[1] : matrix;
                    double Tm = 0;
                    if (parts.Length > 2) double.TryParse(parts[2], out Tm);

                    // 弹出对话框要求用户输入
                    using (var dlg = new FusionEnthalpyInputDialog(missingElement, Tm))
                    {
                        var dlgResult = dlg.ShowDialog(this.FindForm());
                        if (dlgResult != DialogResult.OK || double.IsNaN(dlg.ResultDeltaHf))
                        {
                            // 用户取消 → 终止计算
                            MessageBox.Show(
                                $"Calculation cancelled.\n\n" +
                                $"ΔHf for '{missingElement}' is required to compute the liquidus temperature.",
                                "Calculation Stopped",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                            return;
                        }

                        userDeltaHf = dlg.ResultDeltaHf;
                    }

                    // 使用用户输入的 ΔHf 重新计算
                    Cursor = Cursors.WaitCursor;
                    result = calc.CalculateLiquidus(matrix, comp_dict, state, bm.UEM1, "UEM1", userDeltaHf);
                }

                // 显示结果
                int row = dataGridView1.Rows.Add();
                dataGridView1["col_matrix", row].Value = result.MatrixElement;
                dataGridView1["col_composition", row].Value = result.Composition;
                dataGridView1["col_Tm", row].Value = result.T_pure_melting;
                dataGridView1["col_deltaHf", row].Value = result.DeltaHf;
                dataGridView1["col_Tliq_Wagner", row].Value = double.IsNaN(result.T_liquidus_Wagner) ? (object)"N/A" : Math.Round(result.T_liquidus_Wagner, 1);
                dataGridView1["col_Tliq_Pelton", row].Value = double.IsNaN(result.T_liquidus_Pelton) ? (object)"N/A" : Math.Round(result.T_liquidus_Pelton, 1);
                dataGridView1["col_Tliq_Elliot", row].Value = double.IsNaN(result.T_liquidus_Elliot) ? (object)"N/A" : Math.Round(result.T_liquidus_Elliot, 1);
                dataGridView1["col_deltaT", row].Value = double.IsNaN(result.DeltaT_Wagner) ? (object)"N/A" : Math.Round(result.DeltaT_Wagner, 1);
                dataGridView1["col_a_solvent", row].Value = double.IsNaN(result.SolventActivity_Wagner) ? (object)"N/A" : Math.Round(result.SolventActivity_Wagner, 4);
                dataGridView1["col_converged", row].Value = result.Converged ? "OK" : "Warning";

                if (!result.Converged && !string.IsNullOrEmpty(result.ErrorMessage))
                {
                    dataGridView1.Rows[row].DefaultCellStyle.ForeColor = Color.OrangeRed;
                    MessageBox.Show($"Calculation completed with warnings:\n{result.ErrorMessage}",
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                dataGridView1.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Calculation error: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void Reset_btn_Click(object sender, EventArgs e)
        {
            ResetAll();
        }
    }
}
