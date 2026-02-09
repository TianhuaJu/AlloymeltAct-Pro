namespace AlloyAct_Pro.Controls
{
    partial class InfiniteDilutionPanel
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && components != null) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            inputPanel = new TableLayoutPanel();
            grpMatrix = new GroupBox(); cboMatrix = new ComboBox();
            grpSolute = new GroupBox(); cboSolute = new ComboBox();
            grpTemp = new GroupBox(); cboTemp = new ComboBox();
            grpState = new GroupBox(); rbLiquid = new RadioButton(); rbSolid = new RadioButton();
            btnCalc = new Button(); btnReset = new Button();
            dataGridView1 = new DataGridView();
            melts = new DataGridViewTextBoxColumn(); lnYi = new DataGridViewTextBoxColumn();
            exp = new DataGridViewTextBoxColumn(); Tem = new DataGridViewTextBoxColumn();
            state = new DataGridViewTextBoxColumn(); Remark = new DataGridViewTextBoxColumn();

            SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();

            inputPanel.BackColor = Color.White; inputPanel.Dock = DockStyle.Top; inputPanel.Height = 100; inputPanel.Padding = new Padding(4);
            inputPanel.ColumnCount = 5;
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            inputPanel.RowCount = 1; inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            AppTheme.SetupInputGroup(grpMatrix, cboMatrix, "Matrix (k)", new object[] { "Fe", "Ni", "Co", "Cu", "Al" });
            AppTheme.SetupInputGroup(grpSolute, cboSolute, "Solute (i)");
            AppTheme.SetupInputGroup(grpTemp, cboTemp, "Temp (K)", new object[] { "1873", "1273" });

            grpState.Text = "Phase"; grpState.Font = AppTheme.GroupBoxFont; grpState.Dock = DockStyle.Fill; grpState.Margin = new Padding(3);
            grpState.Controls.Add(rbLiquid); grpState.Controls.Add(rbSolid);
            rbLiquid.Text = "Liquid"; rbLiquid.Font = AppTheme.BodyFont; rbLiquid.Location = new Point(6, 24); rbLiquid.Size = new Size(88, 24); rbLiquid.Checked = true;
            rbSolid.Text = "Solid"; rbSolid.Font = AppTheme.BodyFont; rbSolid.Location = new Point(6, 50); rbSolid.Size = new Size(88, 24);

            var btnPanel = new TableLayoutPanel();
            btnPanel.Dock = DockStyle.Fill; btnPanel.Margin = new Padding(3); btnPanel.Padding = Padding.Empty;
            btnPanel.ColumnCount = 1; btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            btnPanel.RowCount = 2; btnPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F)); btnPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            btnCalc.Text = "Calculate"; btnCalc.Dock = DockStyle.Fill; btnCalc.Margin = new Padding(0, 0, 0, 2);
            AppTheme.StyleCalcButton(btnCalc); btnCalc.Click += Cal_btn_Click;
            btnReset.Text = "Reset"; btnReset.Dock = DockStyle.Fill; btnReset.Margin = new Padding(0, 2, 0, 0);
            AppTheme.StyleResetButton(btnReset); btnReset.Click += Reset_btn_Click;
            btnPanel.Controls.Add(btnCalc, 0, 0); btnPanel.Controls.Add(btnReset, 0, 1);

            inputPanel.Controls.Add(grpMatrix, 0, 0); inputPanel.Controls.Add(grpSolute, 1, 0);
            inputPanel.Controls.Add(grpTemp, 2, 0); inputPanel.Controls.Add(grpState, 3, 0);
            inputPanel.Controls.Add(btnPanel, 4, 0);

            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { melts, lnYi, exp, Tem, state, Remark });
            AppTheme.StyleDataGridView(dataGridView1);
            melts.HeaderText = "k-i"; melts.Name = "melts"; melts.FillWeight = 80; melts.MinimumWidth = 50;
            lnYi.HeaderText = "ln\u03B3\u1D62\u2070 (Calc.)"; lnYi.Name = "lnYi"; lnYi.FillWeight = 100; lnYi.MinimumWidth = 70; lnYi.DefaultCellStyle = new DataGridViewCellStyle { Format = "N4" };
            exp.HeaderText = "ln\u03B3\u1D62\u2070 (Exp.)"; exp.Name = "exp"; exp.FillWeight = 100; exp.MinimumWidth = 70; exp.DefaultCellStyle = new DataGridViewCellStyle { Format = "N4" };
            Tem.HeaderText = "T (K)"; Tem.Name = "Tem"; Tem.FillWeight = 60; Tem.MinimumWidth = 45;
            state.HeaderText = "State"; state.Name = "state"; state.FillWeight = 60; state.MinimumWidth = 45;
            Remark.HeaderText = "Remark"; Remark.Name = "Remark"; Remark.FillWeight = 80; Remark.MinimumWidth = 55;

            BackColor = AppTheme.ContentBg;
            Controls.Add(dataGridView1); Controls.Add(inputPanel);
            Name = "InfiniteDilutionPanel"; Size = new Size(1050, 700);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        private TableLayoutPanel inputPanel;
        private GroupBox grpMatrix, grpSolute, grpTemp, grpState;
        private ComboBox cboMatrix, cboSolute, cboTemp;
        private RadioButton rbLiquid, rbSolid;
        private Button btnCalc, btnReset;
        private DataGridView dataGridView1;
        private DataGridViewTextBoxColumn melts, lnYi, exp, Tem, state, Remark;
    }
}
