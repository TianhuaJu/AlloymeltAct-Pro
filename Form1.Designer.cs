namespace AlloyAct_Pro
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanel1 = new TableLayoutPanel();
            Activity = new Button();
            ActivityCoeff = new Button();
            InteractionCoeff = new Button();
            ActivityCoefficientAtInfinitely = new Button();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.BackColor = Color.FromArgb(128, 255, 255);
            tableLayoutPanel1.BackgroundImage = Properties.Resources.R;
            tableLayoutPanel1.CellBorderStyle = TableLayoutPanelCellBorderStyle.OutsetDouble;
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
            tableLayoutPanel1.Controls.Add(Activity, 0, 0);
            tableLayoutPanel1.Controls.Add(ActivityCoeff, 1, 0);
            tableLayoutPanel1.Controls.Add(InteractionCoeff, 1, 1);
            tableLayoutPanel1.Controls.Add(ActivityCoefficientAtInfinitely, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(3, 4, 3, 4);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Size = new Size(800, 600);
            tableLayoutPanel1.TabIndex = 0;
            //
            // Activity
            //
            Activity.BackColor = Color.FromArgb(128, 255, 255);
            Activity.Dock = DockStyle.Fill;
            Activity.FlatStyle = FlatStyle.Popup;
            Activity.Font = new Font("微软雅黑", 24F, FontStyle.Bold, GraphicsUnit.Point);
            Activity.Location = new Point(6, 7);
            Activity.Margin = new Padding(8);
            Activity.Name = "Activity";
            Activity.Size = new Size(306, 293);
            Activity.TabIndex = 3;
            Activity.Text = "活  度";
            Activity.UseVisualStyleBackColor = false;
            Activity.Click += Activity_Click;
            //
            // ActivityCoeff
            //
            ActivityCoeff.BackColor = Color.FromArgb(100, 200, 255);
            ActivityCoeff.Dock = DockStyle.Fill;
            ActivityCoeff.FlatStyle = FlatStyle.Popup;
            ActivityCoeff.Font = new Font("微软雅黑", 24F, FontStyle.Bold, GraphicsUnit.Point);
            ActivityCoeff.Location = new Point(321, 7);
            ActivityCoeff.Margin = new Padding(8);
            ActivityCoeff.Name = "ActivityCoeff";
            ActivityCoeff.Size = new Size(306, 293);
            ActivityCoeff.TabIndex = 1;
            ActivityCoeff.Text = "活度系数";
            ActivityCoeff.UseVisualStyleBackColor = false;
            ActivityCoeff.Click += ActivityCoeff_Click;
            //
            // InteractionCoeff
            //
            InteractionCoeff.BackColor = Color.FromArgb(100, 220, 180);
            InteractionCoeff.Dock = DockStyle.Fill;
            InteractionCoeff.FlatStyle = FlatStyle.Popup;
            InteractionCoeff.Font = new Font("微软雅黑", 20F, FontStyle.Bold, GraphicsUnit.Point);
            InteractionCoeff.Location = new Point(321, 311);
            InteractionCoeff.Margin = new Padding(8);
            InteractionCoeff.Name = "InteractionCoeff";
            InteractionCoeff.Size = new Size(306, 293);
            InteractionCoeff.TabIndex = 2;
            InteractionCoeff.Text = "活度相互\r\n作用系数";
            InteractionCoeff.UseVisualStyleBackColor = false;
            InteractionCoeff.Click += InteractionCoeff_Click;
            //
            // ActivityCoefficientAtInfinitely
            //
            ActivityCoefficientAtInfinitely.BackColor = Color.FromArgb(100, 200, 255);
            ActivityCoefficientAtInfinitely.Dock = DockStyle.Fill;
            ActivityCoefficientAtInfinitely.FlatStyle = FlatStyle.Popup;
            ActivityCoefficientAtInfinitely.Font = new Font("微软雅黑", 20F, FontStyle.Bold, GraphicsUnit.Point);
            ActivityCoefficientAtInfinitely.ForeColor = SystemColors.ActiveCaptionText;
            ActivityCoefficientAtInfinitely.Location = new Point(6, 311);
            ActivityCoefficientAtInfinitely.Margin = new Padding(8);
            ActivityCoefficientAtInfinitely.Name = "ActivityCoefficientAtInfinitely";
            ActivityCoefficientAtInfinitely.Size = new Size(306, 293);
            ActivityCoefficientAtInfinitely.TabIndex = 4;
            ActivityCoefficientAtInfinitely.Text = "无限稀\r\n活度系数";
            ActivityCoefficientAtInfinitely.UseVisualStyleBackColor = false;
            ActivityCoefficientAtInfinitely.Click += ActivityCoefficientAtInfinitely_Click;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(240, 248, 255);
            ClientSize = new Size(800, 600);
            Controls.Add(tableLayoutPanel1);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "合金熔体活度计算器 Pro";
            tableLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private Button ActivityCoeff;
        private Button InteractionCoeff;
        private Button Activity;
        private Button ActivityCoefficientAtInfinitely;
    }
}