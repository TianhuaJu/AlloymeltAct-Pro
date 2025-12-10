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
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            splitContainer1 = new SplitContainer();
            tableLayoutPanel2 = new TableLayoutPanel();
            tableLayoutPanel1 = new TableLayoutPanel();
            groupBox5 = new GroupBox();
            tableLayoutPanel3 = new TableLayoutPanel();
            checkBox1 = new CheckBox();
            checkBox2 = new CheckBox();
            compositions = new GroupBox();
            alloy_comboBox1 = new ComboBox();
            Temp_groupBox4 = new GroupBox();
            temp_comboBox4 = new ComboBox();
            i_groupBox3 = new GroupBox();
            i_comboBox3 = new ComboBox();
            groupBox2 = new GroupBox();
            k_comboBox2 = new ComboBox();
            tableLayoutPanel4 = new TableLayoutPanel();
            Cal_btn = new Button();
            reset_btn = new Button();
            menuStrip1 = new MenuStrip();
            optionToolStripMenuItem = new ToolStripMenuItem();
            saveToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            graphicToolStripMenuItem = new ToolStripMenuItem();
            settingToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            dataGridView1 = new DataGridView();
            k_name = new DataGridViewTextBoxColumn();
            Melt_composition = new DataGridViewTextBoxColumn();
            solute_i = new DataGridViewTextBoxColumn();
            acf_wagner = new DataGridViewTextBoxColumn();
            activityCoefficient = new DataGridViewTextBoxColumn();
            acf_elloit = new DataGridViewTextBoxColumn();
            Tem = new DataGridViewTextBoxColumn();
            state = new DataGridViewTextBoxColumn();
            remark = new DataGridViewTextBoxColumn();
            toolTip1 = new ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            groupBox5.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            compositions.SuspendLayout();
            Temp_groupBox4.SuspendLayout();
            i_groupBox3.SuspendLayout();
            groupBox2.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
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
            splitContainer1.Size = new Size(1200, 772);
            splitContainer1.SplitterDistance = 178;
            splitContainer1.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(tableLayoutPanel1, 0, 0);
            tableLayoutPanel2.Controls.Add(tableLayoutPanel4, 0, 1);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(0, 28);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 64.7482F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 35.2517967F));
            tableLayoutPanel2.Size = new Size(1110, 150);
            tableLayoutPanel2.TabIndex = 1;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.CellBorderStyle = TableLayoutPanelCellBorderStyle.InsetDouble;
            tableLayoutPanel1.ColumnCount = 5;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            tableLayoutPanel1.Controls.Add(groupBox5, 4, 0);
            tableLayoutPanel1.Controls.Add(compositions, 1, 0);
            tableLayoutPanel1.Controls.Add(Temp_groupBox4, 3, 0);
            tableLayoutPanel1.Controls.Add(i_groupBox3, 2, 0);
            tableLayoutPanel1.Controls.Add(groupBox2, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(3, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1104, 91);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(tableLayoutPanel3);
            groupBox5.Dock = DockStyle.Fill;
            groupBox5.Font = new Font("Times New Roman", 14F, FontStyle.Bold, GraphicsUnit.Point);
            groupBox5.Location = new Point(938, 6);
            groupBox5.Name = "groupBox5";
            groupBox5.Size = new Size(160, 79);
            groupBox5.TabIndex = 5;
            groupBox5.TabStop = false;
            groupBox5.Text = "State";
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
            tableLayoutPanel3.Size = new Size(154, 51);
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
            checkBox1.Size = new Size(66, 39);
            checkBox1.TabIndex = 0;
            checkBox1.Text = "L";
            toolTip1.SetToolTip(checkBox1, "液态");
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.Click += checkBox1_Click;
            // 
            // checkBox2
            // 
            checkBox2.AutoSize = true;
            checkBox2.Dock = DockStyle.Fill;
            checkBox2.Font = new Font("Times New Roman", 14F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            checkBox2.Location = new Point(81, 6);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new Size(67, 39);
            checkBox2.TabIndex = 1;
            checkBox2.Text = "S";
            toolTip1.SetToolTip(checkBox2, "固态");
            checkBox2.UseVisualStyleBackColor = true;
            checkBox2.Click += checkBox2_Click;
            // 
            // compositions
            // 
            compositions.Controls.Add(alloy_comboBox1);
            compositions.Dock = DockStyle.Fill;
            compositions.Font = new Font("宋体", 14F, FontStyle.Bold, GraphicsUnit.Point);
            compositions.Location = new Point(171, 6);
            compositions.Name = "compositions";
            compositions.Size = new Size(428, 79);
            compositions.TabIndex = 2;
            compositions.TabStop = false;
            compositions.Text = "合金元素组成(AxByCz...)";
            toolTip1.SetToolTip(compositions, "不包含基体元素的合金组成，");
            // 
            // alloy_comboBox1
            // 
            alloy_comboBox1.Dock = DockStyle.Fill;
            alloy_comboBox1.Font = new Font("宋体", 18F, FontStyle.Bold, GraphicsUnit.Point);
            alloy_comboBox1.FormattingEnabled = true;
            alloy_comboBox1.Location = new Point(3, 25);
            alloy_comboBox1.Name = "alloy_comboBox1";
            alloy_comboBox1.Size = new Size(422, 32);
            alloy_comboBox1.TabIndex = 0;
            toolTip1.SetToolTip(alloy_comboBox1, "不包含基体元素的合金组成，\r\nAxByCz表示xA=x/(1+x+y+z……)\r\n");
            // 
            // Temp_groupBox4
            // 
            Temp_groupBox4.Controls.Add(temp_comboBox4);
            Temp_groupBox4.Dock = DockStyle.Fill;
            Temp_groupBox4.Font = new Font("宋体", 14F, FontStyle.Bold, GraphicsUnit.Point);
            Temp_groupBox4.Location = new Point(773, 6);
            Temp_groupBox4.Name = "Temp_groupBox4";
            Temp_groupBox4.Size = new Size(156, 79);
            Temp_groupBox4.TabIndex = 4;
            Temp_groupBox4.TabStop = false;
            Temp_groupBox4.Text = "温度(K)";
            // 
            // temp_comboBox4
            // 
            temp_comboBox4.Dock = DockStyle.Fill;
            temp_comboBox4.Font = new Font("宋体", 18F, FontStyle.Bold, GraphicsUnit.Point);
            temp_comboBox4.FormattingEnabled = true;
            temp_comboBox4.Items.AddRange(new object[] { "1873", "1273" });
            temp_comboBox4.Location = new Point(3, 25);
            temp_comboBox4.Name = "temp_comboBox4";
            temp_comboBox4.Size = new Size(150, 32);
            temp_comboBox4.TabIndex = 3;
            toolTip1.SetToolTip(temp_comboBox4, "熔体的温度，单位K");
            // 
            // i_groupBox3
            // 
            i_groupBox3.Controls.Add(i_comboBox3);
            i_groupBox3.Dock = DockStyle.Fill;
            i_groupBox3.Font = new Font("宋体", 14F, FontStyle.Bold, GraphicsUnit.Point);
            i_groupBox3.Location = new Point(608, 6);
            i_groupBox3.Name = "i_groupBox3";
            i_groupBox3.Size = new Size(156, 79);
            i_groupBox3.TabIndex = 3;
            i_groupBox3.TabStop = false;
            i_groupBox3.Text = "溶质(i)";
            // 
            // i_comboBox3
            // 
            i_comboBox3.Dock = DockStyle.Fill;
            i_comboBox3.DropDownHeight = 110;
            i_comboBox3.Font = new Font("宋体", 18F, FontStyle.Bold, GraphicsUnit.Point);
            i_comboBox3.FormattingEnabled = true;
            i_comboBox3.IntegralHeight = false;
            i_comboBox3.Location = new Point(3, 25);
            i_comboBox3.Name = "i_comboBox3";
            i_comboBox3.Size = new Size(150, 32);
            i_comboBox3.TabIndex = 2;
            toolTip1.SetToolTip(i_comboBox3, "待求活度系数的组元");
            i_comboBox3.Click += i_comboBox3_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(k_comboBox2);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Font = new Font("宋体", 14F, FontStyle.Bold, GraphicsUnit.Point);
            groupBox2.Location = new Point(6, 6);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(156, 79);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "基体(k)";
            // 
            // k_comboBox2
            // 
            k_comboBox2.Dock = DockStyle.Fill;
            k_comboBox2.Font = new Font("宋体", 18F, FontStyle.Bold, GraphicsUnit.Point);
            k_comboBox2.FormattingEnabled = true;
            k_comboBox2.Location = new Point(3, 25);
            k_comboBox2.Name = "k_comboBox2";
            k_comboBox2.Size = new Size(150, 32);
            k_comboBox2.TabIndex = 0;
            toolTip1.SetToolTip(k_comboBox2, "合金基体");
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
            tableLayoutPanel4.Location = new Point(3, 100);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 1;
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel4.Size = new Size(1104, 47);
            tableLayoutPanel4.TabIndex = 1;
            // 
            // Cal_btn
            // 
            Cal_btn.Dock = DockStyle.Fill;
            Cal_btn.Font = new Font("隶书", 18F, FontStyle.Regular, GraphicsUnit.Point);
            Cal_btn.ForeColor = Color.Maroon;
            Cal_btn.Location = new Point(279, 3);
            Cal_btn.Name = "Cal_btn";
            Cal_btn.Size = new Size(270, 41);
            Cal_btn.TabIndex = 0;
            Cal_btn.Text = "计算";
            Cal_btn.UseVisualStyleBackColor = true;
            Cal_btn.Click += Cal_btn_Click;
            // 
            // reset_btn
            // 
            reset_btn.Dock = DockStyle.Fill;
            reset_btn.Font = new Font("隶书", 18F, FontStyle.Regular, GraphicsUnit.Point);
            reset_btn.ForeColor = Color.Maroon;
            reset_btn.Location = new Point(555, 3);
            reset_btn.Name = "reset_btn";
            reset_btn.Size = new Size(270, 41);
            reset_btn.TabIndex = 1;
            reset_btn.Text = "清除";
            reset_btn.UseVisualStyleBackColor = true;
            reset_btn.Click += reset_btn_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { optionToolStripMenuItem, graphicToolStripMenuItem, settingToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1110, 28);
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
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(112, 24);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(112, 24);
            exitToolStripMenuItem.Text = "Exit";
            // 
            // graphicToolStripMenuItem
            // 
            graphicToolStripMenuItem.Name = "graphicToolStripMenuItem";
            graphicToolStripMenuItem.Size = new Size(77, 24);
            graphicToolStripMenuItem.Text = "Graphic";
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
            helpToolStripMenuItem.Click += helpToolStripMenuItem_Click;
            // 
            // dataGridView1
            // 
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.BorderStyle = BorderStyle.Fixed3D;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.Sunken;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { k_name, Melt_composition, solute_i, acf_wagner, activityCoefficient, acf_elloit, Tem, state, remark });
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = SystemColors.Window;
            dataGridViewCellStyle3.Font = new Font("Times New Roman", 14F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle3.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle3.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
            dataGridView1.DefaultCellStyle = dataGridViewCellStyle3;
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(0, 0);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.RowTemplate.Height = 29;
            dataGridView1.Size = new Size(1110, 441);
            dataGridView1.TabIndex = 0;
            // 
            // k_name
            // 
            dataGridViewCellStyle1.Format = "N3";
            k_name.DefaultCellStyle = dataGridViewCellStyle1;
            k_name.HeaderText = "基体";
            k_name.MinimumWidth = 6;
            k_name.Name = "k_name";
            // 
            // Melt_composition
            // 
            Melt_composition.FillWeight = 300F;
            Melt_composition.HeaderText = "组成";
            Melt_composition.MinimumWidth = 6;
            Melt_composition.Name = "Melt_composition";
            Melt_composition.ToolTipText = "AxByCz……，x表示包含基体元素的1mol熔体中组分A的组成";
            // 
            // solute_i
            // 
            solute_i.HeaderText = "溶质(i)";
            solute_i.MinimumWidth = 6;
            solute_i.Name = "solute_i";
            // 
            // acf_wagner
            //
            acf_wagner.HeaderText = "lnγ_i(Wagner)";
            acf_wagner.Name = "acf_wagner";
            acf_wagner.ToolTipText = "Wagner模型：采用Wagner一阶相互作用参数公式，适用于无限稀溶液";
            //
            // activityCoefficient
            //
            dataGridViewCellStyle2.NullValue = null;
            activityCoefficient.DefaultCellStyle = dataGridViewCellStyle2;
            activityCoefficient.HeaderText = "lnγ_i(Pelton)";
            activityCoefficient.MinimumWidth = 6;
            activityCoefficient.Name = "activityCoefficient";
            activityCoefficient.ToolTipText = "Pelton模型(Darken方法)：采用Pelton-Darken积分公式计算多元系活度系数，适用于稀溶液和浓溶液";
            //
            // acf_elloit
            //
            acf_elloit.HeaderText = "lnγ_i(Elliott)";
            acf_elloit.Name = "acf_elloit";
            acf_elloit.ToolTipText = "Elliott模型：采用Elliott统一活度相互作用参数公式，考虑溶质间相互作用";
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
            // remark
            // 
            remark.HeaderText = "Remark";
            remark.MinimumWidth = 6;
            remark.Name = "remark";
            // 
            // ActivityCoefficientFm
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1200, 800);
            Controls.Add(splitContainer1);
            MainMenuStrip = menuStrip1;
            Name = "ActivityCoefficientFm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Activity Coefficients";
            FormClosed += ActivityCoefficientFm_FormClosed;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            groupBox5.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel3.PerformLayout();
            compositions.ResumeLayout(false);
            Temp_groupBox4.ResumeLayout(false);
            i_groupBox3.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            tableLayoutPanel4.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
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
        private ComboBox temp_comboBox4;
        private ComboBox i_comboBox3;
        private ComboBox k_comboBox2;
        private ComboBox alloy_comboBox1;
        private TableLayoutPanel tableLayoutPanel3;
        private CheckBox checkBox1;
        private CheckBox checkBox2;
        private TableLayoutPanel tableLayoutPanel4;
        private Button Cal_btn;
        private Button reset_btn;
        private ToolTip toolTip1;
        private DataGridViewTextBoxColumn k_name;
        private DataGridViewTextBoxColumn Melt_composition;
        private DataGridViewTextBoxColumn solute_i;
        private DataGridViewTextBoxColumn acf_wagner;
        private DataGridViewTextBoxColumn activityCoefficient;
        private DataGridViewTextBoxColumn acf_elloit;
        private DataGridViewTextBoxColumn Tem;
        private DataGridViewTextBoxColumn state;
        private DataGridViewTextBoxColumn remark;
    }
}