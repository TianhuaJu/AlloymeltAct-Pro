namespace AlloyAct_Pro
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            sidebarPanel = new Panel();
            sidebarLogo = new Label();
            lblCalcSection = new Label();
            btnActivity = AppTheme.CreateNavButton("  Activity");
            btnCoefficient = AppTheme.CreateNavButton("  Activity Coeff.");
            btnInteraction = AppTheme.CreateNavButton("  Interaction Coeff.");
            btnInfiniteDilution = AppTheme.CreateNavButton("  Infinite Dilution");
            btnSecondOrder = AppTheme.CreateNavButton("  Second-Order");
            lblToolsSection = new Label();
            btnUnitConvert = AppTheme.CreateNavButton("  Unit Conversion");
            lblAdvancedSection = new Label();
            btnDatabase = AppTheme.CreateNavButton("  Database");
            btnLiquidus = AppTheme.CreateNavButton("  Liquidus Temp.");
            btnDft = AppTheme.CreateNavButton("  DFT Import");
            lblAISection = new Label();
            btnChat = AppTheme.CreateNavButton("  AI Assistant");
            btnKnowledge = AppTheme.CreateNavButton("  Knowledge");

            headerPanel = new Panel();
            lblPageTitle = new Label();
            headerBtnPanel = new FlowLayoutPanel();
            btnExport = new Button();
            btnHelp = new Button();
            btnAbout = new Button();

            contentPanel = new Panel();

            SuspendLayout();

            // ===== Sidebar =====
            sidebarPanel.BackColor = AppTheme.SidebarBg;
            sidebarPanel.Dock = DockStyle.Left;
            sidebarPanel.Width = 220;

            // Logo at top
            sidebarLogo.Text = "AlloyAct Pro";
            sidebarLogo.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            sidebarLogo.ForeColor = Color.White;
            sidebarLogo.BackColor = Color.FromArgb(35, 50, 68);
            sidebarLogo.Dock = DockStyle.Top;
            sidebarLogo.Height = 56;
            sidebarLogo.TextAlign = ContentAlignment.MiddleCenter;

            // Section labels
            lblCalcSection.Text = "  CALCULATIONS";
            lblCalcSection.Font = AppTheme.SidebarSectionFont;
            lblCalcSection.ForeColor = Color.FromArgb(127, 140, 141);
            lblCalcSection.BackColor = AppTheme.SidebarBg;
            lblCalcSection.Dock = DockStyle.Top;
            lblCalcSection.Height = 30;
            lblCalcSection.TextAlign = ContentAlignment.BottomLeft;
            lblCalcSection.Padding = new Padding(14, 0, 0, 4);

            lblToolsSection.Text = "  TOOLS";
            lblToolsSection.Font = AppTheme.SidebarSectionFont;
            lblToolsSection.ForeColor = Color.FromArgb(127, 140, 141);
            lblToolsSection.BackColor = AppTheme.SidebarBg;
            lblToolsSection.Dock = DockStyle.Top;
            lblToolsSection.Height = 30;
            lblToolsSection.TextAlign = ContentAlignment.BottomLeft;
            lblToolsSection.Padding = new Padding(14, 0, 0, 4);

            lblAdvancedSection.Text = "  ADVANCED";
            lblAdvancedSection.Font = AppTheme.SidebarSectionFont;
            lblAdvancedSection.ForeColor = Color.FromArgb(127, 140, 141);
            lblAdvancedSection.BackColor = AppTheme.SidebarBg;
            lblAdvancedSection.Dock = DockStyle.Top;
            lblAdvancedSection.Height = 30;
            lblAdvancedSection.TextAlign = ContentAlignment.BottomLeft;
            lblAdvancedSection.Padding = new Padding(14, 0, 0, 4);

            // Nav button click events
            btnActivity.Click += BtnActivity_Click;
            btnCoefficient.Click += BtnCoefficient_Click;
            btnInteraction.Click += BtnInteraction_Click;
            btnInfiniteDilution.Click += BtnInfiniteDilution_Click;
            btnSecondOrder.Click += BtnSecondOrder_Click;
            btnUnitConvert.Click += BtnUnitConvert_Click;
            btnDatabase.Click += BtnDatabase_Click;
            btnLiquidus.Click += BtnLiquidus_Click;
            btnDft.Click += BtnDft_Click;
            btnChat.Click += BtnChat_Click;
            btnKnowledge.Click += BtnKnowledge_Click;

            // AI section label
            lblAISection.Text = "  AI";
            lblAISection.Font = AppTheme.SidebarSectionFont;
            lblAISection.ForeColor = Color.FromArgb(127, 140, 141);
            lblAISection.BackColor = AppTheme.SidebarBg;
            lblAISection.Dock = DockStyle.Top;
            lblAISection.Height = 30;
            lblAISection.TextAlign = ContentAlignment.BottomLeft;
            lblAISection.Padding = new Padding(14, 0, 0, 4);

            // Add in REVERSE order because Dock=Top stacks top-down
            // Visual order: CALCULATIONS → ADVANCED → TOOLS

            // AI section (bottom-most): AI Assistant, Knowledge
            sidebarPanel.Controls.Add(btnKnowledge);
            sidebarPanel.Controls.Add(btnChat);
            sidebarPanel.Controls.Add(lblAISection);
            // TOOLS section: Database, Unit Conversion
            sidebarPanel.Controls.Add(btnUnitConvert);
            sidebarPanel.Controls.Add(btnDatabase);
            sidebarPanel.Controls.Add(lblToolsSection);
            // ADVANCED section (middle): Liquidus Temp., DFT Import
            sidebarPanel.Controls.Add(btnDft);
            sidebarPanel.Controls.Add(btnLiquidus);
            sidebarPanel.Controls.Add(lblAdvancedSection);
            // CALCULATIONS section (top): Infinite Dilution, Interaction, Second-Order, Activity Coeff., Activity
            sidebarPanel.Controls.Add(btnActivity);
            sidebarPanel.Controls.Add(btnCoefficient);
            sidebarPanel.Controls.Add(btnSecondOrder);
            sidebarPanel.Controls.Add(btnInteraction);
            sidebarPanel.Controls.Add(btnInfiniteDilution);
            sidebarPanel.Controls.Add(lblCalcSection);
            sidebarPanel.Controls.Add(sidebarLogo);

            // ===== Header =====
            headerPanel.BackColor = AppTheme.HeaderBg;
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 52;
            headerPanel.Padding = new Padding(16, 0, 12, 0);

            lblPageTitle.Text = "Activity Calculation";
            lblPageTitle.Font = AppTheme.PageTitleFont;
            lblPageTitle.ForeColor = Color.FromArgb(44, 62, 80);
            lblPageTitle.Dock = DockStyle.Fill;
            lblPageTitle.TextAlign = ContentAlignment.MiddleLeft;

            // Header button panel (FlowLayout, right-aligned)
            headerBtnPanel.Dock = DockStyle.Right;
            headerBtnPanel.FlowDirection = FlowDirection.RightToLeft;
            headerBtnPanel.WrapContents = false;
            headerBtnPanel.AutoSize = true;
            headerBtnPanel.BackColor = AppTheme.HeaderBg;
            headerBtnPanel.Padding = new Padding(0, 9, 0, 9);

            btnExport.Text = "Export";
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.BackColor = Color.FromArgb(39, 174, 96);
            btnExport.ForeColor = Color.White;
            btnExport.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            btnExport.Size = new Size(80, 32);
            btnExport.Margin = new Padding(6, 0, 0, 0);
            btnExport.Cursor = Cursors.Hand;
            btnExport.Click += BtnExport_Click;

            btnHelp.Text = "Help";
            btnHelp.FlatStyle = FlatStyle.Flat;
            btnHelp.FlatAppearance.BorderColor = Color.FromArgb(52, 152, 219);
            btnHelp.FlatAppearance.BorderSize = 1;
            btnHelp.BackColor = Color.White;
            btnHelp.ForeColor = Color.FromArgb(52, 152, 219);
            btnHelp.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            btnHelp.Size = new Size(70, 32);
            btnHelp.Margin = new Padding(6, 0, 0, 0);
            btnHelp.Cursor = Cursors.Hand;
            btnHelp.Click += BtnHelp_Click;

            btnAbout.Text = "About";
            btnAbout.FlatStyle = FlatStyle.Flat;
            btnAbout.FlatAppearance.BorderColor = Color.FromArgb(189, 195, 199);
            btnAbout.FlatAppearance.BorderSize = 1;
            btnAbout.BackColor = Color.White;
            btnAbout.ForeColor = Color.FromArgb(100, 100, 100);
            btnAbout.Font = new Font("Microsoft YaHei UI", 9F);
            btnAbout.Size = new Size(70, 32);
            btnAbout.Margin = new Padding(6, 0, 0, 0);
            btnAbout.Cursor = Cursors.Hand;
            btnAbout.Click += BtnAbout_Click;

            headerBtnPanel.Controls.Add(btnAbout);
            headerBtnPanel.Controls.Add(btnHelp);
            headerBtnPanel.Controls.Add(btnExport);

            headerPanel.Controls.Add(lblPageTitle);
            headerPanel.Controls.Add(headerBtnPanel);

            // ===== Content Panel =====
            contentPanel.BackColor = AppTheme.ContentBg;
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Padding = new Padding(0);

            // ===== Form1 =====
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = AppTheme.ContentBg;
            ClientSize = new Size(1470, 860);
            MinimumSize = new Size(1200, 750);
            Controls.Add(contentPanel);
            Controls.Add(headerPanel);
            Controls.Add(sidebarPanel);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AlloyAct Pro - Alloy Melt Activity Calculator";
            try
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                    Icon = Icon.ExtractAssociatedIcon(exePath);
            }
            catch { }
            ResumeLayout(false);
        }

        #endregion

        private Panel sidebarPanel;
        private Panel headerPanel;
        private FlowLayoutPanel headerBtnPanel;
        private Panel contentPanel;
        private Label sidebarLogo;
        private Label lblCalcSection;
        private Label lblToolsSection;
        private Label lblPageTitle;
        private Button btnActivity;
        private Button btnCoefficient;
        private Button btnInteraction;
        private Button btnInfiniteDilution;
        private Button btnSecondOrder;
        private Button btnUnitConvert;
        private Label lblAdvancedSection;
        private Button btnDatabase;
        private Button btnLiquidus;
        private Button btnExport;
        private Button btnHelp;
        private Button btnAbout;
        private Label lblAISection;
        private Button btnChat;
        private Button btnKnowledge;
        private Button btnDft;
    }
}
