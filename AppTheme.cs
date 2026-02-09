namespace AlloyAct_Pro
{
    internal static class AppTheme
    {
        // Sidebar
        public static readonly Color SidebarBg = Color.FromArgb(45, 62, 80);
        public static readonly Color SidebarActiveBg = Color.FromArgb(52, 73, 94);
        public static readonly Color SidebarActiveIndicator = Color.FromArgb(52, 152, 219);
        public static readonly Color SidebarText = Color.FromArgb(189, 195, 199);
        public static readonly Color SidebarActiveText = Color.White;

        // Content
        public static readonly Color ContentBg = Color.FromArgb(245, 247, 250);
        public static readonly Color HeaderBg = Color.White;

        // Buttons
        public static readonly Color CalcBtnBg = Color.FromArgb(41, 128, 185);
        public static readonly Color ResetBtnBg = Color.FromArgb(149, 165, 166);

        // DataGridView
        public static readonly Color DgvHeaderBg = Color.FromArgb(52, 73, 94);
        public static readonly Color DgvHeaderFg = Color.White;
        public static readonly Color DgvAltRowBg = Color.FromArgb(236, 240, 241);

        // Cards
        public static readonly Color CardBorder = Color.FromArgb(189, 195, 199);

        // Fonts
        public static readonly Font BodyFont = new Font("Microsoft YaHei UI", 10F);
        public static readonly Font InputFont = new Font("Microsoft YaHei UI", 11F);
        public static readonly Font GroupBoxFont = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        public static readonly Font PageTitleFont = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
        public static readonly Font CalcBtnFont = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        public static readonly Font SidebarFont = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        public static readonly Font SidebarSectionFont = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);

        public static void StyleDataGridView(DataGridView dgv)
        {
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.FromArgb(228, 232, 235);
            dgv.EnableHeadersVisualStyles = false;
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToAddRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 36;
            dgv.RowTemplate.Height = 30;

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = DgvHeaderBg,
                ForeColor = DgvHeaderFg,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                SelectionBackColor = DgvHeaderBg,
                SelectionForeColor = DgvHeaderFg,
                Padding = new Padding(4)
            };

            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = BodyFont,
                Padding = new Padding(4),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                SelectionBackColor = Color.FromArgb(52, 152, 219),
                SelectionForeColor = Color.White
            };

            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = DgvAltRowBg
            };
        }

        public static void StyleCalcButton(Button btn)
        {
            btn.BackColor = CalcBtnBg;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = CalcBtnFont;
            btn.Cursor = Cursors.Hand;
        }

        public static void StyleResetButton(Button btn)
        {
            btn.BackColor = ResetBtnBg;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = CalcBtnFont;
            btn.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Setup a GroupBox with a ComboBox that reliably fills the width.
        /// The ComboBox is placed in a wrapper panel with Dock=Top and Padding
        /// so it stretches correctly even when the parent is resized.
        /// </summary>
        public static void SetupInputGroup(GroupBox grp, ComboBox cbo, string label, object[] items = null)
        {
            grp.Text = label;
            grp.Font = GroupBoxFont;
            grp.Dock = DockStyle.Fill;
            grp.Margin = new Padding(3);
            grp.Padding = new Padding(6, 22, 6, 4);
            cbo.Font = InputFont;
            cbo.Dock = DockStyle.Top;
            if (items != null) cbo.Items.AddRange(items);
            grp.Controls.Add(cbo);
        }

        /// <summary>
        /// Setup a GroupBox with a TextBox that reliably fills the width.
        /// Same Dock+Padding pattern as SetupInputGroup but for TextBox.
        /// </summary>
        public static void SetupInputGroupTextBox(GroupBox grp, TextBox txt, string label)
        {
            grp.Text = label;
            grp.Font = GroupBoxFont;
            grp.Dock = DockStyle.Fill;
            grp.Margin = new Padding(3);
            grp.Padding = new Padding(6, 22, 6, 4);
            txt.Font = InputFont;
            txt.Dock = DockStyle.Top;
            grp.Controls.Add(txt);
        }

        public static Button CreateNavButton(string text)
        {
            var btn = new Button();
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = SidebarActiveBg;
            btn.BackColor = SidebarBg;
            btn.ForeColor = SidebarText;
            btn.Font = SidebarFont;
            btn.Text = text;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(20, 0, 0, 0);
            btn.Size = new Size(220, 44);
            btn.Cursor = Cursors.Hand;
            btn.Dock = DockStyle.Top;
            return btn;
        }
    }
}
