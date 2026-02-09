using System.Text.RegularExpressions;

namespace AlloyAct_Pro.Controls
{
    public partial class ActivityPanel : UserControl
    {
        public string PageTitle => "Activity Calculation";

        public ActivityPanel()
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
            cboSolute.Text = string.Empty;
            cboTemp.Text = string.Empty;
            txtComposition.Text = string.Empty;
        }

        private string GetState() => rbLiquid.Checked ? "liquid" : "solid";

        private void filldata_gv(string matrix, string composition, string solutei, double Tem, string state, Geo_Model geo_Model, string GeoModel, ref int row)
        {
            double Pelton_acf, acf, xi, Wagner_act, Elloit_act;
            string alloy_melts = matrix + composition;
            Dictionary<string, double> comp_dict = get_Compositions(matrix, alloy_melts);
            Activity_Coefficient activity_ = new Activity_Coefficient();

            Pelton_acf = activity_.activity_coefficient_Pelton(comp_dict, solutei, matrix, Tem, geo_Model, GeoModel, state);
            xi = comp_dict[solutei];
            acf = Math.Exp(Pelton_acf) * xi;
            Wagner_act = activity_.activity_Coefficient_Wagner(comp_dict, matrix, solutei, geo_Model, GeoModel, (state, Tem));
            Wagner_act = Math.Exp(Wagner_act) * xi;
            Elloit_act = activity_.activity_coefficient_Elloit(comp_dict, solutei, matrix, Tem, geo_Model, GeoModel, state);
            Elloit_act = Math.Exp(Elloit_act) * xi;

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
            dataGridView1["activity", row].Value = Math.Round(acf, 3);
            dataGridView1["ai_wagner", row].Value = Math.Round(Wagner_act, 3);
            dataGridView1["ai_elloit", row].Value = Math.Round(Elloit_act, 3);
            dataGridView1["xi", row].Value = Math.Round(xi, 3);
            dataGridView1["k_name", row].Value = matrix;
            dataGridView1.Update();
        }

        private Dictionary<string, double> get_Compositions(string solv, string alloyComposition)
        {
            Dictionary<string, double> compo_dict = new Dictionary<string, double>();
            Regex re = new Regex(@"([A-Z]{1}[a-z]?)(\d+[\.]?\d*)?");
            string composition = solv + alloyComposition;
            MatchCollection matchs = re.Matches(composition);

            foreach (Match match in matchs)
            {
                double x = 1.0;
                GroupCollection groups = match.Groups;
                string A = groups[1].Value;
                if (!double.TryParse(groups[2].Value, out x)) { x = 1.0; }
                if (compo_dict.ContainsKey(A))
                    compo_dict[A] = x;
                else
                    compo_dict.Add(A, x);
            }

            double sumx = 0;
            foreach (var item in compo_dict.Keys) sumx += compo_dict[item];
            foreach (var item in compo_dict.Keys) compo_dict[item] = compo_dict[item] / sumx;
            return compo_dict;
        }

        int row = 0;
        private void Cal_btn_Click(object sender, EventArgs e)
        {
            string slov = cboMatrix.Text.Trim();
            string alloy_melts = txtComposition.Text.Trim();
            string solute_i = cboSolute.Text.Trim();
            double T = 0;
            double.TryParse(cboTemp.Text.Trim(), out T);

            if (slov != string.Empty && alloy_melts != string.Empty && solute_i != string.Empty)
            {
                Dictionary<string, double> compositions_dict = get_Compositions(slov, alloy_melts);
                string state = GetState();
                Binary_model binary_Model = new Binary_model();
                binary_Model.setState(state);
                binary_Model.setTemperature(T);

                if (compositions_dict.ContainsKey(solute_i) && slov != solute_i)
                {
                    filldata_gv(slov, alloy_melts, solute_i, T, state, binary_Model.UEM1, "UEM1", ref row);
                }
                else
                {
                    MessageBox.Show("Please re-enter solute (i)");
                }
            }
        }

        private void cboSolute_Click(object sender, EventArgs e)
        {
            string text = txtComposition.Text;
            Dictionary<string, double> dict = get_Compositions(cboMatrix.Text, text);
            if (dict.Count >= 1)
            {
                foreach (string item in dict.Keys)
                {
                    if (!cboSolute.Items.Contains(item) && item != cboMatrix.Text)
                    {
                        cboSolute.Items.Add(item);
                    }
                }
            }
        }

        private void Reset_btn_Click(object sender, EventArgs e)
        {
            ResetAll();
        }
    }
}
