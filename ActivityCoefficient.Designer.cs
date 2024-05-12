namespace AlloyAct_Pro
{
    partial class ActivityCoefficientFm
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            splitContainer1 = new SplitContainer();
            dataGridView1 = new DataGridView();
            menuStrip1 = new MenuStrip();
            optionToolStripMenuItem = new ToolStripMenuItem();
            graphicToolStripMenuItem = new ToolStripMenuItem();
            saveToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            settingToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            tableLayoutPanel2 = new TableLayoutPanel();
            tableLayoutPanel1 = new TableLayoutPanel();
            compositions = new GroupBox();
            groupBox2 = new GroupBox();
            i_groupBox3 = new GroupBox();
            Temp_groupBox4 = new GroupBox();
            groupBox5 = new GroupBox();
            comboBox1 = new ComboBox();
            comboBox2 = new ComboBox();
            comboBox3 = new ComboBox();
            comboBox4 = new ComboBox();
            tableLayoutPanel3 = new TableLayoutPanel();
            checkBox1 = new CheckBox();
            checkBox2 = new CheckBox();
            tableLayoutPanel4 = new TableLayoutPanel();
            Cal_btn = new Button();
            reset_btn = new Button();
            Melt_composition = new DataGridViewTextBoxColumn();
            k_name = new DataGridViewTextBoxColumn();
            solute_i = new DataGridViewTextBoxColumn();
            activityCoefficient = new DataGridViewTextBoxColumn();
            Tem = new DataGridViewTextBoxColumn();
            state = new DataGridViewTextBoxColumn();
            remark = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            menuStrip1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            compositions.SuspendLayout();
            groupBox2.SuspendLayout();
            i_groupBox3.SuspendLayout();
            Temp_groupBox4.SuspendLayout();
            groupBox5.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
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
            splitContainer1.Size = new Size(880, 623);
            splitContainer1.SplitterDistance = 167;
            splitContainer1.TabIndex = 0;
            // 
            // dataGridView1
            // 
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.BorderStyle = BorderStyle.Fixed3D;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.Sunken;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { Melt_composition, k_name, solute_i, activityCoefficient, Tem, state, remark });
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Window;
            dataGridViewCellStyle1.Font = new Font("Times New Roman", 14F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
            dataGridView1.DefaultCellStyle = dataGridViewCellStyle1;
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(0, 0);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.RowTemplate.Height = 29;
            dataGridView1.Size = new Size(880, 452);
            dataGridView1.TabIndex = 0;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { optionToolStripMenuItem, graphicToolStripMenuItem, settingToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(880, 28);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // optionToolStripMenuItem
            // 
            optionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { saveToolStripMenuItem, exitToolStripMenuItem });
            optionToolStripMenuItem.Name = "optionToolStripMenuItem";
            optionToolStripMenuItem.Size = new Size(72, 24);
            optionToolStripMenuItem.Text = "Option";
            // 
            // graphicToolStripMenuItem
            // 
            graphicToolStripMenuItem.Name = "graphicToolStripMenuItem";
            graphicToolStripMenuItem.Size = new Size(77, 24);
            graphicToolStripMenuItem.Text = "Graphic";
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(180, 24);
            saveToolStripMenuItem.Text = "Save";
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(180, 24);
            exitToolStripMenuItem.Text = "Exit";
            // 
            // settingToolStripMenuItem
            // 
            settingToolStripMenuItem.Name = "settingToolStripMenuItem";
            settingToolStripMenuItem.Size = new Size(74, 24);
            settingToolStripMenuItem.Text = "Setting";
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(56, 24);
            helpToolStripMenuItem.Text = "Help";
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Controls.Add(tableLayoutPanel1, 0, 0);
            tableLayoutPanel2.Controls.Add(tableLayoutPanel4, 0, 1);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(0, 28);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 63.0769234F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 36.9230766F));
            tableLayoutPanel2.Size = new Size(880, 139);
            tableLayoutPanel2.TabIndex = 1;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.CellBorderStyle = TableLayoutPanelCellBorderStyle.InsetDouble;
            tableLayoutPanel1.ColumnCount = 5;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            tableLayoutPanel1.Controls.Add(groupBox5, 4, 0);
            tableLayoutPanel1.Controls.Add(Temp_groupBox4, 3, 0);
            tableLayoutPanel1.Controls.Add(i_groupBox3, 2, 0);
            tableLayoutPanel1.Controls.Add(groupBox2, 1, 0);
            tableLayoutPanel1.Controls.Add(compositions, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(3, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(874, 81);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // compositions
            // 
            compositions.Controls.Add(comboBox1);
            compositions.Dock = DockStyle.Fill;
            compositions.Font = new Font("宋体", 14F, FontStyle.Bold, GraphicsUnit.Point);
            compositions.Location = new Point(6, 6);
            compositions.Name = "compositions";
            compositions.Size = new Size(336, 69);
            compositions.TabIndex = 0;
            compositions.TabStop = false;
            compositions.Text = "熔体组成(AxByCz...)";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(comboBox2);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Font = new Font("宋体", 14F, FontStyle.Bold, GraphicsUnit.Point);
            groupBox2.Location = new Point(351, 6);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(122, 69);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "基体(k)";
            // 
            // i_groupBox3
            // 
            i_groupBox3.Controls.Add(comboBox3);
            i_groupBox3.Dock = DockStyle.Fill;
            i_groupBox3.Font = new Font("宋体", 14F, FontStyle.Bold, GraphicsUnit.Point);
            i_groupBox3.Location = new Point(482, 6);
            i_groupBox3.Name = "i_groupBox3";
            i_groupBox3.Size = new Size(122, 69);
            i_groupBox3.TabIndex = 2;
            i_groupBox3.TabStop = false;
            i_groupBox3.Text = "溶质(i)";
            // 
            // Temp_groupBox4
            // 
            Temp_groupBox4.Controls.Add(comboBox4);
            Temp_groupBox4.Dock = DockStyle.Fill;
            Temp_groupBox4.Font = new Font("宋体", 14F, FontStyle.Bold, GraphicsUnit.Point);
            Temp_groupBox4.Location = new Point(613, 6);
            Temp_groupBox4.Name = "Temp_groupBox4";
            Temp_groupBox4.Size = new Size(122, 69);
            Temp_groupBox4.TabIndex = 3;
            Temp_groupBox4.TabStop = false;
            Temp_groupBox4.Text = "温度(K)";
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(tableLayoutPanel3);
            groupBox5.Dock = DockStyle.Fill;
            groupBox5.Font = new Font("Times New Roman", 14F, FontStyle.Bold, GraphicsUnit.Point);
            groupBox5.Location = new Point(744, 6);
            groupBox5.Name = "groupBox5";
            groupBox5.Size = new Size(124, 69);
            groupBox5.TabIndex = 4;
            groupBox5.TabStop = false;
            groupBox5.Text = "State";
            // 
            // comboBox1
            // 
            comboBox1.Dock = DockStyle.Fill;
            comboBox1.Font = new Font("宋体", 18F, FontStyle.Bold, GraphicsUnit.Point);
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(3, 25);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(330, 32);
            comboBox1.TabIndex = 0;
            // 
            // comboBox2
            // 
            comboBox2.Dock = DockStyle.Fill;
            comboBox2.Font = new Font("宋体", 18F, FontStyle.Bold, GraphicsUnit.Point);
            comboBox2.FormattingEnabled = true;
            comboBox2.Location = new Point(3, 25);
            comboBox2.Name = "comboBox2";
            comboBox2.Size = new Size(116, 32);
            comboBox2.TabIndex = 1;
            // 
            // comboBox3
            // 
            comboBox3.Dock = DockStyle.Fill;
            comboBox3.Font = new Font("宋体", 18F, FontStyle.Bold, GraphicsUnit.Point);
            comboBox3.FormattingEnabled = true;
            comboBox3.Location = new Point(3, 25);
            comboBox3.Name = "comboBox3";
            comboBox3.Size = new Size(116, 32);
            comboBox3.TabIndex = 1;
            // 
            // comboBox4
            // 
            comboBox4.Dock = DockStyle.Fill;
            comboBox4.Font = new Font("宋体", 18F, FontStyle.Bold, GraphicsUnit.Point);
            comboBox4.FormattingEnabled = true;
            comboBox4.Location = new Point(3, 25);
            comboBox4.Name = "comboBox4";
            comboBox4.Size = new Size(116, 32);
            comboBox4.TabIndex = 1;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.CellBorderStyle = TableLayoutPanelCellBorderStyle.OutsetDouble;
            tableLayoutPanel3.ColumnCount = 2;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.Controls.Add(checkBox1, 0, 0);
            tableLayoutPanel3.Controls.Add(checkBox2, 1, 0);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(3, 25);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 1;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.Size = new Size(118, 41);
            tableLayoutPanel3.TabIndex = 0;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Checked = true;
            checkBox1.CheckState = CheckState.Checked;
            checkBox1.Dock = DockStyle.Fill;
            checkBox1.Font = new Font("Times New Roman", 14F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            checkBox1.Location = new Point(6, 6);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(48, 29);
            checkBox1.TabIndex = 0;
            checkBox1.Text = "L";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            checkBox2.AutoSize = true;
            checkBox2.Dock = DockStyle.Fill;
            checkBox2.Font = new Font("Times New Roman", 14F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            checkBox2.Location = new Point(63, 6);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new Size(49, 29);
            checkBox2.TabIndex = 1;
            checkBox2.Text = "S";
            checkBox2.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel4
            // 
            tableLayoutPanel4.ColumnCount = 4;
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel4.Controls.Add(Cal_btn, 1, 0);
            tableLayoutPanel4.Controls.Add(reset_btn, 2, 0);
            tableLayoutPanel4.Dock = DockStyle.Fill;
            tableLayoutPanel4.Location = new Point(3, 90);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 1;
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel4.Size = new Size(874, 46);
            tableLayoutPanel4.TabIndex = 1;
            // 
            // Cal_btn
            // 
            Cal_btn.Dock = DockStyle.Fill;
            Cal_btn.Font = new Font("隶书", 18F, FontStyle.Regular, GraphicsUnit.Point);
            Cal_btn.ForeColor = Color.Maroon;
            Cal_btn.Location = new Point(221, 3);
            Cal_btn.Name = "Cal_btn";
            Cal_btn.Size = new Size(212, 40);
            Cal_btn.TabIndex = 0;
            Cal_btn.Text = "计算";
            Cal_btn.UseVisualStyleBackColor = true;
            // 
            // reset_btn
            // 
            reset_btn.Dock = DockStyle.Fill;
            reset_btn.Font = new Font("隶书", 18F, FontStyle.Regular, GraphicsUnit.Point);
            reset_btn.ForeColor = Color.Maroon;
            reset_btn.Location = new Point(439, 3);
            reset_btn.Name = "reset_btn";
            reset_btn.Size = new Size(212, 40);
            reset_btn.TabIndex = 1;
            reset_btn.Text = "清除";
            reset_btn.UseVisualStyleBackColor = true;
            // 
            // Melt_composition
            // 
            Melt_composition.HeaderText = "组成";
            Melt_composition.Name = "Melt_composition";
            // 
            // k_name
            // 
            k_name.HeaderText = "基体";
            k_name.Name = "k_name";
            // 
            // solute_i
            // 
            solute_i.HeaderText = "溶质(i)";
            solute_i.Name = "solute_i";
            // 
            // activityCoefficient
            // 
            activityCoefficient.HeaderText = "lnγ_i";
            activityCoefficient.Name = "activityCoefficient";
            // 
            // Tem
            // 
            Tem.HeaderText = "T(K)";
            Tem.Name = "Tem";
            // 
            // state
            // 
            state.HeaderText = "State";
            state.Name = "state";
            // 
            // remark
            // 
            remark.HeaderText = "Remark";
            remark.Name = "remark";
            // 
            // ActivityCoefficientFm
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(880, 623);
            Controls.Add(splitContainer1);
            MainMenuStrip = menuStrip1;
            Name = "ActivityCoefficientFm";
            Text = "Activity Coefficients";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            compositions.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            i_groupBox3.ResumeLayout(false);
            Temp_groupBox4.ResumeLayout(false);
            groupBox5.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel3.PerformLayout();
            tableLayoutPanel4.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private DataGridView dataGridView1;
        private TableLayoutPanel tableLayoutPanel2;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem optionToolStripMenuItem;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem graphicToolStripMenuItem;
        private ToolStripMenuItem settingToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private TableLayoutPanel tableLayoutPanel1;
        private GroupBox groupBox5;
        private GroupBox Temp_groupBox4;
        private GroupBox i_groupBox3;
        private GroupBox groupBox2;
        private GroupBox compositions;
        private ComboBox comboBox4;
        private ComboBox comboBox3;
        private ComboBox comboBox2;
        private ComboBox comboBox1;
        private TableLayoutPanel tableLayoutPanel3;
        private CheckBox checkBox1;
        private CheckBox checkBox2;
        private TableLayoutPanel tableLayoutPanel4;
        private Button Cal_btn;
        private Button reset_btn;
        private DataGridViewTextBoxColumn Melt_composition;
        private DataGridViewTextBoxColumn k_name;
        private DataGridViewTextBoxColumn solute_i;
        private DataGridViewTextBoxColumn activityCoefficient;
        private DataGridViewTextBoxColumn Tem;
        private DataGridViewTextBoxColumn state;
        private DataGridViewTextBoxColumn remark;
    }
}