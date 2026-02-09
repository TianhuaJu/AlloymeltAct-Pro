namespace AlloyAct_Pro.Controls
{
    partial class ActivityPanel
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            inputPanel = new TableLayoutPanel();
            grpMatrix = new GroupBox(); cboMatrix = new ComboBox();
            grpComposition = new GroupBox(); txtComposition = new TextBox();
            grpSolute = new GroupBox(); cboSolute = new ComboBox();
            grpTemp = new GroupBox(); cboTemp = new ComboBox();
            grpState = new GroupBox(); rbLiquid = new RadioButton(); rbSolid = new RadioButton();
            btnCalc = new Button(); btnReset = new Button();
            dataGridView1 = new DataGridView();
            k_name = new DataGridViewTextBoxColumn();
            Melt_composition = new DataGridViewTextBoxColumn();
            solute_i = new DataGridViewTextBoxColumn();
            xi = new DataGridViewTextBoxColumn();
            activity = new DataGridViewTextBoxColumn();
            ai_elloit = new DataGridViewTextBoxColumn();
            ai_wagner = new DataGridViewTextBoxColumn();
            Tem = new DataGridViewTextBoxColumn();
            state = new DataGridViewTextBoxColumn();

            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();

            // ===== inputPanel =====
            inputPanel.BackColor = Color.White;
            inputPanel.Dock = DockStyle.Top;
            inputPanel.Height = 100;
            inputPanel.Padding = new Padding(4);
            inputPanel.ColumnCount = 6;
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));   // Matrix
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));   // Composition (wide TextBox)
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));   // Solute
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));   // Temp
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));   // Phase
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));   // Buttons
            inputPanel.RowCount = 1;
            inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            AppTheme.SetupInputGroup(grpMatrix, cboMatrix, "Matrix (k)", new object[] { "Fe", "Ni", "Co", "Cu", "Al" });
            AppTheme.SetupInputGroupTextBox(grpComposition, txtComposition, "Composition");
            AppTheme.SetupInputGroup(grpSolute, cboSolute, "Solute (i)");
            cboSolute.Click += cboSolute_Click;
            AppTheme.SetupInputGroup(grpTemp, cboTemp, "Temp (K)", new object[] { "1873", "1273" });

            grpState.Text = "Phase"; grpState.Font = AppTheme.GroupBoxFont; grpState.Dock = DockStyle.Fill; grpState.Margin = new Padding(3);
            grpState.Controls.Add(rbLiquid); grpState.Controls.Add(rbSolid);
            rbLiquid.Text = "Liquid"; rbLiquid.Font = AppTheme.BodyFont; rbLiquid.Location = new Point(6, 24); rbLiquid.Size = new Size(88, 24); rbLiquid.Checked = true;
            rbSolid.Text = "Solid"; rbSolid.Font = AppTheme.BodyFont; rbSolid.Location = new Point(6, 50); rbSolid.Size = new Size(88, 24);

            var btnPanel = new TableLayoutPanel();
            btnPanel.Dock = DockStyle.Fill; btnPanel.Margin = new Padding(3); btnPanel.Padding = Padding.Empty;
            btnPanel.ColumnCount = 1; btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            btnPanel.RowCount = 2;
            btnPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            btnPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            btnCalc.Text = "Calculate"; btnCalc.Dock = DockStyle.Fill; btnCalc.Margin = new Padding(0, 0, 0, 2);
            AppTheme.StyleCalcButton(btnCalc); btnCalc.Click += Cal_btn_Click;
            btnReset.Text = "Reset"; btnReset.Dock = DockStyle.Fill; btnReset.Margin = new Padding(0, 2, 0, 0);
            AppTheme.StyleResetButton(btnReset); btnReset.Click += Reset_btn_Click;
            btnPanel.Controls.Add(btnCalc, 0, 0); btnPanel.Controls.Add(btnReset, 0, 1);

            inputPanel.Controls.Add(grpMatrix, 0, 0);
            inputPanel.Controls.Add(grpComposition, 1, 0);
            inputPanel.Controls.Add(grpSolute, 2, 0);
            inputPanel.Controls.Add(grpTemp, 3, 0);
            inputPanel.Controls.Add(grpState, 4, 0);
            inputPanel.Controls.Add(btnPanel, 5, 0);

            // dataGridView1
            dataGridView1.Dock = DockStyle.Fill; dataGridView1.ShowCellToolTips = true;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { k_name, Melt_composition, solute_i, xi, ai_wagner, activity, ai_elloit, Tem, state });
            AppTheme.StyleDataGridView(dataGridView1);

            k_name.HeaderText = "Matrix"; k_name.Name = "k_name"; k_name.FillWeight = 50; k_name.MinimumWidth = 50;
            Melt_composition.HeaderText = "Composition"; Melt_composition.Name = "Melt_composition"; Melt_composition.FillWeight = 130; Melt_composition.MinimumWidth = 80;
            solute_i.HeaderText = "Solute"; solute_i.Name = "solute_i"; solute_i.FillWeight = 50; solute_i.MinimumWidth = 50;
            xi.HeaderText = "x\u1D62"; xi.Name = "xi"; xi.FillWeight = 60; xi.MinimumWidth = 40; xi.DefaultCellStyle = new DataGridViewCellStyle { Format = "N4" };
            activity.HeaderText = "a\u1D62 (Darken)"; activity.Name = "activity"; activity.FillWeight = 90; activity.MinimumWidth = 70; activity.DefaultCellStyle = new DataGridViewCellStyle { Format = "N4" };
            activity.ToolTipText = "Darken quadratic formalism";
            ai_elloit.HeaderText = "a\u1D62 (Elliot)"; ai_elloit.Name = "ai_elloit"; ai_elloit.FillWeight = 90; ai_elloit.MinimumWidth = 70; ai_elloit.DefaultCellStyle = new DataGridViewCellStyle { Format = "N4" };
            ai_elloit.ToolTipText = "Wagner model + 2nd-order interaction coefficients (\u03C1, experimental)";
            ai_wagner.HeaderText = "a\u1D62 (Wagner)"; ai_wagner.Name = "ai_wagner"; ai_wagner.FillWeight = 90; ai_wagner.MinimumWidth = 70; ai_wagner.DefaultCellStyle = new DataGridViewCellStyle { Format = "N4" };
            ai_wagner.ToolTipText = "Wagner dilute solution model (1st-order interaction coefficients)";
            Tem.HeaderText = "T (K)"; Tem.Name = "Tem"; Tem.FillWeight = 50; Tem.MinimumWidth = 45;
            state.HeaderText = "State"; state.Name = "state"; state.FillWeight = 50; state.MinimumWidth = 45;

            BackColor = AppTheme.ContentBg;
            Controls.Add(dataGridView1);
            Controls.Add(inputPanel);
            Name = "ActivityPanel";
            Size = new Size(1050, 700);

            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        private TableLayoutPanel inputPanel;
        private GroupBox grpMatrix, grpComposition, grpSolute, grpTemp, grpState;
        private ComboBox cboMatrix, cboSolute, cboTemp;
        private TextBox txtComposition;
        private RadioButton rbLiquid, rbSolid;
        private Button btnCalc, btnReset;
        private DataGridView dataGridView1;
        private DataGridViewTextBoxColumn k_name, Melt_composition, solute_i, xi, activity, ai_elloit, ai_wagner, Tem, state;
    }
}
