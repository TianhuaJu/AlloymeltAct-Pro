namespace AlloyAct_Pro.Controls
{
    partial class UnitConvertPanel
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && components != null) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            inputTable = new TableLayoutPanel();
            grpK = new GroupBox(); cboK = new ComboBox();
            grpI = new GroupBox(); cboI = new ComboBox();
            grpJ = new GroupBox(); cboJ = new ComboBox();
            grpMode = new GroupBox();
            rbWeight = new RadioButton(); rbAtom = new RadioButton();
            grpConvert = new GroupBox();
            convertTable = new TableLayoutPanel();
            lblInput = new Label(); txtInput = new TextBox();
            lblResult = new Label(); txtResult = new TextBox();
            btnConvert = new Button(); btnReset = new Button();

            SuspendLayout();

            // ===== Top layout =====
            inputTable.BackColor = Color.White;
            inputTable.Dock = DockStyle.Top;
            inputTable.Height = 310;
            inputTable.Padding = new Padding(8);
            inputTable.ColumnCount = 5;
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));  // K
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));  // I
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));  // J
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));  // Mode
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));  // Buttons
            inputTable.RowCount = 3;
            inputTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));   // Row 0: element selectors + mode + buttons
            inputTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));   // Row 1: values
            inputTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // Row 2: spacer

            // Element selectors using SetupInputGroup
            AppTheme.SetupInputGroup(grpK, cboK, "Matrix (k)", new object[] { "Fe", "Ni", "Co", "Cu", "Al" });
            AppTheme.SetupInputGroup(grpI, cboI, "Solute (i)");
            AppTheme.SetupInputGroup(grpJ, cboJ, "Solute (j)");

            // Mode group
            grpMode.Text = "Conversion Mode"; grpMode.Font = AppTheme.GroupBoxFont;
            grpMode.Dock = DockStyle.Fill; grpMode.Margin = new Padding(3);
            grpMode.Controls.Add(rbWeight); grpMode.Controls.Add(rbAtom);
            rbWeight.Text = "Wt% → Atom"; rbWeight.Font = AppTheme.BodyFont;
            rbWeight.Location = new Point(6, 24); rbWeight.Size = new Size(180, 24); rbWeight.Checked = true;
            rbAtom.Text = "Atom → Wt%"; rbAtom.Font = AppTheme.BodyFont;
            rbAtom.Location = new Point(6, 50); rbAtom.Size = new Size(180, 24);

            // Buttons in nested panel
            var btnPanel = new TableLayoutPanel();
            btnPanel.Dock = DockStyle.Fill; btnPanel.Margin = new Padding(3); btnPanel.Padding = Padding.Empty;
            btnPanel.ColumnCount = 1; btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            btnPanel.RowCount = 2;
            btnPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            btnPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            btnConvert.Text = "Convert"; btnConvert.Dock = DockStyle.Fill; btnConvert.Margin = new Padding(0, 0, 0, 2);
            AppTheme.StyleCalcButton(btnConvert); btnConvert.Click += Convert_Click;
            btnReset.Text = "Reset"; btnReset.Dock = DockStyle.Fill; btnReset.Margin = new Padding(0, 2, 0, 0);
            AppTheme.StyleResetButton(btnReset); btnReset.Click += Reset_Click;
            btnPanel.Controls.Add(btnConvert, 0, 0); btnPanel.Controls.Add(btnReset, 0, 1);

            // Values group spanning 5 columns
            grpConvert.Text = "Values"; grpConvert.Font = AppTheme.GroupBoxFont;
            grpConvert.Dock = DockStyle.Fill; grpConvert.Margin = new Padding(3);
            grpConvert.Controls.Add(convertTable);

            convertTable.Dock = DockStyle.Fill;
            convertTable.ColumnCount = 4;
            convertTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
            convertTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            convertTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
            convertTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            convertTable.RowCount = 1;
            convertTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            lblInput.Text = "Input:"; lblInput.Font = AppTheme.BodyFont; lblInput.Dock = DockStyle.Fill;
            lblInput.TextAlign = ContentAlignment.MiddleRight; lblInput.Margin = new Padding(3);
            txtInput.Font = AppTheme.InputFont; txtInput.Dock = DockStyle.Fill; txtInput.Margin = new Padding(3, 14, 3, 14);
            lblResult.Text = "Result:"; lblResult.Font = AppTheme.BodyFont; lblResult.Dock = DockStyle.Fill;
            lblResult.TextAlign = ContentAlignment.MiddleRight; lblResult.Margin = new Padding(3);
            txtResult.Font = AppTheme.InputFont; txtResult.Dock = DockStyle.Fill; txtResult.Margin = new Padding(3, 14, 3, 14);
            txtResult.ReadOnly = true; txtResult.BackColor = Color.FromArgb(236, 240, 241);

            convertTable.Controls.Add(lblInput, 0, 0);
            convertTable.Controls.Add(txtInput, 1, 0);
            convertTable.Controls.Add(lblResult, 2, 0);
            convertTable.Controls.Add(txtResult, 3, 0);

            // Add to inputTable
            inputTable.Controls.Add(grpK, 0, 0);
            inputTable.Controls.Add(grpI, 1, 0);
            inputTable.Controls.Add(grpJ, 2, 0);
            inputTable.Controls.Add(grpMode, 3, 0);
            inputTable.Controls.Add(btnPanel, 4, 0);
            inputTable.Controls.Add(grpConvert, 0, 1);
            inputTable.SetColumnSpan(grpConvert, 5);

            BackColor = AppTheme.ContentBg;
            Controls.Add(inputTable);
            Name = "UnitConvertPanel"; Size = new Size(1050, 700);
            ResumeLayout(false);
        }

        private TableLayoutPanel inputTable, convertTable;
        private GroupBox grpK, grpI, grpJ, grpMode, grpConvert;
        private ComboBox cboK, cboI, cboJ;
        private RadioButton rbWeight, rbAtom;
        private Label lblInput, lblResult;
        private TextBox txtInput, txtResult;
        private Button btnConvert, btnReset;
    }
}
