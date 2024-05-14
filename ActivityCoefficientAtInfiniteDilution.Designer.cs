namespace AlloyAct_Pro
{
    partial class ActivityCoefficientAtInfiniteDilution
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            splitContainer1 = new SplitContainer();
            tableLayoutPanel2 = new TableLayoutPanel();
            label4 = new Label();
            T_combox = new ComboBox();
            Reset_btn = new Button();
            label3 = new Label();
            label1 = new Label();
            i_combox = new ComboBox();
            label2 = new Label();
            k_combox = new ComboBox();
            calc_btn = new Button();
            tableLayoutPanel1 = new TableLayoutPanel();
            L_checkBox = new CheckBox();
            S_checkBox = new CheckBox();
            menuStrip1 = new MenuStrip();
            optionToolStripMenuItem = new ToolStripMenuItem();
            saveToolStripMenuItem = new ToolStripMenuItem();
            settingToolStripMenuItem = new ToolStripMenuItem();
            phaseStateToolStripMenuItem = new ToolStripMenuItem();
            liquidToolStripMenuItem = new ToolStripMenuItem();
            solidToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            dataGridView1 = new DataGridView();
            melts = new DataGridViewTextBoxColumn();
            lnYi = new DataGridViewTextBoxColumn();
            exp = new DataGridViewTextBoxColumn();
            Tem = new DataGridViewTextBoxColumn();
            state = new DataGridViewTextBoxColumn();
            Remark = new DataGridViewTextBoxColumn();
            toolTip1 = new ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Margin = new Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(tableLayoutPanel2);
            splitContainer1.Panel1.Controls.Add(menuStrip1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dataGridView1);
            splitContainer1.Size = new Size(816, 493);
            splitContainer1.SplitterDistance = 220;
            splitContainer1.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.CellBorderStyle = TableLayoutPanelCellBorderStyle.OutsetDouble;
            tableLayoutPanel2.ColumnCount = 4;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel2.Controls.Add(label4, 3, 0);
            tableLayoutPanel2.Controls.Add(T_combox, 2, 1);
            tableLayoutPanel2.Controls.Add(Reset_btn, 2, 2);
            tableLayoutPanel2.Controls.Add(label3, 2, 0);
            tableLayoutPanel2.Controls.Add(label1, 0, 0);
            tableLayoutPanel2.Controls.Add(i_combox, 1, 1);
            tableLayoutPanel2.Controls.Add(label2, 1, 0);
            tableLayoutPanel2.Controls.Add(k_combox, 0, 1);
            tableLayoutPanel2.Controls.Add(calc_btn, 1, 2);
            tableLayoutPanel2.Controls.Add(tableLayoutPanel1, 3, 1);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(0, 28);
            tableLayoutPanel2.Margin = new Padding(3, 4, 3, 4);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 3;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 26.9430046F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 43.5233154F));
            tableLayoutPanel2.Size = new Size(816, 192);
            tableLayoutPanel2.TabIndex = 2;
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            label4.AutoSize = true;
            label4.BackColor = Color.LightSeaGreen;
            label4.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Italic, GraphicsUnit.Point);
            label4.Location = new Point(615, 31);
            label4.Name = "label4";
            label4.Size = new Size(195, 25);
            label4.TabIndex = 9;
            label4.Text = "State";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // T_combox
            // 
            T_combox.Dock = DockStyle.Fill;
            T_combox.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Regular, GraphicsUnit.Point);
            T_combox.FormattingEnabled = true;
            T_combox.Location = new Point(412, 63);
            T_combox.Margin = new Padding(3, 4, 3, 4);
            T_combox.Name = "T_combox";
            T_combox.Size = new Size(194, 35);
            T_combox.TabIndex = 7;
            // 
            // Reset_btn
            // 
            Reset_btn.Dock = DockStyle.Fill;
            Reset_btn.Font = new Font("楷体", 15F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            Reset_btn.Location = new Point(412, 114);
            Reset_btn.Margin = new Padding(3, 4, 3, 4);
            Reset_btn.Name = "Reset_btn";
            Reset_btn.Size = new Size(194, 71);
            Reset_btn.TabIndex = 1;
            Reset_btn.Text = "清除";
            Reset_btn.UseVisualStyleBackColor = true;
            Reset_btn.Click += Reset_btn_Click;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            label3.AutoSize = true;
            label3.BackColor = Color.LightSeaGreen;
            label3.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Italic, GraphicsUnit.Point);
            label3.Location = new Point(412, 31);
            label3.Name = "label3";
            label3.Size = new Size(194, 25);
            label3.TabIndex = 4;
            label3.Text = "温度(K)";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.BackColor = Color.LightSeaGreen;
            label1.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Italic, GraphicsUnit.Point);
            label1.Location = new Point(6, 31);
            label1.Name = "label1";
            label1.Size = new Size(194, 25);
            label1.TabIndex = 2;
            label1.Text = "基体(k)";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // i_combox
            // 
            i_combox.Dock = DockStyle.Fill;
            i_combox.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Regular, GraphicsUnit.Point);
            i_combox.FormattingEnabled = true;
            i_combox.Location = new Point(209, 63);
            i_combox.Margin = new Padding(3, 4, 3, 4);
            i_combox.Name = "i_combox";
            i_combox.Size = new Size(194, 35);
            i_combox.TabIndex = 6;
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            label2.AutoSize = true;
            label2.BackColor = Color.LightSeaGreen;
            label2.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Italic, GraphicsUnit.Point);
            label2.Location = new Point(209, 31);
            label2.Name = "label2";
            label2.Size = new Size(194, 25);
            label2.TabIndex = 3;
            label2.Text = "溶质(i)";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // k_combox
            // 
            k_combox.Dock = DockStyle.Fill;
            k_combox.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Regular, GraphicsUnit.Point);
            k_combox.FormattingEnabled = true;
            k_combox.Location = new Point(6, 63);
            k_combox.Margin = new Padding(3, 4, 3, 4);
            k_combox.Name = "k_combox";
            k_combox.Size = new Size(194, 35);
            k_combox.TabIndex = 5;
            // 
            // calc_btn
            // 
            calc_btn.Dock = DockStyle.Fill;
            calc_btn.Font = new Font("楷体", 15F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            calc_btn.Location = new Point(209, 114);
            calc_btn.Margin = new Padding(3, 4, 3, 4);
            calc_btn.Name = "calc_btn";
            calc_btn.Size = new Size(194, 71);
            calc_btn.TabIndex = 8;
            calc_btn.Text = "计算";
            calc_btn.UseVisualStyleBackColor = true;
            calc_btn.Click += calc_btn_Click;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.CellBorderStyle = TableLayoutPanelCellBorderStyle.InsetDouble;
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(L_checkBox, 0, 0);
            tableLayoutPanel1.Controls.Add(S_checkBox, 1, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(615, 63);
            tableLayoutPanel1.Margin = new Padding(3, 4, 3, 4);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Size = new Size(195, 40);
            tableLayoutPanel1.TabIndex = 10;
            // 
            // L_checkBox
            // 
            L_checkBox.AutoSize = true;
            L_checkBox.Checked = true;
            L_checkBox.CheckState = CheckState.Checked;
            L_checkBox.Dock = DockStyle.Fill;
            L_checkBox.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            L_checkBox.Location = new Point(6, 7);
            L_checkBox.Margin = new Padding(3, 4, 3, 4);
            L_checkBox.Name = "L_checkBox";
            L_checkBox.Size = new Size(87, 26);
            L_checkBox.TabIndex = 0;
            L_checkBox.Text = "L";
            L_checkBox.UseVisualStyleBackColor = true;
            L_checkBox.Click += L_checkBox_Click;
            // 
            // S_checkBox
            // 
            S_checkBox.AutoSize = true;
            S_checkBox.Dock = DockStyle.Fill;
            S_checkBox.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            S_checkBox.Location = new Point(102, 7);
            S_checkBox.Margin = new Padding(3, 4, 3, 4);
            S_checkBox.Name = "S_checkBox";
            S_checkBox.Size = new Size(87, 26);
            S_checkBox.TabIndex = 1;
            S_checkBox.Text = "S";
            S_checkBox.UseVisualStyleBackColor = true;
            S_checkBox.Click += S_checkBox_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { optionToolStripMenuItem, settingToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(816, 28);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // optionToolStripMenuItem
            // 
            optionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { saveToolStripMenuItem });
            optionToolStripMenuItem.Name = "optionToolStripMenuItem";
            optionToolStripMenuItem.Size = new Size(72, 24);
            optionToolStripMenuItem.Text = "Option";
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(112, 24);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // settingToolStripMenuItem
            // 
            settingToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { phaseStateToolStripMenuItem });
            settingToolStripMenuItem.Name = "settingToolStripMenuItem";
            settingToolStripMenuItem.Size = new Size(74, 24);
            settingToolStripMenuItem.Text = "Setting";
            // 
            // phaseStateToolStripMenuItem
            // 
            phaseStateToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { liquidToolStripMenuItem, solidToolStripMenuItem });
            phaseStateToolStripMenuItem.Name = "phaseStateToolStripMenuItem";
            phaseStateToolStripMenuItem.Size = new Size(158, 24);
            phaseStateToolStripMenuItem.Text = "PhaseState";
            // 
            // liquidToolStripMenuItem
            // 
            liquidToolStripMenuItem.Checked = true;
            liquidToolStripMenuItem.CheckState = CheckState.Checked;
            liquidToolStripMenuItem.Name = "liquidToolStripMenuItem";
            liquidToolStripMenuItem.Size = new Size(123, 24);
            liquidToolStripMenuItem.Text = "Liquid";
            liquidToolStripMenuItem.Click += liquidToolStripMenuItem_Click;
            // 
            // solidToolStripMenuItem
            // 
            solidToolStripMenuItem.Name = "solidToolStripMenuItem";
            solidToolStripMenuItem.Size = new Size(123, 24);
            solidToolStripMenuItem.Text = "Solid";
            solidToolStripMenuItem.Click += solidToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(56, 24);
            helpToolStripMenuItem.Text = "Help";
            // 
            // dataGridView1
            // 
            dataGridViewCellStyle1.NullValue = null;
            dataGridView1.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.BorderStyle = BorderStyle.Fixed3D;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.Raised;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Control;
            dataGridViewCellStyle2.Font = new Font("楷体", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle2.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
            dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { melts, lnYi, exp, Tem, state, Remark });
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = SystemColors.Window;
            dataGridViewCellStyle5.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle5.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle5.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = DataGridViewTriState.False;
            dataGridView1.DefaultCellStyle = dataGridViewCellStyle5;
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(0, 0);
            dataGridView1.Margin = new Padding(3, 4, 3, 4);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.RowTemplate.Height = 29;
            dataGridView1.ScrollBars = ScrollBars.Vertical;
            dataGridView1.Size = new Size(816, 269);
            dataGridView1.TabIndex = 0;
            // 
            // melts
            // 
            melts.HeaderText = "k-i";
            melts.MinimumWidth = 6;
            melts.Name = "melts";
            // 
            // lnYi
            // 
            dataGridViewCellStyle3.Format = "N3";
            dataGridViewCellStyle3.NullValue = null;
            lnYi.DefaultCellStyle = dataGridViewCellStyle3;
            lnYi.HeaderText = "lnYi0";
            lnYi.MinimumWidth = 6;
            lnYi.Name = "lnYi";
            // 
            // exp
            // 
            dataGridViewCellStyle4.Format = "N3";
            dataGridViewCellStyle4.NullValue = null;
            exp.DefaultCellStyle = dataGridViewCellStyle4;
            exp.HeaderText = "lnYi0(exp)";
            exp.MinimumWidth = 6;
            exp.Name = "exp";
            // 
            // Tem
            // 
            Tem.HeaderText = "T(K)";
            Tem.MinimumWidth = 6;
            Tem.Name = "Tem";
            // 
            // state
            // 
            state.HeaderText = "State";
            state.MinimumWidth = 6;
            state.Name = "state";
            // 
            // Remark
            // 
            Remark.HeaderText = "Remark";
            Remark.MinimumWidth = 6;
            Remark.Name = "Remark";
            // 
            // ActivityCoefficientAtInfiniteDilution
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(816, 493);
            Controls.Add(splitContainer1);
            MainMenuStrip = menuStrip1;
            Margin = new Padding(3, 4, 3, 4);
            Name = "ActivityCoefficientAtInfiniteDilution";
            Text = "Activity Coefficient At Infinite Dilution";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem optionToolStripMenuItem;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem settingToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private Label label1;
        private Label label3;
        private Label label2;
        private ComboBox k_combox;
        private ComboBox i_combox;
        private ComboBox T_combox;
        private ToolTip toolTip1;
        private DataGridView dataGridView1;
        private ToolStripMenuItem phaseStateToolStripMenuItem;
        private ToolStripMenuItem liquidToolStripMenuItem;
        private ToolStripMenuItem solidToolStripMenuItem;
        private TableLayoutPanel tableLayoutPanel2;
        private Label label4;
        private Button Reset_btn;
        private Button calc_btn;
        private TableLayoutPanel tableLayoutPanel1;
        private CheckBox L_checkBox;
        private CheckBox S_checkBox;
        private DataGridViewTextBoxColumn melts;
        private DataGridViewTextBoxColumn lnYi;
        private DataGridViewTextBoxColumn exp;
        private DataGridViewTextBoxColumn Tem;
        private DataGridViewTextBoxColumn state;
        private DataGridViewTextBoxColumn Remark;
    }
}