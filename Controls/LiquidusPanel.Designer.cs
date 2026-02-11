namespace AlloyAct_Pro.Controls
{
    partial class LiquidusPanel
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
            grpTemp = new GroupBox(); cboTemp = new ComboBox();
            grpState = new GroupBox(); rbLiquid = new RadioButton(); rbSolid = new RadioButton();
            btnCalc = new Button(); btnReset = new Button();
            dataGridView1 = new DataGridView();
            col_matrix = new DataGridViewTextBoxColumn();
            col_composition = new DataGridViewTextBoxColumn();
            col_Tm = new DataGridViewTextBoxColumn();
            col_deltaHf = new DataGridViewTextBoxColumn();
            col_Tliq_Wagner = new DataGridViewTextBoxColumn();
            col_Tliq_Pelton = new DataGridViewTextBoxColumn();
            col_Tliq_Elliot = new DataGridViewTextBoxColumn();
            col_deltaT = new DataGridViewTextBoxColumn();
            col_a_solvent = new DataGridViewTextBoxColumn();
            col_converged = new DataGridViewTextBoxColumn();

            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();

            // ===== inputPanel =====
            inputPanel.BackColor = Color.White;
            inputPanel.Dock = DockStyle.Top;
            inputPanel.Height = 100;
            inputPanel.Padding = new Padding(4);
            inputPanel.ColumnCount = 5;
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));   // Matrix
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));   // Composition
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));   // Temp
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));   // Phase
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));   // Buttons
            inputPanel.RowCount = 1;
            inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            AppTheme.SetupInputGroup(grpMatrix, cboMatrix, "Matrix (k)",
                new object[] { "Fe", "Ni", "Co", "Cu", "Al", "Zn", "Sn", "Pb", "Mg" });
            AppTheme.SetupInputGroupTextBox(grpComposition, txtComposition, "Composition (mole frac.)");
            AppTheme.SetupInputGroup(grpTemp, cboTemp, "Ref Temp (K)",
                new object[] { "1873", "1823", "1773", "1723", "1673", "1623", "1573", "1273" });

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
            inputPanel.Controls.Add(grpTemp, 2, 0);
            inputPanel.Controls.Add(grpState, 3, 0);
            inputPanel.Controls.Add(btnPanel, 4, 0);

            // dataGridView1
            dataGridView1.Dock = DockStyle.Fill; dataGridView1.ShowCellToolTips = true;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] {
                col_matrix, col_composition, col_Tm, col_deltaHf,
                col_Tliq_Wagner, col_Tliq_Pelton, col_Tliq_Elliot,
                col_deltaT, col_a_solvent, col_converged
            });
            AppTheme.StyleDataGridView(dataGridView1);

            col_matrix.HeaderText = "Matrix"; col_matrix.Name = "col_matrix";
            col_matrix.FillWeight = 50; col_matrix.MinimumWidth = 50;

            col_composition.HeaderText = "Composition"; col_composition.Name = "col_composition";
            col_composition.FillWeight = 130; col_composition.MinimumWidth = 80;

            col_Tm.HeaderText = "Tm (K)"; col_Tm.Name = "col_Tm";
            col_Tm.FillWeight = 60; col_Tm.MinimumWidth = 50;
            col_Tm.DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" };

            col_deltaHf.HeaderText = "\u0394Hf (kJ/mol)"; col_deltaHf.Name = "col_deltaHf";
            col_deltaHf.FillWeight = 70; col_deltaHf.MinimumWidth = 60;
            col_deltaHf.DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" };

            col_Tliq_Wagner.HeaderText = "T\u2097\u1d62\u2091 Wagner (K)"; col_Tliq_Wagner.Name = "col_Tliq_Wagner";
            col_Tliq_Wagner.FillWeight = 90; col_Tliq_Wagner.MinimumWidth = 80;
            col_Tliq_Wagner.DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" };
            col_Tliq_Wagner.ToolTipText = "Liquidus temperature (Wagner dilute solution model)";

            col_Tliq_Pelton.HeaderText = "T\u2097\u1d62\u2091 Darken (K)"; col_Tliq_Pelton.Name = "col_Tliq_Pelton";
            col_Tliq_Pelton.FillWeight = 90; col_Tliq_Pelton.MinimumWidth = 80;
            col_Tliq_Pelton.DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" };
            col_Tliq_Pelton.ToolTipText = "Liquidus temperature (Darken quadratic formalism)";

            col_Tliq_Elliot.HeaderText = "T\u2097\u1d62\u2091 Elliot (K)"; col_Tliq_Elliot.Name = "col_Tliq_Elliot";
            col_Tliq_Elliot.FillWeight = 90; col_Tliq_Elliot.MinimumWidth = 80;
            col_Tliq_Elliot.DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" };
            col_Tliq_Elliot.ToolTipText = "Liquidus temperature (Elliot model with 2nd-order corrections)";

            col_deltaT.HeaderText = "\u0394T (K)"; col_deltaT.Name = "col_deltaT";
            col_deltaT.FillWeight = 60; col_deltaT.MinimumWidth = 50;
            col_deltaT.DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" };
            col_deltaT.ToolTipText = "Freezing point depression: Tm - T_liquidus (Wagner)";

            col_a_solvent.HeaderText = "a\u2096"; col_a_solvent.Name = "col_a_solvent";
            col_a_solvent.FillWeight = 60; col_a_solvent.MinimumWidth = 50;
            col_a_solvent.DefaultCellStyle = new DataGridViewCellStyle { Format = "N4" };
            col_a_solvent.ToolTipText = "Solvent activity at liquidus temperature (Wagner)";

            col_converged.HeaderText = "Status"; col_converged.Name = "col_converged";
            col_converged.FillWeight = 50; col_converged.MinimumWidth = 45;

            BackColor = AppTheme.ContentBg;
            Controls.Add(dataGridView1);
            Controls.Add(inputPanel);
            Name = "LiquidusPanel";
            Size = new Size(1050, 700);

            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        private TableLayoutPanel inputPanel;
        private GroupBox grpMatrix, grpComposition, grpTemp, grpState;
        private ComboBox cboMatrix, cboTemp;
        private TextBox txtComposition;
        private RadioButton rbLiquid, rbSolid;
        private Button btnCalc, btnReset;
        private DataGridView dataGridView1;
        private DataGridViewTextBoxColumn col_matrix, col_composition, col_Tm, col_deltaHf;
        private DataGridViewTextBoxColumn col_Tliq_Wagner, col_Tliq_Pelton, col_Tliq_Elliot;
        private DataGridViewTextBoxColumn col_deltaT, col_a_solvent, col_converged;
    }
}
