using AlloyAct_Pro.LLM;
using System.Drawing.Drawing2D;

namespace AlloyAct_Pro.Controls
{
    public class ChatPanel : UserControl
    {
        public string PageTitle => "AI Assistant";

        private ChatAgent? _agent;
        private CancellationTokenSource? _cts;

        // Config controls
        private ComboBox cboProvider = null!;
        private ComboBox cboModel = null!;
        private TextBox txtApiKey = null!;
        private Button btnConnect = null!;
        private Label lblStatus = null!;

        // Chat area
        private Panel chatContainer = null!;
        private FlowLayoutPanel messagesPanel = null!;
        private ScrollableControl scrollArea = null!;

        // Input area
        private TextBox txtInput = null!;
        private Button btnSend = null!;
        private Button btnClear = null!;

        public ChatPanel()
        {
            InitializeUI();
            AddWelcomeMessage();
        }

        #region UI Setup

        private void InitializeUI()
        {
            this.BackColor = AppTheme.ContentBg;
            this.Padding = new Padding(16);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // Config
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Chat
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));  // Input

            // === Config Row ===
            var configPanel = CreateConfigPanel();
            mainLayout.Controls.Add(configPanel, 0, 0);

            // === Chat Area ===
            chatContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            messagesPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(4),
                BackColor = Color.Transparent
            };
            messagesPanel.SizeChanged += (s, e) =>
            {
                // Auto-scroll to bottom
                chatContainer.ScrollControlIntoView(messagesPanel);
            };

            chatContainer.Controls.Add(messagesPanel);
            mainLayout.Controls.Add(chatContainer, 0, 1);

            // === Input Area ===
            var inputPanel = CreateInputPanel();
            mainLayout.Controls.Add(inputPanel, 0, 2);

            this.Controls.Add(mainLayout);
        }

        private Panel CreateConfigPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Height = 55 };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                Padding = new Padding(0, 4, 0, 4)
            };

            // Provider
            var lblProvider = new Label
            {
                Text = "提供商:",
                Font = AppTheme.BodyFont,
                AutoSize = true,
                Margin = new Padding(0, 8, 4, 0)
            };

            cboProvider = new ComboBox
            {
                Font = AppTheme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
                Margin = new Padding(0, 4, 8, 0)
            };
            cboProvider.Items.AddRange(ProviderRegistry.GetProviderNames());
            cboProvider.SelectedIndex = 0;
            cboProvider.SelectedIndexChanged += (s, e) => UpdateModelList();

            // Model
            var lblModel = new Label
            {
                Text = "模型:",
                Font = AppTheme.BodyFont,
                AutoSize = true,
                Margin = new Padding(0, 8, 4, 0)
            };

            cboModel = new ComboBox
            {
                Font = AppTheme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDown,
                Width = 200,
                Margin = new Padding(0, 4, 8, 0)
            };
            UpdateModelList();

            // API Key
            var lblKey = new Label
            {
                Text = "API Key:",
                Font = AppTheme.BodyFont,
                AutoSize = true,
                Margin = new Padding(0, 8, 4, 0)
            };

            txtApiKey = new TextBox
            {
                Font = AppTheme.BodyFont,
                UseSystemPasswordChar = true,
                Width = 180,
                Margin = new Padding(0, 4, 8, 0)
            };
            txtApiKey.PlaceholderText = "本地模型无需填写";

            // Connect button
            btnConnect = new Button
            {
                Text = "连接",
                Font = AppTheme.CalcBtnFont,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 32),
                Margin = new Padding(0, 4, 8, 0),
                Cursor = Cursors.Hand
            };
            btnConnect.FlatAppearance.BorderSize = 0;
            btnConnect.Click += BtnConnect_Click;

            // Status
            lblStatus = new Label
            {
                Text = "未连接",
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(231, 76, 60),
                AutoSize = true,
                Margin = new Padding(4, 8, 0, 0)
            };

            flow.Controls.AddRange(new Control[] {
                lblProvider, cboProvider, lblModel, cboModel,
                lblKey, txtApiKey, btnConnect, lblStatus
            });

            panel.Controls.Add(flow);
            return panel;
        }

        private Panel CreateInputPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 4, 0, 0)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

            txtInput = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 12F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "输入您的问题... (Ctrl+Enter 发送)"
            };
            txtInput.KeyDown += TxtInput_KeyDown;

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(4, 0, 0, 0)
            };

            btnSend = new Button
            {
                Text = "发送",
                Font = AppTheme.CalcBtnFont,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 40),
                Enabled = false,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 4)
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += BtnSend_Click;

            btnClear = new Button
            {
                Text = "清空",
                Font = AppTheme.BodyFont,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 34),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += BtnClear_Click;

            btnPanel.Controls.Add(btnSend);
            btnPanel.Controls.Add(btnClear);

            table.Controls.Add(txtInput, 0, 0);
            table.Controls.Add(btnPanel, 1, 0);

            panel.Controls.Add(table);
            return panel;
        }

        #endregion

        #region Message Bubbles

        private void AddWelcomeMessage()
        {
            var welcome = CreateBubble(
                "欢迎使用 AI 热力学计算助手！\n\n" +
                "您可以用自然语言描述计算需求，例如：\n" +
                "• 计算Al-5%Cu合金的液相线温度\n" +
                "• 铝中每增加1%铜，熔点会降低多少？\n" +
                "• 计算Fe-Mn-Si合金在1873K下Mn的活度\n" +
                "• 获取Fe元素的热力学性质\n" +
                "• 绘制Cu含量对Al合金液相线温度的影响图\n\n" +
                "请先在上方配置 LLM 后端并点击「连接」。",
                Color.FromArgb(245, 245, 245), Color.FromArgb(100, 100, 100),
                "系统", Color.FromArgb(100, 100, 100));
            messagesPanel.Controls.Add(welcome);
        }

        private void AddUserMessage(string text)
        {
            var bubble = CreateBubble(text,
                Color.FromArgb(232, 244, 248), Color.FromArgb(44, 62, 80),
                "你", Color.FromArgb(44, 62, 80));
            messagesPanel.Controls.Add(bubble);
            ScrollToBottom();
        }

        private void AddAssistantMessage(string text)
        {
            var bubble = CreateBubble(text,
                Color.FromArgb(240, 248, 232), Color.FromArgb(44, 62, 80),
                "助手", Color.FromArgb(39, 174, 96));
            messagesPanel.Controls.Add(bubble);
            ScrollToBottom();
        }

        private void AddToolCallBubble(string toolName, string arguments)
        {
            string argsDisplay = arguments;
            if (argsDisplay.Length > 300) argsDisplay = argsDisplay.Substring(0, 300) + "...";

            // Pretty print
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(arguments);
                argsDisplay = System.Text.Json.JsonSerializer.Serialize(doc, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                if (argsDisplay.Length > 300) argsDisplay = argsDisplay.Substring(0, 300) + "...";
            }
            catch { }

            var bubble = CreateBubble($"调用: {toolName}\n{argsDisplay}",
                Color.FromArgb(248, 240, 255), Color.FromArgb(85, 85, 85),
                "工具", Color.FromArgb(142, 68, 173),
                useMonoFont: true, smaller: true);
            messagesPanel.Controls.Add(bubble);
            ScrollToBottom();
        }

        private void AddSystemMessage(string text)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(136, 136, 136),
                BackColor = Color.FromArgb(240, 240, 240),
                AutoSize = false,
                Width = messagesPanel.ClientSize.Width - 40,
                MaximumSize = new Size(messagesPanel.ClientSize.Width - 40, 0),
                AutoEllipsis = false,
                Padding = new Padding(10, 5, 10, 5),
                Margin = new Padding(10, 3, 10, 3)
            };
            // Auto-size height
            using var g = lbl.CreateGraphics();
            var sz = g.MeasureString(text, lbl.Font, lbl.Width - 20);
            lbl.Height = (int)sz.Height + 16;

            messagesPanel.Controls.Add(lbl);
            ScrollToBottom();
        }

        private void AddChartBubble(Dictionary<string, object> chartData)
        {
            try
            {
                var chartPanel = new Panel
                {
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Width = Math.Min(messagesPanel.ClientSize.Width - 30, 700),
                    Height = 360,
                    Margin = new Padding(10, 5, 10, 5),
                    Padding = new Padding(8)
                };

                var pb = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom
                };

                // Render chart using GDI+
                var bmp = RenderChart(chartData, chartPanel.Width - 16, chartPanel.Height - 16);
                pb.Image = bmp;

                chartPanel.Controls.Add(pb);
                messagesPanel.Controls.Add(chartPanel);
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                AddSystemMessage($"图表渲染失败: {ex.Message}");
            }
        }

        private Panel CreateBubble(string text, Color bgColor, Color textColor,
            string roleText, Color roleColor, bool useMonoFont = false, bool smaller = false)
        {
            int panelWidth = messagesPanel.ClientSize.Width - 30;
            if (panelWidth < 200) panelWidth = 600;

            var bubble = new Panel
            {
                BackColor = bgColor,
                Width = panelWidth,
                Margin = new Padding(5, 3, 5, 3),
                Padding = new Padding(12, 6, 12, 6),
                AutoSize = false
            };
            // Rounded corners via region
            bubble.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(200, 200, 200), 1);
                var rect = new Rectangle(0, 0, bubble.Width - 1, bubble.Height - 1);
                using var path = RoundedRect(rect, 8);
                e.Graphics.DrawPath(pen, path);
            };

            var roleLabel = new Label
            {
                Text = roleText,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = roleColor,
                AutoSize = true,
                Location = new Point(12, 6),
                BackColor = Color.Transparent
            };

            var contentFont = useMonoFont
                ? new Font("Consolas", smaller ? 9F : 10F)
                : new Font("Microsoft YaHei UI", smaller ? 10F : 11F);

            var contentLabel = new Label
            {
                Text = text,
                Font = contentFont,
                ForeColor = textColor,
                AutoSize = false,
                Width = panelWidth - 30,
                MaximumSize = new Size(panelWidth - 30, 0),
                Location = new Point(12, 26),
                BackColor = Color.Transparent
            };

            // Calculate text height
            using var g = this.CreateGraphics();
            var size = g.MeasureString(text, contentFont, panelWidth - 30);
            contentLabel.Height = (int)size.Height + 8;
            bubble.Height = contentLabel.Height + 38;

            bubble.Controls.Add(roleLabel);
            bubble.Controls.Add(contentLabel);
            return bubble;
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void ScrollToBottom()
        {
            BeginInvoke(new Action(() =>
            {
                chatContainer.VerticalScroll.Value = chatContainer.VerticalScroll.Maximum;
                chatContainer.PerformLayout();
                chatContainer.ScrollControlIntoView(messagesPanel.Controls[messagesPanel.Controls.Count - 1]);
            }));
        }

        #endregion

        #region Chart Rendering (GDI+)

        private Bitmap RenderChart(Dictionary<string, object> chartData, int width, int height)
        {
            var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            var title = chartData["title"]?.ToString() ?? "";
            var xLabel = chartData["x_label"]?.ToString() ?? "";
            var yLabel = chartData["y_label"]?.ToString() ?? "";
            var chartType = chartData.ContainsKey("chart_type") ? chartData["chart_type"]?.ToString() ?? "line" : "line";
            var series = chartData["data_series"] as List<Dictionary<string, object>> ?? new();

            // Margins
            int ml = 70, mr = 20, mt = 40, mb = 50;
            var plotRect = new Rectangle(ml, mt, width - ml - mr, height - mt - mb);

            // Find data range
            double xMin = double.MaxValue, xMax = double.MinValue;
            double yMin = double.MaxValue, yMax = double.MinValue;
            foreach (var s in series)
            {
                var xv = s["x_values"] as List<double> ?? new();
                var yv = s["y_values"] as List<double> ?? new();
                foreach (var x in xv) { if (x < xMin) xMin = x; if (x > xMax) xMax = x; }
                foreach (var y in yv) { if (y < yMin) yMin = y; if (y > yMax) yMax = y; }
            }
            if (Math.Abs(xMax - xMin) < 1e-10) { xMin -= 1; xMax += 1; }
            if (Math.Abs(yMax - yMin) < 1e-10) { yMin -= 1; yMax += 1; }
            double yPad = (yMax - yMin) * 0.1;
            yMin -= yPad; yMax += yPad;

            // Draw grid
            using var gridPen = new Pen(Color.FromArgb(230, 230, 230), 1) { DashStyle = DashStyle.Dash };
            using var axisPen = new Pen(Color.FromArgb(100, 100, 100), 1);
            using var axisFont = new Font("Microsoft YaHei UI", 8F);
            using var titleFont = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            using var labelFont = new Font("Microsoft YaHei UI", 9F);

            // Axes
            g.DrawRectangle(axisPen, plotRect);

            // X ticks
            int nxTicks = 5;
            for (int i = 0; i <= nxTicks; i++)
            {
                double v = xMin + (xMax - xMin) * i / nxTicks;
                int px = plotRect.Left + (int)(plotRect.Width * i / (double)nxTicks);
                g.DrawLine(gridPen, px, plotRect.Top, px, plotRect.Bottom);
                var txt = v.ToString("G4");
                var sz = g.MeasureString(txt, axisFont);
                g.DrawString(txt, axisFont, Brushes.Gray, px - sz.Width / 2, plotRect.Bottom + 4);
            }

            // Y ticks
            int nyTicks = 5;
            for (int i = 0; i <= nyTicks; i++)
            {
                double v = yMin + (yMax - yMin) * i / nyTicks;
                int py = plotRect.Bottom - (int)(plotRect.Height * i / (double)nyTicks);
                g.DrawLine(gridPen, plotRect.Left, py, plotRect.Right, py);
                var txt = v.ToString("G4");
                var sz = g.MeasureString(txt, axisFont);
                g.DrawString(txt, axisFont, Brushes.Gray, plotRect.Left - sz.Width - 4, py - sz.Height / 2);
            }

            // Title
            var titleSz = g.MeasureString(title, titleFont);
            g.DrawString(title, titleFont, Brushes.Black, (width - titleSz.Width) / 2, 8);

            // X Label
            var xlSz = g.MeasureString(xLabel, labelFont);
            g.DrawString(xLabel, labelFont, Brushes.DimGray, (width - xlSz.Width) / 2, height - 22);

            // Y Label (rotated)
            var state = g.Save();
            g.TranslateTransform(14, height / 2f);
            g.RotateTransform(-90);
            var ylSz = g.MeasureString(yLabel, labelFont);
            g.DrawString(yLabel, labelFont, Brushes.DimGray, -ylSz.Width / 2, 0);
            g.Restore(state);

            // Plot data
            Color[] colors = {
                Color.FromArgb(41, 128, 185), Color.FromArgb(231, 76, 60),
                Color.FromArgb(39, 174, 96), Color.FromArgb(243, 156, 18),
                Color.FromArgb(142, 68, 173), Color.FromArgb(26, 188, 156)
            };

            int legendY = mt + 8;
            for (int si = 0; si < series.Count; si++)
            {
                var s = series[si];
                var xv = s["x_values"] as List<double> ?? new();
                var yv = s["y_values"] as List<double> ?? new();
                var name = s["name"]?.ToString() ?? $"Series {si + 1}";
                var color = colors[si % colors.Length];

                using var pen = new Pen(color, 2);
                using var brush = new SolidBrush(color);

                var points = new List<Point>();
                for (int i = 0; i < Math.Min(xv.Count, yv.Count); i++)
                {
                    int px = plotRect.Left + (int)((xv[i] - xMin) / (xMax - xMin) * plotRect.Width);
                    int py = plotRect.Bottom - (int)((yv[i] - yMin) / (yMax - yMin) * plotRect.Height);
                    px = Math.Max(plotRect.Left, Math.Min(plotRect.Right, px));
                    py = Math.Max(plotRect.Top, Math.Min(plotRect.Bottom, py));
                    points.Add(new Point(px, py));
                }

                if (points.Count > 1 && (chartType == "line" || chartType == "scatter"))
                {
                    if (chartType == "line")
                        g.DrawLines(pen, points.ToArray());
                    foreach (var pt in points)
                        g.FillEllipse(brush, pt.X - 3, pt.Y - 3, 6, 6);
                }
                else if (chartType == "bar" && points.Count > 0)
                {
                    int barW = Math.Max(4, plotRect.Width / (points.Count * (series.Count + 1)));
                    for (int i = 0; i < points.Count; i++)
                    {
                        int x = points[i].X - barW / 2 + si * (barW + 2);
                        int h = plotRect.Bottom - points[i].Y;
                        g.FillRectangle(brush, x, points[i].Y, barW, h);
                    }
                }

                // Legend
                g.FillRectangle(brush, plotRect.Right - 120, legendY, 12, 12);
                g.DrawString(name, axisFont, Brushes.Black, plotRect.Right - 104, legendY - 1);
                legendY += 18;
            }

            return bmp;
        }

        #endregion

        #region Event Handlers

        private void UpdateModelList()
        {
            cboModel.Items.Clear();
            var provider = cboProvider.SelectedItem?.ToString() ?? "ollama";
            if (ProviderRegistry.Providers.TryGetValue(provider, out var config))
            {
                cboModel.Items.AddRange(config.ModelList);
                if (cboModel.Items.Count > 0)
                    cboModel.SelectedIndex = 0;
                txtApiKey.PlaceholderText = config.ApiKeyHint;
            }
        }

        private void BtnConnect_Click(object? sender, EventArgs e)
        {
            var provider = cboProvider.SelectedItem?.ToString() ?? "ollama";
            var model = cboModel.Text;
            var apiKey = string.IsNullOrWhiteSpace(txtApiKey.Text) ? null : txtApiKey.Text.Trim();

            try
            {
                _agent = new ChatAgent(provider, apiKey, model);
                _agent.OnToolCall = (name, args) =>
                {
                    if (InvokeRequired)
                        Invoke(() => AddToolCallBubble(name, args));
                    else
                        AddToolCallBubble(name, args);
                };
                _agent.OnChartRequested = (chartData) =>
                {
                    if (InvokeRequired)
                        Invoke(() => AddChartBubble(chartData));
                    else
                        AddChartBubble(chartData);
                };

                lblStatus.Text = $"已连接: {model}";
                lblStatus.ForeColor = Color.FromArgb(39, 174, 96);
                btnSend.Enabled = true;
                btnConnect.Text = "重连";

                AddSystemMessage($"已成功连接到 {provider} ({model})");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"连接失败:\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "连接失败";
                lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
            }
        }

        private async void BtnSend_Click(object? sender, EventArgs e)
        {
            await SendMessage();
        }

        private async void TxtInput_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SendMessage();
            }
        }

        private async Task SendMessage()
        {
            var message = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(message) || _agent == null) return;

            txtInput.Clear();
            AddUserMessage(message);

            btnSend.Enabled = false;
            btnSend.Text = "思考中...";
            _cts = new CancellationTokenSource();

            try
            {
                var response = await Task.Run(() => _agent.ChatAsync(message, _cts.Token));
                AddAssistantMessage(response);
            }
            catch (OperationCanceledException)
            {
                AddSystemMessage("对话已取消");
            }
            catch (Exception ex)
            {
                AddSystemMessage($"错误: {ex.Message}");
            }
            finally
            {
                btnSend.Enabled = true;
                btnSend.Text = "发送";
                _cts = null;
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            messagesPanel.Controls.Clear();
            _agent?.Reset();
            AddSystemMessage("对话已清空，开始新的会话。");
        }

        public void ExportToExcel()
        {
            MessageBox.Show("AI 对话暂不支持导出到 Excel。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion
    }
}
