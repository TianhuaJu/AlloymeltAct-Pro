namespace AlloyAct_Pro.Controls
{
    partial class InteractionCoefficientPanel
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && components != null) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            inputPanel = new TableLayoutPanel(); miedemaTable = new TableLayoutPanel();
            grpK = new GroupBox(); cboK = new ComboBox();
            grpI = new GroupBox(); cboI = new ComboBox();
            grpJ = new GroupBox(); cboJ = new ComboBox();
            grpTemp = new GroupBox(); cboTemp = new ComboBox();
            grpState = new GroupBox(); rbLiquid = new RadioButton(); rbSolid = new RadioButton();
            grpScale = new GroupBox(); rbMole = new RadioButton(); rbWeight = new RadioButton();
            btnCalc = new Button(); btnReset = new Button();
            cardK = new Panel(); cardI = new Panel(); cardJ = new Panel();
            lblKTitle = new Label(); lblKPhi = new Label(); lblKNws = new Label(); lblKV = new Label();
            lblITitle = new Label(); lblIPhi = new Label(); lblINws = new Label(); lblIV = new Label();
            lblJTitle = new Label(); lblJPhi = new Label(); lblJNws = new Label(); lblJV = new Label();
            lblKPhiH = new Label(); lblKNwsH = new Label(); lblKVH = new Label();
            lblIPhiH = new Label(); lblINwsH = new Label(); lblIVH = new Label();
            lblJPhiH = new Label(); lblJNwsH = new Label(); lblJVH = new Label();
            dataGridView1 = new DataGridView();
            compositions = new DataGridViewTextBoxColumn(); CalculatedResult = new DataGridViewTextBoxColumn();
            ExperimentalValue = new DataGridViewTextBoxColumn(); Temperature = new DataGridViewTextBoxColumn();
            state = new DataGridViewTextBoxColumn(); Remark = new DataGridViewTextBoxColumn();

            SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();

            // ===== inputPanel =====
            inputPanel.BackColor = Color.White; inputPanel.Dock = DockStyle.Top; inputPanel.Height = 100; inputPanel.Padding = new Padding(4);
            inputPanel.ColumnCount = 7;
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            inputPanel.RowCount = 1; inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            AppTheme.SetupInputGroup(grpK, cboK, "Matrix (k)", new object[] { "Fe", "Ni", "Co", "Cu", "Al" });
            AppTheme.SetupInputGroup(grpI, cboI, "Solute (i)", new object[] { "Al", "Si", "C", "Ti", "Mn", "V" });
            AppTheme.SetupInputGroup(grpJ, cboJ, "Solute (j)", new object[] { "Al", "Si", "V", "Ti", "Cr", "Mn", "C", "B" });
            AppTheme.SetupInputGroup(grpTemp, cboTemp, "Temp (K)", new object[] { "1873" });

            grpState.Text = "Phase"; grpState.Font = AppTheme.GroupBoxFont; grpState.Dock = DockStyle.Fill; grpState.Margin = new Padding(3);
            grpState.Controls.Add(rbLiquid); grpState.Controls.Add(rbSolid);
            rbLiquid.Text = "Liquid"; rbLiquid.Font = AppTheme.BodyFont; rbLiquid.Location = new Point(6, 24); rbLiquid.Size = new Size(88, 24); rbLiquid.Checked = true;
            rbSolid.Text = "Solid"; rbSolid.Font = AppTheme.BodyFont; rbSolid.Location = new Point(6, 50); rbSolid.Size = new Size(88, 24);

            grpScale.Text = "Scale"; grpScale.Font = AppTheme.GroupBoxFont; grpScale.Dock = DockStyle.Fill; grpScale.Margin = new Padding(3);
            grpScale.Controls.Add(rbMole); grpScale.Controls.Add(rbWeight);
            rbMole.Text = "Mole Fraction"; rbMole.Font = AppTheme.BodyFont; rbMole.Location = new Point(6, 22); rbMole.Size = new Size(120, 24); rbMole.Checked = true;
            rbWeight.Text = "Weight %"; rbWeight.Font = AppTheme.BodyFont; rbWeight.Location = new Point(6, 48); rbWeight.Size = new Size(120, 24);
            rbMole.CheckedChanged += ScaleChanged;
            rbWeight.CheckedChanged += ScaleChanged;

            var btnPanel = new TableLayoutPanel();
            btnPanel.Dock = DockStyle.Fill; btnPanel.Margin = new Padding(3); btnPanel.Padding = Padding.Empty;
            btnPanel.ColumnCount = 1; btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            btnPanel.RowCount = 2; btnPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F)); btnPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            btnCalc.Text = "Calculate"; btnCalc.Dock = DockStyle.Fill; btnCalc.Margin = new Padding(0, 0, 0, 2);
            AppTheme.StyleCalcButton(btnCalc); btnCalc.Click += Cal_btn_Click;
            btnReset.Text = "Reset"; btnReset.Dock = DockStyle.Fill; btnReset.Margin = new Padding(0, 2, 0, 0);
            AppTheme.StyleResetButton(btnReset); btnReset.Click += Reset_btn_Click;
            btnPanel.Controls.Add(btnCalc, 0, 0); btnPanel.Controls.Add(btnReset, 0, 1);

            inputPanel.Controls.Add(grpK, 0, 0); inputPanel.Controls.Add(grpI, 1, 0);
            inputPanel.Controls.Add(grpJ, 2, 0); inputPanel.Controls.Add(grpTemp, 3, 0);
            inputPanel.Controls.Add(grpState, 4, 0); inputPanel.Controls.Add(grpScale, 5, 0);
            inputPanel.Controls.Add(btnPanel, 6, 0);

            // ===== miedemaTable =====
            miedemaTable.BackColor = Color.White; miedemaTable.Dock = DockStyle.Top; miedemaTable.Height = 84; miedemaTable.Padding = new Padding(4);
            miedemaTable.ColumnCount = 3;
            miedemaTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            miedemaTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            miedemaTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            miedemaTable.RowCount = 1; miedemaTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            SetupMiedemaCard(cardK, "Matrix (k)", lblKTitle, lblKPhiH, lblKPhi, lblKNwsH, lblKNws, lblKVH, lblKV);
            SetupMiedemaCard(cardI, "Solute (i)", lblITitle, lblIPhiH, lblIPhi, lblINwsH, lblINws, lblIVH, lblIV);
            SetupMiedemaCard(cardJ, "Solute (j)", lblJTitle, lblJPhiH, lblJPhi, lblJNwsH, lblJNws, lblJVH, lblJV);
            miedemaTable.Controls.Add(cardK, 0, 0); miedemaTable.Controls.Add(cardI, 1, 0); miedemaTable.Controls.Add(cardJ, 2, 0);

            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { compositions, CalculatedResult, ExperimentalValue, Temperature, state, Remark });
            AppTheme.StyleDataGridView(dataGridView1);
            compositions.HeaderText = "k-i-j"; compositions.Name = "compositions"; compositions.FillWeight = 80; compositions.MinimumWidth = 60;
            CalculatedResult.HeaderText = "\u03B5\u1D62\u02B2 (Calc.)"; CalculatedResult.Name = "CalculatedResult"; CalculatedResult.FillWeight = 100; CalculatedResult.MinimumWidth = 70; CalculatedResult.DefaultCellStyle = new DataGridViewCellStyle { Format = "N4" };
            ExperimentalValue.HeaderText = "\u03B5\u1D62\u02B2 (Exp.)"; ExperimentalValue.Name = "ExperimentalValue"; ExperimentalValue.FillWeight = 100; ExperimentalValue.MinimumWidth = 70; ExperimentalValue.DefaultCellStyle = new DataGridViewCellStyle { Format = "N4" };
            Temperature.HeaderText = "T (K)"; Temperature.Name = "Temperature"; Temperature.FillWeight = 60; Temperature.MinimumWidth = 45;
            state.HeaderText = "State"; state.Name = "state"; state.FillWeight = 60; state.MinimumWidth = 45;
            Remark.HeaderText = "Remark"; Remark.Name = "Remark"; Remark.FillWeight = 80; Remark.MinimumWidth = 55;

            BackColor = AppTheme.ContentBg;
            Controls.Add(dataGridView1); Controls.Add(miedemaTable); Controls.Add(inputPanel);
            Name = "InteractionCoefficientPanel"; Size = new Size(1050, 700);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        private void SetupMiedemaCard(Panel card, string title, Label lblTitle, Label lblPhiH, Label lblPhi, Label lblNwsH, Label lblNws, Label lblVH, Label lblV)
        {
            card.Dock = DockStyle.Fill; card.Margin = new Padding(4); card.BackColor = Color.White; card.BorderStyle = BorderStyle.FixedSingle;
            var tbl = new TableLayoutPanel();
            tbl.Dock = DockStyle.Fill; tbl.Padding = new Padding(6, 2, 4, 2);
            tbl.ColumnCount = 4; tbl.RowCount = 3;
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            var hdrFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            var valFont = new Font("Microsoft YaHei UI", 9F);
            var hdrColor = Color.FromArgb(100, 100, 100);
            lblTitle.Text = title; lblTitle.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold); lblTitle.ForeColor = AppTheme.SidebarActiveBg; lblTitle.AutoSize = true; lblTitle.Margin = Padding.Empty;
            lblPhiH.Text = "φ ="; lblPhiH.Font = hdrFont; lblPhiH.ForeColor = hdrColor; lblPhiH.AutoSize = true; lblPhiH.Margin = new Padding(0, 2, 2, 0);
            lblPhi.Font = valFont; lblPhi.AutoSize = true; lblPhi.Margin = new Padding(0, 2, 8, 0);
            lblNwsH.Text = "  "; lblNwsH.Font = hdrFont; lblNwsH.ForeColor = hdrColor; lblNwsH.AutoSize = false; lblNwsH.Size = new Size(80, 20); lblNwsH.Margin = new Padding(0, 2, 2, 0);
            lblNwsH.Paint += (s, pe) => DrawMiedemaLabel(pe.Graphics, "n", "WS", "1/3", "=", hdrFont, hdrColor);
            lblNws.Font = valFont; lblNws.AutoSize = true; lblNws.Margin = new Padding(0, 2, 0, 0);
            lblVH.Text = "  "; lblVH.Font = hdrFont; lblVH.ForeColor = hdrColor; lblVH.AutoSize = false; lblVH.Size = new Size(65, 20); lblVH.Margin = new Padding(0, 2, 2, 0);
            lblVH.Paint += (s, pe) => DrawMiedemaLabel(pe.Graphics, "V", "", "2/3", "=", hdrFont, hdrColor);
            lblV.Font = valFont; lblV.AutoSize = true; lblV.Margin = new Padding(0, 2, 0, 0);
            tbl.Controls.Add(lblTitle, 0, 0); tbl.SetColumnSpan(lblTitle, 4);
            tbl.Controls.Add(lblPhiH, 0, 1); tbl.Controls.Add(lblPhi, 1, 1);
            tbl.Controls.Add(lblNwsH, 2, 1); tbl.Controls.Add(lblNws, 3, 1);
            tbl.Controls.Add(lblVH, 0, 2); tbl.Controls.Add(lblV, 1, 2);
            card.Controls.Add(tbl);
        }

        /// <summary>Draw label with subscript base and superscript exponent, e.g. n_{WS}^{1/3} =</summary>
        private void DrawMiedemaLabel(Graphics g, string main, string sub, string sup, string suffix, Font baseFont, Color color)
        {
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using var mainFont = new Font(baseFont.FontFamily, baseFont.Size, baseFont.Style);
            using var smallFont = new Font(baseFont.FontFamily, baseFont.Size * 0.7f, baseFont.Style);
            using var brush = new SolidBrush(color);
            float x = 0;
            float baseY = 2f;
            // Main symbol
            var mainSize = g.MeasureString(main, mainFont, 200, StringFormat.GenericTypographic);
            g.DrawString(main, mainFont, brush, x, baseY, StringFormat.GenericTypographic);
            x += mainSize.Width;
            // Subscript
            if (!string.IsNullOrEmpty(sub))
            {
                float subY = baseY + mainSize.Height * 0.35f;
                g.DrawString(sub, smallFont, brush, x, subY, StringFormat.GenericTypographic);
                x += g.MeasureString(sub, smallFont, 200, StringFormat.GenericTypographic).Width;
            }
            // Superscript
            if (!string.IsNullOrEmpty(sup))
            {
                float supY = baseY - mainSize.Height * 0.15f;
                g.DrawString(sup, smallFont, brush, x, supY, StringFormat.GenericTypographic);
                x += g.MeasureString(sup, smallFont, 200, StringFormat.GenericTypographic).Width;
            }
            // Suffix (= sign)
            if (!string.IsNullOrEmpty(suffix))
            {
                g.DrawString(" " + suffix, mainFont, brush, x, baseY, StringFormat.GenericTypographic);
            }
        }

        private TableLayoutPanel inputPanel, miedemaTable;
        private Panel cardK, cardI, cardJ;
        private GroupBox grpK, grpI, grpJ, grpTemp, grpState, grpScale;
        private ComboBox cboK, cboI, cboJ, cboTemp;
        private RadioButton rbLiquid, rbSolid, rbMole, rbWeight;
        private Button btnCalc, btnReset;
        private Label lblKTitle, lblKPhi, lblKNws, lblKV, lblKPhiH, lblKNwsH, lblKVH;
        private Label lblITitle, lblIPhi, lblINws, lblIV, lblIPhiH, lblINwsH, lblIVH;
        private Label lblJTitle, lblJPhi, lblJNws, lblJV, lblJPhiH, lblJNwsH, lblJVH;
        private DataGridView dataGridView1;
        private DataGridViewTextBoxColumn compositions, CalculatedResult, ExperimentalValue, Temperature, state, Remark;
    }
}
