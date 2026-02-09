namespace AlloyAct_Pro.Controls
{
    public partial class InfiniteDilutionPanel : UserControl
    {
        public string PageTitle => "Infinite Dilution Coefficient";

        public InfiniteDilutionPanel()
        {
            InitializeComponent();
        }

        public void ExportToExcel() { myFunctions.saveToExcel(dataGridView1); }
        public void ResetAll()
        {
            dataGridView1.Rows.Clear();
            cboMatrix.Text = string.Empty;
            cboSolute.Text = string.Empty;
            cboTemp.Text = string.Empty;
        }

        private string GetState() => rbLiquid.Checked ? "liquid" : "solid";

        int row = 0;
        private void fill_data(string solvent, string solute_i, double Tem, string State, ref int row)
        {
            Element Ek = new Element(solvent);
            Element Ei = new Element(solute_i);
            Ternary_melts ternary_Melts = new Ternary_melts();
            ternary_Melts.setState(State);
            ternary_Melts.setTemperature(Tem);

            List<string> non_Meta = new List<string>() { "C", "Si", "S", "B", "P" };
            ternary_Melts.setEntropy(non_Meta.Contains(solute_i));

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

        private void Cal_btn_Click(object sender, EventArgs e)
        {
            string matrix = cboMatrix.Text.Trim();
            string solute_i = cboSolute.Text.Trim();
            double T;
            double.TryParse(cboTemp.Text.Trim(), out T);

            if (matrix != string.Empty && solute_i != string.Empty)
                fill_data(matrix, solute_i, T, GetState(), ref row);
            else
                MessageBox.Show("Check element symbols and temperature");
        }

        private void Reset_btn_Click(object sender, EventArgs e) { ResetAll(); }
    }
}
