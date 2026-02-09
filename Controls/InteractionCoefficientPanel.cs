namespace AlloyAct_Pro.Controls
{
    public partial class InteractionCoefficientPanel : UserControl
    {
        public string PageTitle => "Interaction Coefficient (1st Order)";

        public InteractionCoefficientPanel()
        {
            InitializeComponent();
        }

        public void ExportToExcel() { myFunctions.saveToExcel(dataGridView1); }
        public void ResetAll()
        {
            dataGridView1.Rows.Clear();
            cboK.Text = string.Empty;
            cboI.Text = string.Empty;
            cboJ.Text = string.Empty;
            cboTemp.Text = string.Empty;
            ClearMiedemaDisplay();
        }

        private void ClearMiedemaDisplay()
        {
            lblKPhi.Text = ""; lblKNws.Text = ""; lblKV.Text = "";
            lblIPhi.Text = ""; lblINws.Text = ""; lblIV.Text = "";
            lblJPhi.Text = ""; lblJNws.Text = ""; lblJV.Text = "";
        }

        private string GetState() => rbLiquid.Checked ? "liquid" : "solid";

        private void display(string k, string i, string j)
        {
            Element Ei = new Element(i);
            Element Ej = new Element(j);
            Element Ek = new Element(k);
            lblKPhi.Text = Ek.Phi.ToString("F2"); lblKNws.Text = Ek.N_WS.ToString("F2"); lblKV.Text = Ek.V.ToString("F2");
            lblIPhi.Text = Ei.Phi.ToString("F2"); lblINws.Text = Ei.N_WS.ToString("F2"); lblIV.Text = Ei.V.ToString("F2");
            lblJPhi.Text = Ej.Phi.ToString("F2"); lblJNws.Text = Ej.N_WS.ToString("F2"); lblJV.Text = Ej.V.ToString("F2");
        }

        int row = 0;
        private void Cal_btn_Click(object sender, EventArgs e)
        {
            string k = cboK.Text.Trim();
            double Tem;
            if (!double.TryParse(cboTemp.Text, out Tem)) Tem = 1873.0;
            if (k == string.Empty) k = "Fe";
            string i = cboI.Text.Trim();
            string j = cboJ.Text.Trim();
            display(k, i, j);
            (string phase, bool entropy, double Tem) info = (GetState(), entropy_Judge(k, i, j), Tem);
            filldata_dgV(k, i, j, info, ref row);
        }

        private void filldata_dgV(string k, string i, string j, (string state, bool entropy, double Tem) info, ref int row)
        {
            double Tem = info.Tem;
            if (k != string.Empty && i != string.Empty && j != string.Empty)
            {
                Element solv = new Element(k);
                Element solui = new Element(i);
                Element soluj = new Element(j);
                Ternary_melts wagner_ = new Ternary_melts(Tem, info.state, info.entropy);
                Binary_model miedemal = new Binary_model();
                miedemal.setState(info.state);
                miedemal.setTemperature(info.Tem);
                miedemal.setEntropy(info.entropy);

                double sij_UEM1 = wagner_.Activity_Interact_Coefficient_1st(solv, solui, soluj, miedemal.UEM1, "UEM1");
                Melt m1 = new Melt(k, i, j, Tem);
                double sij_exp = info.state == "liquid" ? m1.sji : double.NaN;

                // Convert to weight fraction scale if selected
                if (rbWeight.Checked)
                {
                    sij_UEM1 = myFunctions.first_order_mTow(sij_UEM1, new Element(j), new Element(k));
                    if (!double.IsNaN(sij_exp))
                        sij_exp = myFunctions.first_order_mTow(sij_exp, new Element(j), new Element(k));
                }

                string scaleLabel = rbWeight.Checked ? "wt%" : "mol";

                row = +dataGridView1.Rows.Add();
                dataGridView1["compositions", row].Value = k + "-" + i + "-" + j;
                dataGridView1["CalculatedResult", row].Value = sij_UEM1;
                dataGridView1["ExperimentalValue", row].Value = sij_exp;
                dataGridView1["state", row].Value = GetState();
                dataGridView1["Temperature", row].Value = info.Tem;
                dataGridView1["Remark", row].Value = scaleLabel;
                dataGridView1.Update();
            }
        }

        private bool entropy_Judge(string k, string i, string j)
        {
            List<string> s = new List<string>() { k, i, j };
            if (s.Contains("O"))
            {
                if (i == "O")
                    return constant.non_metallst.Contains(j) || constant.non_metallst.Contains(k);
                else if (j == "O")
                    return constant.non_metallst.Contains(i) || constant.non_metallst.Contains(k);
                else
                    return constant.non_metallst.Contains(j) || constant.non_metallst.Contains(i);
            }
            else if (s.Contains("H") || s.Contains("N"))
                return false;
            else
                return s.Contains("C") || s.Contains("Si") || s.Contains("B");
        }

        private void ScaleChanged(object? sender, EventArgs e)
        {
            if (rbMole.Checked)
            {
                CalculatedResult.HeaderText = "\u03B5\u1D62\u02B2 (Calc.)";
                ExperimentalValue.HeaderText = "\u03B5\u1D62\u02B2 (Exp.)";
            }
            else
            {
                CalculatedResult.HeaderText = "e\u1D62\u02B2 (Calc.)";
                ExperimentalValue.HeaderText = "e\u1D62\u02B2 (Exp.)";
            }
        }

        private void Reset_btn_Click(object sender, EventArgs e) { ResetAll(); }
    }
}
