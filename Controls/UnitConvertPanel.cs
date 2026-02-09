namespace AlloyAct_Pro.Controls
{
    public partial class UnitConvertPanel : UserControl
    {
        public string PageTitle => "Unit Conversion";

        public UnitConvertPanel()
        {
            InitializeComponent();
        }

        public void ExportToExcel() { }
        public void ResetAll()
        {
            txtInput.Clear(); txtResult.Clear();
            cboK.Text = string.Empty; cboI.Text = string.Empty; cboJ.Text = string.Empty;
        }

        private void Convert_Click(object sender, EventArgs e)
        {
            string k = cboK.Text.Trim();
            string i = cboI.Text.Trim();
            string j = cboJ.Text.Trim();
            string originalData = txtInput.Text.Trim();
            if (k != string.Empty && i != string.Empty && j != string.Empty)
            {
                double orgn_data;
                if (originalData != string.Empty && double.TryParse(originalData, out orgn_data))
                {
                    if (rbWeight.Checked)
                        txtResult.Text = myFunctions.first_order_w2m(orgn_data, new Element(j), new Element(k)).ToString();
                    else
                        txtResult.Text = myFunctions.first_order_mTow(orgn_data, new Element(j), new Element(k)).ToString();
                }
                else { MessageBox.Show("Check the interaction coefficient value"); }
            }
            else { MessageBox.Show("Check element symbols"); }
        }

        private void Reset_Click(object sender, EventArgs e) { ResetAll(); }
    }
}
