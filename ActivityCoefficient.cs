using System.Text.RegularExpressions;
using Match = System.Text.RegularExpressions.Match;

namespace AlloyAct_Pro
{
    public partial class ActivityCoefficientFm : Form
    {
        public ActivityCoefficientFm()
        {
            InitializeComponent();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
        private void filldata_gv(string matrix, string composition, string solutei, double Tem, string state, Geo_Model geo_Model, string GeoModel, ref int row)
        {

            double Pelton_acf, Wagner_acf;
            string alloy_melts = matrix + composition;

            Dictionary<string, double> comp_dict = get_Compositions(matrix, alloy_melts);
            Activity_Coefficient activity_ = new Activity_Coefficient();//活度系数计算模块



            Wagner_acf = activity_.activity_Coefficient_Wagner(comp_dict, matrix, solutei, geo_Model, GeoModel, (state, Tem));

            Pelton_acf = activity_.activity_coefficient_Pelton(comp_dict, solutei, matrix, Tem, geo_Model, GeoModel, state);

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

            dataGridView1["activityCoefficient", row].Value = Pelton_acf;

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
            string slov = k_comboBox2.Text.Trim();
            string alloy_melts = alloy_comboBox1.Text.Trim();
            string solute_i = i_comboBox3.Text.Trim();
            double T = 0;
            string state;
            double.TryParse(temp_comboBox4.Text.Trim(), out T);

            if (slov != string.Empty && alloy_melts != string.Empty && solute_i != string.Empty)
            {

                Dictionary<string, double> compositions_dict = get_Compositions(slov, alloy_melts);
                state = get_State();
                Binary_model binary_Model = new Binary_model();
                binary_Model.setState(state);
                binary_Model.setTemperature(T);

                if (compositions_dict.ContainsKey(solute_i) && slov != solute_i)
                {
                    filldata_gv(slov, alloy_melts, solute_i, T, state, binary_Model.UEM1, "UEM1", ref row);

                }
                else
                {
                    MessageBox.Show("重新输入溶质i");
                }

            }



        }

        private string get_State()
        {
            if (checkBox1.Checked)
            {
                return "liquid";
            }
            else
            {
                return "solid";
            }
        }
        private void checkBox1_Click(object sender, EventArgs e)
        {
            //熔体状态为液态
            checkBox1.Checked = true;
            checkBox2.Checked = false;
        }

        private void checkBox2_Click(object sender, EventArgs e)
        {
            //熔体状态为固态
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
            k_comboBox2.Text = string.Empty;
            i_comboBox3.Text = string.Empty;
            temp_comboBox4.Text = string.Empty;
            alloy_comboBox1.Text = string.Empty;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myFunctions.saveToExcel(dataGridView1);
        }
    }
}
