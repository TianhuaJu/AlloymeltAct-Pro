namespace AlloyAct_Pro.Controls
{
    partial class DatabasePanel
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && components != null) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            lockPanel = new Panel();
            dataPanel = new Panel();
            tabControl = new TabControl();
            tabMiedema = new TabPage();
            tabFirstOrder = new TabPage();
            tabLnY0 = new TabPage();
            dgvMiedema = new DataGridView();
            dgvFirstOrder = new DataGridView();
            dgvLnY0 = new DataGridView();
            btnSave = new Button();
            btnRefresh = new Button();

            // Filter controls for each tab
            txtFilterMiedema = new TextBox();
            btnFilterMiedema = new Button();
            btnShowAllMiedema = new Button();
            txtFilterFirstOrder = new TextBox();
            btnFilterFirstOrder = new Button();
            btnShowAllFirstOrder = new Button();
            txtFilterLnY0 = new TextBox();
            btnFilterLnY0 = new Button();
            btnShowAllLnY0 = new Button();

            SuspendLayout();

            // ===== Lock Panel (password overlay) =====
            lockPanel.Dock = DockStyle.Fill;
            lockPanel.BackColor = Color.FromArgb(245, 247, 250);

            var lockContainer = new Panel();
            lockContainer.Size = new Size(400, 260);
            lockContainer.BackColor = Color.White;
            lockContainer.BorderStyle = BorderStyle.FixedSingle;

            var lblLockIcon = new Label();
            lblLockIcon.Text = "\U0001F512";
            lblLockIcon.Font = new Font("Segoe UI Emoji", 28F);
            lblLockIcon.TextAlign = ContentAlignment.MiddleCenter;
            lblLockIcon.Dock = DockStyle.Top;
            lblLockIcon.Height = 60;

            var lblLockTitle = new Label();
            lblLockTitle.Text = "Advanced Feature";
            lblLockTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            lblLockTitle.ForeColor = Color.FromArgb(44, 62, 80);
            lblLockTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblLockTitle.Dock = DockStyle.Top;
            lblLockTitle.Height = 36;

            var lblLockHint = new Label();
            lblLockHint.Text = "Enter password to access database management";
            lblLockHint.Font = new Font("Microsoft YaHei UI", 9.5F);
            lblLockHint.ForeColor = Color.FromArgb(120, 120, 120);
            lblLockHint.TextAlign = ContentAlignment.MiddleCenter;
            lblLockHint.Dock = DockStyle.Top;
            lblLockHint.Height = 28;

            txtPassword = new TextBox();
            txtPassword.Font = new Font("Microsoft YaHei UI", 11F);
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.Size = new Size(260, 30);
            txtPassword.Location = new Point(70, 140);
            txtPassword.PlaceholderText = "Password";
            txtPassword.KeyDown += TxtPassword_KeyDown;

            lblError = new Label();
            lblError.Text = "";
            lblError.Font = new Font("Microsoft YaHei UI", 9F);
            lblError.ForeColor = Color.FromArgb(231, 76, 60);
            lblError.TextAlign = ContentAlignment.MiddleCenter;
            lblError.Location = new Point(70, 178);
            lblError.Size = new Size(260, 22);

            btnUnlock = new Button();
            btnUnlock.Text = "Unlock";
            btnUnlock.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            btnUnlock.FlatStyle = FlatStyle.Flat;
            btnUnlock.FlatAppearance.BorderSize = 0;
            btnUnlock.BackColor = Color.FromArgb(41, 128, 185);
            btnUnlock.ForeColor = Color.White;
            btnUnlock.Size = new Size(260, 36);
            btnUnlock.Location = new Point(70, 206);
            btnUnlock.Cursor = Cursors.Hand;
            btnUnlock.Click += BtnUnlock_Click;

            lockContainer.Controls.Add(btnUnlock);
            lockContainer.Controls.Add(lblError);
            lockContainer.Controls.Add(txtPassword);
            lockContainer.Controls.Add(lblLockHint);
            lockContainer.Controls.Add(lblLockTitle);
            lockContainer.Controls.Add(lblLockIcon);

            lockPanel.Controls.Add(lockContainer);
            // Center the lockContainer when panel resizes
            lockPanel.Resize += (s, e) =>
            {
                lockContainer.Location = new Point(
                    (lockPanel.Width - lockContainer.Width) / 2,
                    (lockPanel.Height - lockContainer.Height) / 2 - 40);
            };

            // ===== Data Panel (hidden until unlocked) =====
            dataPanel.Dock = DockStyle.Fill;
            dataPanel.Visible = false;

            // Bottom button bar
            var bottomBar = new Panel();
            bottomBar.Dock = DockStyle.Bottom;
            bottomBar.Height = 50;
            bottomBar.BackColor = Color.White;
            bottomBar.Padding = new Padding(10, 8, 10, 8);

            btnSave.Text = "Save Changes";
            btnSave.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.BackColor = Color.FromArgb(39, 174, 96);
            btnSave.ForeColor = Color.White;
            btnSave.Size = new Size(130, 34);
            btnSave.Location = new Point(10, 8);
            btnSave.Cursor = Cursors.Hand;
            btnSave.Click += BtnSave_Click;

            btnRefresh.Text = "Refresh";
            btnRefresh.Font = new Font("Microsoft YaHei UI", 9.5F);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(189, 195, 199);
            btnRefresh.FlatAppearance.BorderSize = 1;
            btnRefresh.BackColor = Color.White;
            btnRefresh.ForeColor = Color.FromArgb(80, 80, 80);
            btnRefresh.Size = new Size(100, 34);
            btnRefresh.Location = new Point(150, 8);
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Click += BtnRefresh_Click;

            bottomBar.Controls.Add(btnRefresh);
            bottomBar.Controls.Add(btnSave);

            // Tab control
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Microsoft YaHei UI", 10F);
            tabControl.Padding = new Point(12, 4);

            // --- Tab1: Miedema Parameters ---
            tabMiedema.Text = "Miedema Parameters";
            tabMiedema.Padding = new Padding(0);

            var filterBarMiedema = new Panel();
            filterBarMiedema.Dock = DockStyle.Top; filterBarMiedema.Height = 40; filterBarMiedema.BackColor = Color.White; filterBarMiedema.Padding = new Padding(8, 6, 8, 4);
            var lblFm = new Label(); lblFm.Text = "Filter by Symbol:"; lblFm.Font = new Font("Microsoft YaHei UI", 9F); lblFm.AutoSize = true; lblFm.Location = new Point(10, 10);
            txtFilterMiedema.Font = new Font("Microsoft YaHei UI", 9.5F); txtFilterMiedema.Size = new Size(120, 28); txtFilterMiedema.Location = new Point(130, 6);
            btnFilterMiedema.Text = "Filter"; btnFilterMiedema.FlatStyle = FlatStyle.Flat; btnFilterMiedema.Font = new Font("Microsoft YaHei UI", 8.5F);
            btnFilterMiedema.Size = new Size(60, 28); btnFilterMiedema.Location = new Point(260, 6); btnFilterMiedema.BackColor = Color.FromArgb(41, 128, 185); btnFilterMiedema.ForeColor = Color.White;
            btnFilterMiedema.FlatAppearance.BorderSize = 0; btnFilterMiedema.Cursor = Cursors.Hand; btnFilterMiedema.Click += BtnFilterMiedema_Click;
            btnShowAllMiedema.Text = "Show All"; btnShowAllMiedema.FlatStyle = FlatStyle.Flat; btnShowAllMiedema.Font = new Font("Microsoft YaHei UI", 8.5F);
            btnShowAllMiedema.Size = new Size(80, 28); btnShowAllMiedema.Location = new Point(326, 6); btnShowAllMiedema.BackColor = Color.FromArgb(149, 165, 166); btnShowAllMiedema.ForeColor = Color.White;
            btnShowAllMiedema.FlatAppearance.BorderSize = 0; btnShowAllMiedema.Cursor = Cursors.Hand; btnShowAllMiedema.Click += BtnShowAllMiedema_Click;
            filterBarMiedema.Controls.Add(btnShowAllMiedema); filterBarMiedema.Controls.Add(btnFilterMiedema); filterBarMiedema.Controls.Add(txtFilterMiedema); filterBarMiedema.Controls.Add(lblFm);

            dgvMiedema.Dock = DockStyle.Fill;
            AppTheme.StyleDataGridView(dgvMiedema);
            dgvMiedema.AllowUserToAddRows = false;
            dgvMiedema.AllowUserToDeleteRows = false;
            dgvMiedema.ReadOnly = false;

            tabMiedema.Controls.Add(dgvMiedema);
            tabMiedema.Controls.Add(filterBarMiedema);

            // --- Tab2: First Order Interaction Coefficients ---
            tabFirstOrder.Text = "Interaction Coeff. (\u03B5\u1D62\u02B2)";
            tabFirstOrder.Padding = new Padding(0);

            var filterBarFO = new Panel();
            filterBarFO.Dock = DockStyle.Top; filterBarFO.Height = 40; filterBarFO.BackColor = Color.White; filterBarFO.Padding = new Padding(8, 6, 8, 4);
            var lblFfo = new Label(); lblFfo.Text = "Filter by Solvent:"; lblFfo.Font = new Font("Microsoft YaHei UI", 9F); lblFfo.AutoSize = true; lblFfo.Location = new Point(10, 10);
            txtFilterFirstOrder.Font = new Font("Microsoft YaHei UI", 9.5F); txtFilterFirstOrder.Size = new Size(120, 28); txtFilterFirstOrder.Location = new Point(130, 6);
            btnFilterFirstOrder.Text = "Filter"; btnFilterFirstOrder.FlatStyle = FlatStyle.Flat; btnFilterFirstOrder.Font = new Font("Microsoft YaHei UI", 8.5F);
            btnFilterFirstOrder.Size = new Size(60, 28); btnFilterFirstOrder.Location = new Point(260, 6); btnFilterFirstOrder.BackColor = Color.FromArgb(41, 128, 185); btnFilterFirstOrder.ForeColor = Color.White;
            btnFilterFirstOrder.FlatAppearance.BorderSize = 0; btnFilterFirstOrder.Cursor = Cursors.Hand; btnFilterFirstOrder.Click += BtnFilterFirstOrder_Click;
            btnShowAllFirstOrder.Text = "Show All"; btnShowAllFirstOrder.FlatStyle = FlatStyle.Flat; btnShowAllFirstOrder.Font = new Font("Microsoft YaHei UI", 8.5F);
            btnShowAllFirstOrder.Size = new Size(80, 28); btnShowAllFirstOrder.Location = new Point(326, 6); btnShowAllFirstOrder.BackColor = Color.FromArgb(149, 165, 166); btnShowAllFirstOrder.ForeColor = Color.White;
            btnShowAllFirstOrder.FlatAppearance.BorderSize = 0; btnShowAllFirstOrder.Cursor = Cursors.Hand; btnShowAllFirstOrder.Click += BtnShowAllFirstOrder_Click;
            filterBarFO.Controls.Add(btnShowAllFirstOrder); filterBarFO.Controls.Add(btnFilterFirstOrder); filterBarFO.Controls.Add(txtFilterFirstOrder); filterBarFO.Controls.Add(lblFfo);

            dgvFirstOrder.Dock = DockStyle.Fill;
            AppTheme.StyleDataGridView(dgvFirstOrder);
            dgvFirstOrder.AllowUserToAddRows = false;
            dgvFirstOrder.AllowUserToDeleteRows = false;
            dgvFirstOrder.ReadOnly = false;

            tabFirstOrder.Controls.Add(dgvFirstOrder);
            tabFirstOrder.Controls.Add(filterBarFO);

            // --- Tab3: Infinite Dilution ---
            tabLnY0.Text = "Infinite Dilution (ln\u03B3\u1D62\u2070)";
            tabLnY0.Padding = new Padding(0);

            var filterBarLn = new Panel();
            filterBarLn.Dock = DockStyle.Top; filterBarLn.Height = 40; filterBarLn.BackColor = Color.White; filterBarLn.Padding = new Padding(8, 6, 8, 4);
            var lblFln = new Label(); lblFln.Text = "Filter by Solvent:"; lblFln.Font = new Font("Microsoft YaHei UI", 9F); lblFln.AutoSize = true; lblFln.Location = new Point(10, 10);
            txtFilterLnY0.Font = new Font("Microsoft YaHei UI", 9.5F); txtFilterLnY0.Size = new Size(120, 28); txtFilterLnY0.Location = new Point(130, 6);
            btnFilterLnY0.Text = "Filter"; btnFilterLnY0.FlatStyle = FlatStyle.Flat; btnFilterLnY0.Font = new Font("Microsoft YaHei UI", 8.5F);
            btnFilterLnY0.Size = new Size(60, 28); btnFilterLnY0.Location = new Point(260, 6); btnFilterLnY0.BackColor = Color.FromArgb(41, 128, 185); btnFilterLnY0.ForeColor = Color.White;
            btnFilterLnY0.FlatAppearance.BorderSize = 0; btnFilterLnY0.Cursor = Cursors.Hand; btnFilterLnY0.Click += BtnFilterLnY0_Click;
            btnShowAllLnY0.Text = "Show All"; btnShowAllLnY0.FlatStyle = FlatStyle.Flat; btnShowAllLnY0.Font = new Font("Microsoft YaHei UI", 8.5F);
            btnShowAllLnY0.Size = new Size(80, 28); btnShowAllLnY0.Location = new Point(326, 6); btnShowAllLnY0.BackColor = Color.FromArgb(149, 165, 166); btnShowAllLnY0.ForeColor = Color.White;
            btnShowAllLnY0.FlatAppearance.BorderSize = 0; btnShowAllLnY0.Cursor = Cursors.Hand; btnShowAllLnY0.Click += BtnShowAllLnY0_Click;
            filterBarLn.Controls.Add(btnShowAllLnY0); filterBarLn.Controls.Add(btnFilterLnY0); filterBarLn.Controls.Add(txtFilterLnY0); filterBarLn.Controls.Add(lblFln);

            dgvLnY0.Dock = DockStyle.Fill;
            AppTheme.StyleDataGridView(dgvLnY0);
            dgvLnY0.AllowUserToAddRows = false;
            dgvLnY0.AllowUserToDeleteRows = false;
            dgvLnY0.ReadOnly = false;

            tabLnY0.Controls.Add(dgvLnY0);
            tabLnY0.Controls.Add(filterBarLn);

            // Assemble tabs
            tabControl.TabPages.Add(tabMiedema);
            tabControl.TabPages.Add(tabFirstOrder);
            tabControl.TabPages.Add(tabLnY0);

            dataPanel.Controls.Add(tabControl);
            dataPanel.Controls.Add(bottomBar);

            // ===== Assemble main panel =====
            BackColor = AppTheme.ContentBg;
            Controls.Add(dataPanel);
            Controls.Add(lockPanel);
            Name = "DatabasePanel";
            Size = new Size(1050, 700);

            ResumeLayout(false);
        }

        private Panel lockPanel, dataPanel;
        private TextBox txtPassword;
        private Label lblError;
        private Button btnUnlock;
        private TabControl tabControl;
        private TabPage tabMiedema, tabFirstOrder, tabLnY0;
        private DataGridView dgvMiedema, dgvFirstOrder, dgvLnY0;
        private Button btnSave, btnRefresh;
        private TextBox txtFilterMiedema, txtFilterFirstOrder, txtFilterLnY0;
        private Button btnFilterMiedema, btnShowAllMiedema;
        private Button btnFilterFirstOrder, btnShowAllFirstOrder;
        private Button btnFilterLnY0, btnShowAllLnY0;
    }
}
