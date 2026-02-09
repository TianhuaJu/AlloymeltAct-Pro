namespace AlloyAct_Pro.Controls
{
    partial class SecondOrderPanel
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && components != null) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            inputPanel = new TableLayoutPanel(); miedemaTable = new TableLayoutPanel();
            grpM = new GroupBox(); cboM = new ComboBox();
            grpI = new GroupBox(); cboI = new ComboBox();
            grpJ = new GroupBox(); cboJ = new ComboBox();
            grpK = new GroupBox(); cboK = new ComboBox();
            grpTemp = new GroupBox(); cboTemp = new ComboBox();
            grpState = new GroupBox(); rbLiquid = new RadioButton(); rbSolid = new RadioButton();
            btnCalc = new Button(); btnReset = new Button();
            cardM = new Panel(); cardI = new Panel(); cardJ = new Panel(); cardK2 = new Panel();
            lblMTitle = new Label(); lblMPhi = new Label(); lblMNws = new Label(); lblMV = new Label(); lblMPhiH = new Label(); lblMNwsH = new Label(); lblMVH = new Label();
            lblITitle = new Label(); lblIPhi = new Label(); lblINws = new Label(); lblIV = new Label(); lblIPhiH = new Label(); lblINwsH = new Label(); lblIVH = new Label();
            lblJTitle = new Label(); lblJPhi = new Label(); lblJNws = new Label(); lblJV = new Label(); lblJPhiH = new Label(); lblJNwsH = new Label(); lblJVH = new Label();
            lblKTitle = new Label(); lblKPhi = new Label(); lblKNws = new Label(); lblKV = new Label(); lblKPhiH = new Label(); lblKNwsH = new Label(); lblKVH = new Label();
            dataGridView1 = new DataGridView();
            compositions = new DataGridViewTextBoxColumn();
            ri_ii = new DataGridViewTextBoxColumn(); ri_ij = new DataGridViewTextBoxColumn();
            ri_jj = new DataGridViewTextBoxColumn(); ri_jk = new DataGridViewTextBoxColumn();
            state = new DataGridViewTextBoxColumn(); Temperature = new DataGridViewTextBoxColumn();

            SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();

            inputPanel.BackColor = Color.White; inputPanel.Dock = DockStyle.Top; inputPanel.Height = 100; inputPanel.Padding = new Padding(4);
            inputPanel.ColumnCount = 7;
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26F));
            inputPanel.RowCount = 1; inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            AppTheme.SetupInputGroup(grpM, cboM, "Matrix (m)", new object[] { "Fe", "Ni", "Co", "Cu", "Al" });
            AppTheme.SetupInputGroup(grpI, cboI, "Solute (i)", new object[] { "Al", "Si", "C", "Ti", "Mn", "V" });
            AppTheme.SetupInputGroup(grpJ, cboJ, "Solute (j)", new object[] { "Al", "Si", "V", "Ti", "Cr", "Mn", "C", "B" });
            AppTheme.SetupInputGroup(grpK, cboK, "Solute (k)", new object[] { "Al", "Si", "V", "Ti", "Cr", "Mn", "C", "B" });
            AppTheme.SetupInputGroup(grpTemp, cboTemp, "Temp (K)", new object[] { "1873" });

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

            inputPanel.Controls.Add(grpM, 0, 0); inputPanel.Controls.Add(grpI, 1, 0);
            inputPanel.Controls.Add(grpJ, 2, 0); inputPanel.Controls.Add(grpK, 3, 0);
            inputPanel.Controls.Add(grpTemp, 4, 0); inputPanel.Controls.Add(grpState, 5, 0);
            inputPanel.Controls.Add(btnPanel, 6, 0);

            miedemaTable.BackColor = Color.White; miedemaTable.Dock = DockStyle.Top; miedemaTable.Height = 84; miedemaTable.Padding = new Padding(4);
            miedemaTable.ColumnCount = 4;
            miedemaTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            miedemaTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            miedemaTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            miedemaTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            miedemaTable.RowCount = 1; miedemaTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            SetupCard(cardM, "Matrix (m)", lblMTitle, lblMPhiH, lblMPhi, lblMNwsH, lblMNws, lblMVH, lblMV);
            SetupCard(cardI, "Solute (i)", lblITitle, lblIPhiH, lblIPhi, lblINwsH, lblINws, lblIVH, lblIV);
            SetupCard(cardJ, "Solute (j)", lblJTitle, lblJPhiH, lblJPhi, lblJNwsH, lblJNws, lblJVH, lblJV);
            SetupCard(cardK2, "Solute (k)", lblKTitle, lblKPhiH, lblKPhi, lblKNwsH, lblKNws, lblKVH, lblKV);
            miedemaTable.Controls.Add(cardM, 0, 0); miedemaTable.Controls.Add(cardI, 1, 0);
            miedemaTable.Controls.Add(cardJ, 2, 0); miedemaTable.Controls.Add(cardK2, 3, 0);

            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { compositions, ri_ii, ri_ij, ri_jj, ri_jk, Temperature, state });
            AppTheme.StyleDataGridView(dataGridView1);
            compositions.HeaderText = "m-i-j-k"; compositions.Name = "compositions"; compositions.FillWeight = 100; compositions.MinimumWidth = 60;
            ri_ii.HeaderText = "\u03C1\u1D62\u2071\u2071"; ri_ii.Name = "ri_ii"; ri_ii.FillWeight = 80; ri_ii.MinimumWidth = 55; ri_ii.DefaultCellStyle = new DataGridViewCellStyle { Format = "N4" };
            ri_ij.HeaderText = "\u03C1\u1D62\u2071\u02B2"; ri_ij.Name = "ri_ij"; ri_ij.FillWeight = 80; ri_ij.MinimumWidth = 55; ri_ij.DefaultCellStyle = new DataGridViewCellStyle { Format = "N4" };
            ri_jj.HeaderText = "\u03C1\u1D62\u02B2\u02B2"; ri_jj.Name = "ri_jj"; ri_jj.FillWeight = 80; ri_jj.MinimumWidth = 55; ri_jj.DefaultCellStyle = new DataGridViewCellStyle { Format = "N4" };
            ri_jk.HeaderText = "\u03C1\u1D62\u02B2\u1D4F"; ri_jk.Name = "ri_jk"; ri_jk.FillWeight = 80; ri_jk.MinimumWidth = 55; ri_jk.DefaultCellStyle = new DataGridViewCellStyle { Format = "N4" };
            Temperature.HeaderText = "T (K)"; Temperature.Name = "Temperature"; Temperature.FillWeight = 60; Temperature.MinimumWidth = 45;
            state.HeaderText = "State"; state.Name = "state"; state.FillWeight = 60; state.MinimumWidth = 45;

            BackColor = AppTheme.ContentBg;
            Controls.Add(dataGridView1); Controls.Add(miedemaTable); Controls.Add(inputPanel);
            Name = "SecondOrderPanel"; Size = new Size(1050, 700);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        private void SetupCard(Panel card, string title, Label lblTitle, Label lblPhiH, Label lblPhi, Label lblNwsH, Label lblNws, Label lblVH, Label lblV)
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
            var mainSize = g.MeasureString(main, mainFont, 200, StringFormat.GenericTypographic);
            g.DrawString(main, mainFont, brush, x, baseY, StringFormat.GenericTypographic);
            x += mainSize.Width;
            if (!string.IsNullOrEmpty(sub))
            {
                float subY = baseY + mainSize.Height * 0.35f;
                g.DrawString(sub, smallFont, brush, x, subY, StringFormat.GenericTypographic);
                x += g.MeasureString(sub, smallFont, 200, StringFormat.GenericTypographic).Width;
            }
            if (!string.IsNullOrEmpty(sup))
            {
                float supY = baseY - mainSize.Height * 0.15f;
                g.DrawString(sup, smallFont, brush, x, supY, StringFormat.GenericTypographic);
                x += g.MeasureString(sup, smallFont, 200, StringFormat.GenericTypographic).Width;
            }
            if (!string.IsNullOrEmpty(suffix))
            {
                g.DrawString(" " + suffix, mainFont, brush, x, baseY, StringFormat.GenericTypographic);
            }
        }

        private TableLayoutPanel inputPanel, miedemaTable;
        private Panel cardM, cardI, cardJ, cardK2;
        private GroupBox grpM, grpI, grpJ, grpK, grpTemp, grpState;
        private ComboBox cboM, cboI, cboJ, cboK, cboTemp;
        private RadioButton rbLiquid, rbSolid;
        private Button btnCalc, btnReset;
        private Label lblMTitle, lblMPhi, lblMNws, lblMV, lblMPhiH, lblMNwsH, lblMVH;
        private Label lblITitle, lblIPhi, lblINws, lblIV, lblIPhiH, lblINwsH, lblIVH;
        private Label lblJTitle, lblJPhi, lblJNws, lblJV, lblJPhiH, lblJNwsH, lblJVH;
        private Label lblKTitle, lblKPhi, lblKNws, lblKV, lblKPhiH, lblKNwsH, lblKVH;
        private DataGridView dataGridView1;
        private DataGridViewTextBoxColumn compositions, ri_ii, ri_ij, ri_jj, ri_jk, state, Temperature;
    }
}
