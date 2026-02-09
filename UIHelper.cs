using System.Text.RegularExpressions;

namespace AlloyAct_Pro
{
    /// <summary>
    /// UI辅助类，提供公共方法减少重复代码
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// 常用金属元素列表（用于ComboBox预填充）
        /// </summary>
        public static readonly string[] CommonMetals = new string[]
        {
            "Fe", "Cu", "Ni", "Co", "Mn", "Cr", "Mo", "W", "V", "Ti",
            "Al", "Si", "Zn", "Sn", "Pb", "Ag", "Au", "Pt", "Pd"
        };

        /// <summary>
        /// 常用溶质元素列表
        /// </summary>
        public static readonly string[] CommonSolutes = new string[]
        {
            "C", "Si", "Mn", "P", "S", "O", "N", "H", "B",
            "Cr", "Ni", "Mo", "V", "Ti", "Al", "Cu", "Nb", "W"
        };

        /// <summary>
        /// 常用温度值（K）
        /// </summary>
        public static readonly string[] CommonTemperatures = new string[]
        {
            "1873", "1823", "1773", "1723", "1673", "1623", "1573", "1273"
        };

        /// <summary>
        /// 显示或激活已存在的窗体
        /// </summary>
        /// <typeparam name="T">窗体类型</typeparam>
        /// <param name="form">窗体引用</param>
        /// <returns>返回显示的窗体实例</returns>
        public static T ShowOrActivateForm<T>(ref T form) where T : Form, new()
        {
            if (form == null || form.IsDisposed)
            {
                form = new T();
                form.Show();
            }
            else
            {
                if (!form.Visible)
                {
                    form.Show();
                }
                if (form.WindowState == FormWindowState.Minimized)
                {
                    form.WindowState = FormWindowState.Normal;
                }
                form.BringToFront();
                form.Activate();
            }
            return form;
        }

        /// <summary>
        /// 以键对形式解析熔体的组成，返回标准化的摩尔分数
        /// </summary>
        /// <param name="solvent">溶剂/基体元素</param>
        /// <param name="alloyComposition">合金组成字符串（如 "Mn0.1Si0.02"）</param>
        /// <returns>元素-摩尔分数字典</returns>
        public static Dictionary<string, double> ParseComposition(string solvent, string alloyComposition)
        {
            Dictionary<string, double> compositionDict = new Dictionary<string, double>();
            Regex elementPattern = new Regex(@"([A-Z]{1}[a-z]?)(\d+[\.]?\d*)?");

            string fullComposition = solvent + alloyComposition;
            MatchCollection matches = elementPattern.Matches(fullComposition);

            foreach (Match match in matches)
            {
                string element = match.Groups[1].Value;
                double fraction = 1.0;

                if (!string.IsNullOrEmpty(match.Groups[2].Value))
                {
                    double.TryParse(match.Groups[2].Value, out fraction);
                }

                if (compositionDict.ContainsKey(element))
                {
                    compositionDict[element] = fraction; // 后面的值覆盖前面的
                }
                else
                {
                    compositionDict.Add(element, fraction);
                }
            }

            // 标准化为摩尔分数
            double sum = compositionDict.Values.Sum();
            if (sum > 0)
            {
                var keys = compositionDict.Keys.ToList();
                foreach (var key in keys)
                {
                    compositionDict[key] = compositionDict[key] / sum;
                }
            }

            return compositionDict;
        }

        /// <summary>
        /// 初始化ComboBox，添加常用元素选项
        /// </summary>
        /// <param name="comboBox">要初始化的ComboBox</param>
        /// <param name="items">要添加的项目</param>
        /// <param name="allowEdit">是否允许编辑</param>
        public static void InitializeComboBox(ComboBox comboBox, string[] items, bool allowEdit = true)
        {
            comboBox.Items.Clear();
            comboBox.Items.AddRange(items);
            comboBox.DropDownStyle = allowEdit ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList;
            comboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            comboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
        }

        /// <summary>
        /// 验证必填字段是否为空
        /// </summary>
        /// <param name="controls">控件和字段名称的元组数组</param>
        /// <returns>验证是否通过</returns>
        public static bool ValidateRequiredFields(params (Control control, string fieldName)[] controls)
        {
            List<string> emptyFields = new List<string>();

            foreach (var (control, fieldName) in controls)
            {
                string value = control.Text?.Trim();
                if (string.IsNullOrEmpty(value))
                {
                    emptyFields.Add(fieldName);
                    HighlightControl(control, true);
                }
                else
                {
                    HighlightControl(control, false);
                }
            }

            if (emptyFields.Count > 0)
            {
                MessageBox.Show(
                    $"请填写以下必填字段：\n{string.Join("、", emptyFields)}",
                    "输入验证",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 高亮显示控件（用于验证失败提示）
        /// </summary>
        /// <param name="control">控件</param>
        /// <param name="highlight">是否高亮</param>
        private static void HighlightControl(Control control, bool highlight)
        {
            if (control is ComboBox || control is TextBox)
            {
                control.BackColor = highlight ? Color.MistyRose : SystemColors.Window;
            }
        }

        /// <summary>
        /// 验证温度输入
        /// </summary>
        /// <param name="temperatureText">温度文本</param>
        /// <param name="temperature">解析出的温度值</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>是否验证通过</returns>
        public static bool ValidateTemperature(string temperatureText, out double temperature, double defaultValue = 1873.0)
        {
            if (string.IsNullOrWhiteSpace(temperatureText))
            {
                temperature = defaultValue;
                return true;
            }

            if (double.TryParse(temperatureText.Trim(), out temperature))
            {
                if (temperature > 0 && temperature < 10000)
                {
                    return true;
                }
            }

            MessageBox.Show(
                "请输入有效的温度值（K），范围：0 ~ 10000",
                "温度验证",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            temperature = defaultValue;
            return false;
        }

        /// <summary>
        /// 格式化组成显示字符串
        /// </summary>
        /// <param name="compositionDict">组成字典</param>
        /// <param name="excludeElement">要排除的元素（如基体）</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatComposition(Dictionary<string, double> compositionDict, string excludeElement = null)
        {
            var parts = compositionDict
                .Where(kvp => kvp.Key != excludeElement)
                .Select(kvp => $"{kvp.Key}{Math.Round(kvp.Value, 3)}");
            return string.Join("", parts);
        }

        /// <summary>
        /// 安全关闭窗体
        /// </summary>
        /// <param name="form">要关闭的窗体</param>
        public static void SafeCloseForm(Form form)
        {
            if (form != null && !form.IsDisposed)
            {
                try
                {
                    form.Close();
                }
                catch
                {
                    // 忽略关闭时的异常
                }
            }
        }
    }
}
