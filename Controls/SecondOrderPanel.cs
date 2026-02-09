namespace AlloyAct_Pro.Controls
{
    public partial class SecondOrderPanel : UserControl
    {
        public string PageTitle => "Second-Order Interaction Coefficient";

        public SecondOrderPanel()
        {
            InitializeComponent();
        }

        public void ExportToExcel() { myFunctions.saveToExcel(dataGridView1); }
        public void ResetAll()
        {
            dataGridView1.Rows.Clear();
            cboM.Text = string.Empty; cboI.Text = string.Empty;
            cboJ.Text = string.Empty; cboK.Text = string.Empty;
            cboTemp.Text = string.Empty;
        }

        private string GetState() => rbLiquid.Checked ? "liquid" : "solid";

        private void display(string m, string i, string j, string k)
        {
            Element Em = new Element(m); Element Ei = new Element(i);
            Element Ej = new Element(j); Element Ek = new Element(k);
            lblMPhi.Text = Em.Phi.ToString("F2"); lblMNws.Text = Em.N_WS.ToString("F2"); lblMV.Text = Em.V.ToString("F2");
            lblIPhi.Text = Ei.Phi.ToString("F2"); lblINws.Text = Ei.N_WS.ToString("F2"); lblIV.Text = Ei.V.ToString("F2");
            lblJPhi.Text = Ej.Phi.ToString("F2"); lblJNws.Text = Ej.N_WS.ToString("F2"); lblJV.Text = Ej.V.ToString("F2");
            lblKPhi.Text = Ek.Phi.ToString("F2"); lblKNws.Text = Ek.N_WS.ToString("F2"); lblKV.Text = Ek.V.ToString("F2");
        }

        int row = 0;
        private void Cal_btn_Click(object sender, EventArgs e)
        {
            string m = cboM.Text.Trim();
            double Tem;
            if (!double.TryParse(cboTemp.Text, out Tem)) Tem = 1873.0;
            if (m == string.Empty) m = "Fe";
            string i = cboI.Text.Trim(), j = cboJ.Text.Trim(), k = cboK.Text.Trim();
            display(m, i, j, k);
            (string phase, bool entropy, double Tem) info = (GetState(), entropy_Judge(m, i, j, k), Tem);
            filldata_dgV(m, i, j, k, info, ref row);
        }

        private void filldata_dgV(string m, string i, string j, string k, (string state, bool entropy, double Tem) info, ref int row)
        {
            double Tem = info.Tem;
            if (m != string.Empty && i != string.Empty && j != string.Empty && k != string.Empty)
            {
                Element solv = new Element(m); Element solui = new Element(i);
                Element soluj = new Element(j); Element soluk = new Element(k);
                Ternary_melts wagner_ = new Ternary_melts(Tem, info.state, info.entropy);
                Binary_model miedemal = new Binary_model();
                miedemal.setState(info.state); miedemal.setTemperature(info.Tem); miedemal.setEntropy(info.entropy);

                double rii = wagner_.Roui_ii(solv, solui, miedemal.UEM1);
                double rij = wagner_.Roui_ij(solv, solui, soluj, miedemal.UEM1);
                double rjj = wagner_.Roui_jj(solv, solui, soluj, miedemal.UEM1);
                double rjk = wagner_.Roui_jk(solv, solui, soluj, soluk, miedemal.UEM1);

                row = +dataGridView1.Rows.Add();
                dataGridView1["compositions", row].Value = m + "-" + i + "-" + j + "-" + k;
                dataGridView1["ri_ii", row].Value = Math.Round(rii, 3);
                dataGridView1["ri_ij", row].Value = Math.Round(rij, 3);
                dataGridView1["ri_jj", row].Value = Math.Round(rjj, 3);
                dataGridView1["ri_jk", row].Value = Math.Round(rjk, 3);
                dataGridView1["state", row].Value = GetState();
                dataGridView1["Temperature", row].Value = info.Tem;
                dataGridView1.Update();
            }
        }

        private bool entropy_Judge(string m, string i, string j, string k)
        {
            List<string> s = new List<string>() { m, i, j, k };
            if (s.Contains("O"))
            {
                if (i == "O") return constant.non_metallst.Contains(j) || constant.non_metallst.Contains(m);
                else if (j == "O") return constant.non_metallst.Contains(i) || constant.non_metallst.Contains(m);
                else return constant.non_metallst.Contains(j) || constant.non_metallst.Contains(i);
            }
            else if (s.Contains("H") || s.Contains("N")) return false;
            else return true;
        }

        private void Reset_btn_Click(object sender, EventArgs e) { ResetAll(); }
    }
}
