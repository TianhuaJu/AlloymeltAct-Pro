namespace AlloyAct_Pro.Controls
{
    /// <summary>
    /// 用户输入熔化焓对话框。
    /// 当 TDB 数据库中找不到某元素的纯物质数据时弹出，
    /// 要求用户手动输入 ΔHf (kJ/mol)。
    /// 支持纯数字输入（如 "13.81"）和含温度的表达式（如 "-1500/T+2.5"）。
    /// </summary>
    internal class FusionEnthalpyInputDialog : Form
    {
        private TextBox txtInput;
        private Label lblMessage;
        private Label lblHint;
        private Label lblPreview;
        private Button btnOK;
        private Button btnCancel;

        /// <summary>
        /// 用户输入的 ΔHf 值 (kJ/mol)，已根据 Tm 求值。
        /// 若用户取消则为 NaN。
        /// </summary>
        public double ResultDeltaHf { get; private set; } = double.NaN;

        /// <summary>
        /// 用户输入的原始文本
        /// </summary>
        public string RawInput { get; private set; } = string.Empty;

        private readonly string _elementName;
        private readonly double _Tm;

        /// <summary>
        /// 创建熔化焓输入对话框
        /// </summary>
        /// <param name="elementName">缺失数据的元素符号</param>
        /// <param name="Tm">该元素的熔点 (K)</param>
        public FusionEnthalpyInputDialog(string elementName, double Tm)
        {
            _elementName = elementName;
            _Tm = Tm;
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            // Form 基本设置
            Text = $"Fusion Enthalpy Required — {_elementName}";
            ClientSize = new Size(520, 300);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            BackColor = Color.White;
            ShowInTaskbar = false;
            AcceptButton = null; // 由自定义逻辑处理
            Font = AppTheme.BodyFont;

            int left = 24;
            int contentLeft = 24;
            int contentWidth = ClientSize.Width - contentLeft * 2;
            int y = 20;

            // 警告图标
            var iconBox = new PictureBox();
            iconBox.Image = SystemIcons.Warning.ToBitmap();
            iconBox.SizeMode = PictureBoxSizeMode.Zoom;
            iconBox.Size = new Size(40, 40);
            iconBox.Location = new Point(left, y);
            Controls.Add(iconBox);

            // 消息标签
            lblMessage = new Label();
            lblMessage.Text = $"The fusion enthalpy (ΔHf) for element \"{_elementName}\" is not available " +
                              $"in the TDB database.  Tm = {_Tm:F1} K\n\n" +
                              $"Please enter ΔHf (kJ/mol) to continue the calculation:";
            lblMessage.Font = new Font("Microsoft YaHei UI", 10F);
            lblMessage.ForeColor = Color.FromArgb(44, 62, 80);
            lblMessage.AutoSize = false;
            lblMessage.Size = new Size(contentWidth - 56, 80);
            lblMessage.Location = new Point(left + 52, y);
            Controls.Add(lblMessage);

            y += 90;

            // 输入标签
            var lblInput = new Label();
            lblInput.Text = "ΔHf (kJ/mol):";
            lblInput.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            lblInput.ForeColor = Color.FromArgb(44, 62, 80);
            lblInput.AutoSize = true;
            lblInput.Location = new Point(contentLeft, y + 4);
            Controls.Add(lblInput);

            // 输入框
            txtInput = new TextBox();
            txtInput.Font = new Font("Consolas", 13F);
            txtInput.Location = new Point(contentLeft + 120, y);
            txtInput.Size = new Size(contentWidth - 120, 28);
            txtInput.PlaceholderText = "e.g.  13.81  or  -16736/T+9.47";
            txtInput.TextChanged += TxtInput_TextChanged;
            txtInput.KeyDown += TxtInput_KeyDown;
            Controls.Add(txtInput);

            y += 38;

            // 提示
            lblHint = new Label();
            lblHint.Text = "Accepts: plain number (kJ/mol)   or   expression  a/T+b   or   a*T+b";
            lblHint.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Italic);
            lblHint.ForeColor = Color.FromArgb(140, 140, 140);
            lblHint.AutoSize = true;
            lblHint.Location = new Point(contentLeft, y);
            Controls.Add(lblHint);

            y += 28;

            // 实时预览
            lblPreview = new Label();
            lblPreview.Text = "";
            lblPreview.Font = new Font("Microsoft YaHei UI", 10F);
            lblPreview.ForeColor = Color.FromArgb(41, 128, 185);
            lblPreview.AutoSize = false;
            lblPreview.Size = new Size(contentWidth, 36);
            lblPreview.Location = new Point(contentLeft, y);
            Controls.Add(lblPreview);

            y += 44;

            // 按钮
            btnOK = new Button();
            btnOK.Text = "OK";
            btnOK.Size = new Size(110, 38);
            btnOK.Location = new Point(ClientSize.Width - 240, y);
            btnOK.Enabled = false;
            AppTheme.StyleCalcButton(btnOK);
            btnOK.Click += BtnOK_Click;
            Controls.Add(btnOK);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(110, 38);
            btnCancel.Location = new Point(ClientSize.Width - 120, y);
            AppTheme.StyleResetButton(btnCancel);
            btnCancel.Click += BtnCancel_Click;
            Controls.Add(btnCancel);

            // 对话框打开时聚焦到输入框
            Shown += (s, e) => txtInput.Focus();
        }

        private void TxtInput_TextChanged(object sender, EventArgs e)
        {
            string input = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                lblPreview.Text = "";
                lblPreview.ForeColor = Color.FromArgb(41, 128, 185);
                btnOK.Enabled = false;
                return;
            }

            double value = LiquidusCalculator.ParseDeltaHfExpression(input, _Tm);

            if (!double.IsNaN(value))
            {
                if (value > 0)
                {
                    lblPreview.Text = $"→ ΔHf({_elementName}) = {value:F4} kJ/mol  (at Tm = {_Tm:F1} K)";
                    lblPreview.ForeColor = Color.FromArgb(39, 174, 96);
                    btnOK.Enabled = true;
                }
                else
                {
                    lblPreview.Text = $"→ {value:F4} kJ/mol — ΔHf must be positive";
                    lblPreview.ForeColor = Color.OrangeRed;
                    btnOK.Enabled = false;
                }
            }
            else
            {
                lblPreview.Text = "Invalid format. Use a number or expression like a/T+b";
                lblPreview.ForeColor = Color.OrangeRed;
                btnOK.Enabled = false;
            }
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && btnOK.Enabled)
            {
                e.SuppressKeyPress = true;
                BtnOK_Click(sender, e);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                BtnCancel_Click(sender, e);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            string input = txtInput.Text.Trim();
            double value = LiquidusCalculator.ParseDeltaHfExpression(input, _Tm);

            if (double.IsNaN(value) || value <= 0)
            {
                MessageBox.Show(
                    "Please enter a valid positive value for ΔHf.\n" +
                    "Supported formats:\n" +
                    "  • Number: 13.81\n" +
                    "  • Expression: -16736/T+9.47  or  0.005*T+2.3",
                    "Invalid Input",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtInput.Focus();
                return;
            }

            ResultDeltaHf = value;
            RawInput = input;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            ResultDeltaHf = double.NaN;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
