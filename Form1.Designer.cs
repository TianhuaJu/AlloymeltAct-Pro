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
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4F));
            tableLayoutPanel1.Controls.Add(Activity, 0, 0);
            tableLayoutPanel1.Controls.Add(ActivityCoeff, 1, 0);
            tableLayoutPanel1.Controls.Add(InteractionCoeff, 1, 1);
            tableLayoutPanel1.Controls.Add(ActivityCoefficientAtInfinitely, 2, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(2, 3, 2, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Size = new Size(689, 455);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // Activity
            // 
            Activity.BackColor = SystemColors.ButtonFace;
            Activity.Dock = DockStyle.Fill;
            Activity.Font = new Font("楷体", 18F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            Activity.Location = new Point(5, 6);
            Activity.Margin = new Padding(2, 3, 2, 3);
            Activity.Name = "Activity";
            Activity.Size = new Size(221, 217);
            Activity.TabIndex = 3;
            Activity.Text = "活度";
            Activity.UseVisualStyleBackColor = false;
            Activity.Click += Activity_Click;
            // 
            // ActivityCoeff
            // 
            ActivityCoeff.BackColor = Color.Cyan;
            ActivityCoeff.Dock = DockStyle.Fill;
            ActivityCoeff.Font = new Font("楷体", 18F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            ActivityCoeff.Location = new Point(233, 6);
            ActivityCoeff.Margin = new Padding(2, 3, 2, 3);
            ActivityCoeff.Name = "ActivityCoeff";
            ActivityCoeff.Size = new Size(221, 217);
            ActivityCoeff.TabIndex = 1;
            ActivityCoeff.Text = "活度系数";
            ActivityCoeff.UseVisualStyleBackColor = false;
            ActivityCoeff.Click += ActivityCoeff_Click;
            // 
            // InteractionCoeff
            // 
            InteractionCoeff.BackColor = Color.Cyan;
            InteractionCoeff.Dock = DockStyle.Fill;
            InteractionCoeff.Font = new Font("楷体", 18F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            InteractionCoeff.Location = new Point(233, 232);
            InteractionCoeff.Margin = new Padding(2, 3, 2, 3);
            InteractionCoeff.Name = "InteractionCoeff";
            InteractionCoeff.Size = new Size(221, 217);
            InteractionCoeff.TabIndex = 2;
            InteractionCoeff.Text = "活度相互作用系数";
            InteractionCoeff.UseVisualStyleBackColor = false;
            InteractionCoeff.Click += InteractionCoeff_Click;
            // 
            // ActivityCoefficientAtInfinitely
            // 
            ActivityCoefficientAtInfinitely.BackColor = Color.Cyan;
            ActivityCoefficientAtInfinitely.Dock = DockStyle.Fill;
            ActivityCoefficientAtInfinitely.Font = new Font("楷体", 18F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
            ActivityCoefficientAtInfinitely.ForeColor = SystemColors.ActiveCaptionText;
            ActivityCoefficientAtInfinitely.Location = new Point(461, 6);
            ActivityCoefficientAtInfinitely.Margin = new Padding(2, 3, 2, 3);
            ActivityCoefficientAtInfinitely.Name = "ActivityCoefficientAtInfinitely";
            ActivityCoefficientAtInfinitely.Size = new Size(223, 217);
            ActivityCoefficientAtInfinitely.TabIndex = 4;
            ActivityCoefficientAtInfinitely.Text = "无限稀活度系数";
            ActivityCoefficientAtInfinitely.UseVisualStyleBackColor = false;
            ActivityCoefficientAtInfinitely.Click += ActivityCoefficientAtInfinitely_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(689, 455);
            Controls.Add(tableLayoutPanel1);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Margin = new Padding(2, 3, 2, 3);
            Name = "Form1";
            Text = "AlloymeltActivity Pro";
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