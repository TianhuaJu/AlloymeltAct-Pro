using AlloyAct_Pro.LLM;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro.Controls
{
    public class ChatPanel : UserControl
    {
        public string PageTitle => "AI Assistant";

        private ChatAgent? _agent;
        private CancellationTokenSource? _cts;
        private bool _isSending;

        // Config controls
        private ComboBox cboProvider = null!;
        private ComboBox cboModel = null!;
        private Label lblUrl = null!;
        private TextBox txtBaseUrl = null!;
        private Button btnRefreshModels = null!;
        private Button btnManageModels = null!;
        private Label lblKey = null!;
        private TextBox txtApiKey = null!;
        private Button btnConnect = null!;
        private Label lblStatus = null!;

        // Chat area
        private Panel chatContainer = null!;
        private FlowLayoutPanel messagesPanel = null!;

        // "计算中..." indicator
        private Label? _thinkingLabel;

        // 流式气泡
        private Panel? _streamingBubble;
        private RichTextBox? _streamingRtb;
        private int _streamCharCount;
        private const int MaxBubbles = 100;

        // Input area
        private TextBox txtInput = null!;
        private Button btnSend = null!;
        private Button btnClear = null!;

        // Win32 鼠标滚轮转发
        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private static extern IntPtr NativeSendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int WM_MOUSEWHEEL = 0x020A;

        /// <summary>
        /// 将子控件的鼠标滚轮事件转发给 chatContainer，使聊天区域始终可滚动
        /// </summary>
        private void ForwardMouseWheel(object? sender, MouseEventArgs e)
        {
            if (chatContainer == null || chatContainer.IsDisposed) return;
            if (e is HandledMouseEventArgs hme)
                hme.Handled = true;
            NativeSendMessage(chatContainer.Handle, WM_MOUSEWHEEL, (IntPtr)(e.Delta << 16), IntPtr.Zero);
        }

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
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));  // Input

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
                chatContainer.ScrollControlIntoView(messagesPanel);
            };

            chatContainer.Controls.Add(messagesPanel);

            // 窗口大小变化时，重新调整所有气泡宽度
            chatContainer.Resize += (s, e) => ResizeAllBubbles();
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
                Margin = new Padding(0, 4, 8, 0),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 22
            };
            cboModel.DrawItem += CboModel_DrawItem;
            UpdateModelList();

            // Base URL（仅 Ollama 时显示）
            lblUrl = new Label
            {
                Text = "服务器:",
                Font = AppTheme.BodyFont,
                AutoSize = true,
                Margin = new Padding(0, 8, 4, 0)
            };

            txtBaseUrl = new TextBox
            {
                Font = AppTheme.BodyFont,
                Width = 200,
                Margin = new Padding(0, 4, 4, 0)
            };
            txtBaseUrl.PlaceholderText = "http://100.91.243.106:11434/v1";
            txtBaseUrl.Text = "http://100.91.243.106:11434/v1";
            txtBaseUrl.Leave += async (s, e) => await TryRefreshModelsAsync();

            // 刷新模型列表按钮（从远程 API 获取真实模型列表）
            btnRefreshModels = new Button
            {
                Text = "🔄",
                Font = new Font("Segoe UI Emoji", 9F),
                Size = new Size(32, 28),
                Margin = new Padding(0, 4, 4, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(236, 240, 241)
            };
            btnRefreshModels.FlatAppearance.BorderSize = 1;
            btnRefreshModels.FlatAppearance.BorderColor = Color.FromArgb(189, 195, 199);
            btnRefreshModels.Click += async (s, e) => await TryRefreshModelsAsync();

            // 模型管理按钮（增删改模型条目，持久化到 %AppData%）
            btnManageModels = new Button
            {
                Text = "⚙",
                Font = new Font("Segoe UI Emoji", 10F),
                Size = new Size(32, 28),
                Margin = new Padding(0, 4, 8, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(236, 240, 241)
            };
            btnManageModels.FlatAppearance.BorderSize = 1;
            btnManageModels.FlatAppearance.BorderColor = Color.FromArgb(189, 195, 199);
            btnManageModels.Click += BtnManageModels_Click;

            // API Key（仅非 Ollama 时显示，默认隐藏）
            lblKey = new Label
            {
                Text = "API Key:",
                Font = AppTheme.BodyFont,
                AutoSize = true,
                Margin = new Padding(0, 8, 4, 0),
                Visible = false
            };

            txtApiKey = new TextBox
            {
                Font = AppTheme.BodyFont,
                UseSystemPasswordChar = true,
                Width = 200,
                Margin = new Padding(0, 4, 8, 0),
                Visible = false
            };
            txtApiKey.PlaceholderText = "输入 API Key";

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
                lblUrl, txtBaseUrl, btnRefreshModels, btnManageModels, lblKey, txtApiKey, btnConnect, lblStatus
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
                Font = new Font("Microsoft YaHei UI", 13F),
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
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 50),
                Enabled = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 6)
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += BtnSend_Click;

            btnClear = new Button
            {
                Text = "清空",
                Font = new Font("Microsoft YaHei UI", 10F),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 40),
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
            var welcome = CreateRichBubble(
                "欢迎使用 AI 热力学计算助手！\n\n" +
                "您可以用自然语言描述计算需求，例如：\n" +
                "• 计算Al-5%Cu合金的液相线温度\n" +
                "• 铝中每增加1%铜，熔点会降低多少？\n" +
                "• 计算Fe-0.2%C合金中C的析出温度\n" +
                "• 获取Fe元素的热力学性质\n" +
                "• 计算Fe-C-Mn合金中C的活度系数\n" +
                "• 筛选哪些元素对铝合金液相线影响最大\n\n" +
                "请先在上方配置 LLM 后端并点击「连接」。",
                Color.FromArgb(245, 245, 245), Color.FromArgb(100, 100, 100),
                "系统", Color.FromArgb(100, 100, 100));
            messagesPanel.Controls.Add(welcome);
        }

        private void AddUserMessage(string text)
        {
            var bubble = CreateRichBubble(text,
                Color.FromArgb(232, 244, 248), Color.FromArgb(44, 62, 80),
                "你", Color.FromArgb(44, 62, 80));
            messagesPanel.Controls.Add(bubble);
            TrimBubbles();
            ScrollToBottom();
        }

        private void AddAssistantMessage(string text)
        {
            RemoveThinkingIndicator();

            var bubble = CreateRichBubble(text,
                Color.FromArgb(240, 248, 232), Color.FromArgb(44, 62, 80),
                "助手", Color.FromArgb(39, 174, 96));
            messagesPanel.Controls.Add(bubble);
            TrimBubbles();
            ScrollToBottom();
        }

        /// <summary>
        /// 创建流式助手气泡（空内容，后续通过 AppendToStreamingBubble 追加）
        /// </summary>
        private void AddStreamingBubble()
        {
            RemoveThinkingIndicator();

            int panelWidth = GetBubbleWidth();

            _streamingBubble = new Panel
            {
                BackColor = Color.FromArgb(240, 248, 232),
                Width = panelWidth,
                Margin = new Padding(5, 3, 5, 3),
                Padding = new Padding(12, 6, 12, 6),
                AutoSize = false,
                Height = 60
            };
            _streamingBubble.Paint += (s, e) =>
            {
                if (s is Panel p && !p.IsDisposed)
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var pen = new Pen(Color.FromArgb(200, 200, 200), 1);
                    var rect = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                    using var path = RoundedRect(rect, 8);
                    e.Graphics.DrawPath(pen, path);
                }
            };

            var roleLabel = new Label
            {
                Text = "助手",
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(39, 174, 96),
                AutoSize = true,
                Location = new Point(12, 6),
                BackColor = Color.Transparent
            };

            _streamingRtb = new RichTextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(240, 248, 232),
                ForeColor = Color.FromArgb(44, 62, 80),
                Font = new Font("Microsoft YaHei UI", 12F),
                Width = panelWidth - 30,
                Location = new Point(12, 30),
                ScrollBars = RichTextBoxScrollBars.None,
                DetectUrls = false,
                WordWrap = true,
                TabStop = false,
                Height = 30
            };

            _streamingRtb.MouseWheel += ForwardMouseWheel;

            _streamingBubble.Controls.Add(roleLabel);
            _streamingBubble.Controls.Add(_streamingRtb);
            messagesPanel.Controls.Add(_streamingBubble);
            _streamCharCount = 0;
            ScrollToBottom();
        }

        /// <summary>
        /// 追加增量文本到流式气泡
        /// 如果气泡不存在（如工具调用循环第二轮），自动创建新气泡
        /// </summary>
        private void AppendToStreamingBubble(string delta)
        {
            // 如果流式气泡不存在（工具调用后第二轮输出），自动创建
            if (_streamingBubble == null || _streamingRtb == null
                || _streamingBubble.IsDisposed || _streamingRtb.IsDisposed)
            {
                AddStreamingBubble();
            }

            if (_streamingRtb == null || _streamingBubble == null) return;

            try
            {
                _streamingRtb.AppendText(delta);
                _streamCharCount += delta.Length;

                // 每 20 字符或遇到换行才重算高度（避免过于频繁）
                if (_streamCharCount % 20 < delta.Length || delta.Contains('\n'))
                {
                    RecalcStreamingBubbleHeight();
                }
            }
            catch (ObjectDisposedException)
            {
                // 控件已被销毁，忽略
                _streamingBubble = null;
                _streamingRtb = null;
            }
        }

        private void RecalcStreamingBubbleHeight()
        {
            if (_streamingRtb == null || _streamingBubble == null) return;
            if (_streamingRtb.IsDisposed || _streamingBubble.IsDisposed) return;

            try
            {
                var contentHeight = GetRichTextBoxContentHeight(_streamingRtb);
                _streamingRtb.Height = contentHeight + 4;
                _streamingBubble.Height = _streamingRtb.Height + 42;
                ScrollToBottom();
            }
            catch (ObjectDisposedException)
            {
                _streamingBubble = null;
                _streamingRtb = null;
            }
        }

        /// <summary>
        /// 完成流式气泡：重新渲染富文本（支持 markdown/LaTeX）
        /// 此方法可安全重复调用（幂等）
        /// </summary>
        private void FinalizeStreamingBubble(string fullContent)
        {
            try
            {
                var bubble = _streamingBubble;
                var rtb = _streamingRtb;

                if (bubble == null || rtb == null) return;
                if (bubble.IsDisposed || rtb.IsDisposed)
                {
                    _streamingBubble = null;
                    _streamingRtb = null;
                    _streamCharCount = 0;
                    return;
                }

                if (!string.IsNullOrEmpty(fullContent))
                {
                    // 预处理并分割为内容块
                    var processedText = PreprocessText(fullContent);
                    var blocks = SplitIntoContentBlocks(processedText);
                    bool hasTable = blocks.Any(b => b.Type == ContentBlockType.Table);

                    if (!hasTable)
                    {
                        // 没有表格：直接用现有RTB渲染（优化路径，保持原行为）
                        RenderMarkdownToRtb(rtb, processedText, Color.FromArgb(44, 62, 80));
                        var contentHeight = GetRichTextBoxContentHeight(rtb);
                        rtb.Height = contentHeight + 4;
                        bubble.Height = rtb.Height + 42;
                    }
                    else
                    {
                        // 有表格：使用内容块架构
                        // 移除现有的流式RTB
                        bubble.Controls.Remove(rtb);
                        rtb.Dispose();

                        int contentWidth = bubble.Width - 30;
                        int yOffset = 30; // role label 下方
                        foreach (var block in blocks)
                        {
                            if (block.Type == ContentBlockType.Text)
                            {
                                var newRtb = new RichTextBox
                                {
                                    ReadOnly = true,
                                    BorderStyle = BorderStyle.None,
                                    BackColor = bubble.BackColor,
                                    ForeColor = Color.FromArgb(44, 62, 80),
                                    Font = new Font("Microsoft YaHei UI", 12F),
                                    Width = contentWidth,
                                    Location = new Point(12, yOffset),
                                    ScrollBars = RichTextBoxScrollBars.None,
                                    DetectUrls = false,
                                    WordWrap = true,
                                    TabStop = false
                                };
                                newRtb.SelectAll();
                                newRtb.SelectionIndent = 0;
                                newRtb.DeselectAll();

                                RenderMarkdownToRtb(newRtb, block.TextContent, Color.FromArgb(44, 62, 80));

                                var h = GetRichTextBoxContentHeight(newRtb);
                                newRtb.Height = h + 4;
                                newRtb.MouseWheel += ForwardMouseWheel;
                                bubble.Controls.Add(newRtb);
                                yOffset += newRtb.Height + 4;
                            }
                            else // Table
                            {
                                var tableFont = new Font("Microsoft YaHei UI", 11F);
                                var dgv = CreateTableGridView(block.TableRows, tableFont);
                                dgv.Location = new Point(12, yOffset);
                                dgv.Width = contentWidth;
                                dgv.Tag = "embedded_table";
                                dgv.MouseWheel += ForwardMouseWheel;
                                bubble.Controls.Add(dgv);
                                yOffset += dgv.Height + 6;
                            }
                        }

                        bubble.Height = yOffset + 12;
                    }
                }
                else
                {
                    // 无内容（工具调用中间轮）：移除空气泡
                    if (messagesPanel.Controls.Contains(bubble))
                    {
                        messagesPanel.Controls.Remove(bubble);
                        bubble.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                // 渲染过程中出现任何异常都不应导致崩溃
            }

            _streamingBubble = null;
            _streamingRtb = null;
            _streamCharCount = 0;

            try
            {
                TrimBubbles();
                ScrollToBottom();
            }
            catch (Exception)
            {
                // 安全忽略
            }
        }

        private void ShowThinkingIndicator()
        {
            try
            {
                if (_thinkingLabel != null) return;
                if (messagesPanel == null || messagesPanel.IsDisposed) return;
                _thinkingLabel = new Label
                {
                    Text = "  ⏳ 计算中...",
                    Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Italic),
                    ForeColor = Color.FromArgb(120, 120, 120),
                    BackColor = Color.FromArgb(245, 245, 245),
                    AutoSize = false,
                    Width = GetBubbleWidth(),
                    Height = 32,
                    Padding = new Padding(10, 6, 10, 6),
                    Margin = new Padding(5, 3, 5, 3)
                };
                messagesPanel.Controls.Add(_thinkingLabel);
                ScrollToBottom();
            }
            catch (Exception)
            {
                // UI 操作不应导致崩溃
            }
        }

        private void RemoveThinkingIndicator()
        {
            try
            {
                if (_thinkingLabel != null && messagesPanel != null && !messagesPanel.IsDisposed)
                {
                    if (!_thinkingLabel.IsDisposed)
                    {
                        messagesPanel.Controls.Remove(_thinkingLabel);
                        _thinkingLabel.Dispose();
                    }
                    _thinkingLabel = null;
                }
            }
            catch (Exception)
            {
                _thinkingLabel = null;
            }
        }

        private void AddSystemMessage(string text)
        {
            int w = GetBubbleWidth();
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Italic),
                ForeColor = Color.FromArgb(136, 136, 136),
                BackColor = Color.FromArgb(240, 240, 240),
                AutoSize = false,
                Width = w,
                MaximumSize = new Size(w, 0),
                AutoEllipsis = false,
                Padding = new Padding(10, 5, 10, 5),
                Margin = new Padding(5, 3, 5, 3)
            };
            using var g = lbl.CreateGraphics();
            var sz = g.MeasureString(text, lbl.Font, lbl.Width - 20);
            lbl.Height = (int)sz.Height + 16;

            messagesPanel.Controls.Add(lbl);
            TrimBubbles();
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
                    Width = GetBubbleWidth(),
                    Height = 360,
                    Margin = new Padding(5, 3, 5, 3),
                    Padding = new Padding(8)
                };

                var pb = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom
                };

                var bmp = RenderChart(chartData, chartPanel.Width - 16, chartPanel.Height - 16);
                pb.Image = bmp;

                chartPanel.Controls.Add(pb);
                messagesPanel.Controls.Add(chartPanel);
                TrimBubbles();
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                AddSystemMessage($"图表渲染失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 内容块类型：文本或表格
        /// </summary>
        private enum ContentBlockType { Text, Table }

        /// <summary>
        /// 内容块：表示一段文本或一个表格
        /// </summary>
        private class ContentBlock
        {
            public ContentBlockType Type;
            public string TextContent = "";
            public List<string[]> TableRows = new();
        }

        /// <summary>
        /// 将预处理后的文本拆分为交替的文本块和表格块
        /// 使用与 RenderMarkdownToRtb 相同的表格检测逻辑
        /// </summary>
        private List<ContentBlock> SplitIntoContentBlocks(string text)
        {
            var blocks = new List<ContentBlock>();
            var lines = text.Split('\n');
            var currentTextLines = new List<string>();
            bool inTable = false;
            var tableRows = new List<string[]>();
            bool inCodeBlock = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 代码块 ``` toggle
                if (line.TrimStart().StartsWith("```"))
                {
                    inCodeBlock = !inCodeBlock;
                    currentTextLines.Add(line);
                    continue;
                }

                // 代码块内的行不检测表格
                if (inCodeBlock)
                {
                    currentTextLines.Add(line);
                    continue;
                }

                // 检测表格行（与 RenderMarkdownToRtb 相同的逻辑）
                if (line.TrimStart().StartsWith("|") && line.TrimEnd().EndsWith("|"))
                {
                    var trimmed = line.Trim();
                    // 分隔线
                    if (Regex.IsMatch(trimmed, @"^\|[\s\-:|]+\|$"))
                    {
                        inTable = true;
                        continue;
                    }

                    // 数据行
                    var cells = trimmed.Split('|', StringSplitOptions.None)
                        .Where(c => !string.IsNullOrEmpty(c.Trim()) || c.Contains(" "))
                        .Select(c => c.Trim())
                        .Where(c => c.Length > 0 || tableRows.Count > 0)
                        .ToArray();
                    if (cells.Length > 0)
                    {
                        // 从文本转为表格：先保存已积累的文本块
                        if (!inTable && currentTextLines.Count > 0)
                        {
                            blocks.Add(new ContentBlock { Type = ContentBlockType.Text, TextContent = string.Join("\n", currentTextLines) });
                            currentTextLines.Clear();
                        }
                        tableRows.Add(cells);
                        inTable = true;
                    }
                    continue;
                }

                // 非表格行：如果之前在表格中，先保存表格块
                if (inTable && tableRows.Count > 0)
                {
                    blocks.Add(new ContentBlock { Type = ContentBlockType.Table, TableRows = new List<string[]>(tableRows) });
                    tableRows.Clear();
                    inTable = false;
                }

                currentTextLines.Add(line);
            }

            // 处理末尾剩余数据
            if (tableRows.Count > 0)
            {
                blocks.Add(new ContentBlock { Type = ContentBlockType.Table, TableRows = new List<string[]>(tableRows) });
            }
            if (currentTextLines.Count > 0)
            {
                blocks.Add(new ContentBlock { Type = ContentBlockType.Text, TextContent = string.Join("\n", currentTextLines) });
            }

            // 如果没有任何块，添加一个空文本块
            if (blocks.Count == 0)
            {
                blocks.Add(new ContentBlock { Type = ContentBlockType.Text, TextContent = "" });
            }

            return blocks;
        }

        /// <summary>
        /// 创建支持富文本的消息气泡
        /// </summary>
        private Panel CreateRichBubble(string text, Color bgColor, Color textColor,
            string roleText, Color roleColor)
        {
            int panelWidth = GetBubbleWidth();

            var bubble = new Panel
            {
                BackColor = bgColor,
                Width = panelWidth,
                Margin = new Padding(5, 3, 5, 3),
                Padding = new Padding(12, 6, 12, 6),
                AutoSize = false
            };
            bubble.Paint += (s, e) =>
            {
                if (s is Panel p && !p.IsDisposed)
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var pen = new Pen(Color.FromArgb(200, 200, 200), 1);
                    var rect = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                    using var path = RoundedRect(rect, 8);
                    e.Graphics.DrawPath(pen, path);
                }
            };

            var roleLabel = new Label
            {
                Text = roleText,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                ForeColor = roleColor,
                AutoSize = true,
                Location = new Point(12, 6),
                BackColor = Color.Transparent
            };
            bubble.Controls.Add(roleLabel);

            int contentWidth = panelWidth - 30;

            // 预处理文本并分割为内容块（文本/表格交替）
            var processedText = PreprocessText(text);
            var blocks = SplitIntoContentBlocks(processedText);

            int yOffset = 30; // role label 下方
            foreach (var block in blocks)
            {
                if (block.Type == ContentBlockType.Text)
                {
                    var rtb = new RichTextBox
                    {
                        ReadOnly = true,
                        BorderStyle = BorderStyle.None,
                        BackColor = bgColor,
                        ForeColor = textColor,
                        Font = new Font("Microsoft YaHei UI", 12F),
                        Width = contentWidth,
                        Location = new Point(12, yOffset),
                        ScrollBars = RichTextBoxScrollBars.None,
                        DetectUrls = false,
                        WordWrap = true,
                        TabStop = false
                    };
                    rtb.SelectAll();
                    rtb.SelectionIndent = 0;
                    rtb.DeselectAll();

                    RenderMarkdownToRtb(rtb, block.TextContent, textColor);

                    var contentHeight = GetRichTextBoxContentHeight(rtb);
                    rtb.Height = contentHeight + 4;
                    rtb.MouseWheel += ForwardMouseWheel;
                    bubble.Controls.Add(rtb);
                    yOffset += rtb.Height + 4;
                }
                else // Table
                {
                    var tableFont = new Font("Microsoft YaHei UI", 11F);
                    var dgv = CreateTableGridView(block.TableRows, tableFont);
                    dgv.Location = new Point(12, yOffset);
                    dgv.Width = contentWidth;
                    dgv.Tag = "embedded_table";
                    dgv.MouseWheel += ForwardMouseWheel;
                    bubble.Controls.Add(dgv);
                    yOffset += dgv.Height + 6;
                }
            }

            bubble.Height = yOffset + 12;
            return bubble;
        }

        /// <summary>
        /// 限制气泡数量，避免内存泄漏
        /// </summary>
        private void TrimBubbles()
        {
            while (messagesPanel.Controls.Count > MaxBubbles)
            {
                var oldest = messagesPanel.Controls[0];
                messagesPanel.Controls.RemoveAt(0);
                oldest.Dispose();
            }
        }

        #endregion

        #region Rendering Pipeline

        /// <summary>
        /// 渲染管线第1步：预处理文本
        /// LaTeX数学公式 → Unicode + HTML sub/sup
        /// </summary>
        private string PreprocessText(string text)
        {
            // 0. 清理 <think>...</think> 思维链标签（deepseek-r1 等推理模型）
            text = Regex.Replace(text, @"<think>[\s\S]*?</think>", "", RegexOptions.IgnoreCase);

            // 1. LaTeX 公式块和行内公式 → Unicode + sub/sup
            text = ConvertLatexToUnicode(text);

            // 2. LaTeX 上下标 → HTML sub/sup （仅匹配变量后的 _x/^x 模式）
            text = ConvertLatexSubscripts(text);

            // 3. 清理残余的独立 LaTeX 命令（如 \ln, \log 等未在公式块中的）
            text = Regex.Replace(text, @"\\(ln|log|exp|sin|cos|tan|max|min)\b", "$1");

            // 4. 清理残余的 LaTeX 花括号（如 {Si,Mg} → Si,Mg）
            text = Regex.Replace(text, @"(?<!\\)\{([^}]*)\}", "$1");

            // 5. 清理残余的反斜杠命令（如 \, \; \! \quad 等间距命令）
            text = Regex.Replace(text, @"\\[,;!]", " ");
            text = Regex.Replace(text, @"\\quad\b", "  ");
            text = Regex.Replace(text, @"\\qquad\b", "    ");
            text = Regex.Replace(text, @"\\left[\(\[\{\\|]?", "");
            text = Regex.Replace(text, @"\\right[\)\]\}\\|]?", "");

            return text;
        }

        /// <summary>
        /// LaTeX 希腊字母和数学符号 → Unicode
        /// </summary>
        private string ConvertLatexToUnicode(string text)
        {
            // 块级公式 \[...\] → 提取内容（先处理块级）
            text = Regex.Replace(text, @"\\\[(.+?)\\\]", m => "\n" + ConvertLatexExpression(m.Groups[1].Value) + "\n", RegexOptions.Singleline);

            // 块级公式 $$...$$ → 提取内容
            text = Regex.Replace(text, @"\$\$([^$]+)\$\$", m => "\n" + ConvertLatexExpression(m.Groups[1].Value) + "\n");

            // 行内公式 \(...\) → 提取内容
            text = Regex.Replace(text, @"\\\((.+?)\\\)", m => ConvertLatexExpression(m.Groups[1].Value), RegexOptions.Singleline);

            // 行内公式 $...$ → 提取内容
            text = Regex.Replace(text, @"\$([^$]+)\$", m => ConvertLatexExpression(m.Groups[1].Value));

            // 处理不在 $...$ 中的 LaTeX 命令
            text = Regex.Replace(text, @"\\text\{([^}]*)\}", "$1");
            text = Regex.Replace(text, @"\\mathrm\{([^}]*)\}", "$1");
            text = Regex.Replace(text, @"\\pu\{([^}]*)\}", "$1");
            text = Regex.Replace(text, @"\\textbf\{([^}]*)\}", "**$1**");

            // 直接替换常见LaTeX命令（不在$...$中的）
            var greekMap = new Dictionary<string, string>
            {
                [@"\alpha"] = "α", [@"\beta"] = "β", [@"\gamma"] = "γ", [@"\delta"] = "δ",
                [@"\epsilon"] = "ε", [@"\varepsilon"] = "ε", [@"\zeta"] = "ζ", [@"\eta"] = "η",
                [@"\theta"] = "θ", [@"\kappa"] = "κ", [@"\lambda"] = "λ", [@"\mu"] = "μ",
                [@"\nu"] = "ν", [@"\xi"] = "ξ", [@"\pi"] = "π", [@"\rho"] = "ρ",
                [@"\sigma"] = "σ", [@"\tau"] = "τ", [@"\phi"] = "φ", [@"\chi"] = "χ",
                [@"\psi"] = "ψ", [@"\omega"] = "ω",
                [@"\Gamma"] = "Γ", [@"\Delta"] = "Δ", [@"\Theta"] = "Θ", [@"\Lambda"] = "Λ",
                [@"\Sigma"] = "Σ", [@"\Phi"] = "Φ", [@"\Omega"] = "Ω",
                [@"\infty"] = "∞", [@"\times"] = "×", [@"\cdot"] = "·",
                [@"\pm"] = "±", [@"\leq"] = "≤", [@"\geq"] = "≥", [@"\neq"] = "≠",
                [@"\approx"] = "≈", [@"\rightarrow"] = "→", [@"\leftarrow"] = "←",
                [@"\sum"] = "Σ", [@"\prod"] = "∏", [@"\partial"] = "∂",
                [@"\degree"] = "°", [@"\circ"] = "°"
            };

            foreach (var (latex, unicode) in greekMap)
            {
                text = text.Replace(latex, unicode);
            }

            return text;
        }

        /// <summary>
        /// 转换 LaTeX 数学表达式为可读文本
        /// </summary>
        private string ConvertLatexExpression(string expr)
        {
            // \text{abc} → abc（纯文本命令，直接提取内容）
            expr = Regex.Replace(expr, @"\\text\{([^}]*)\}", "$1");
            // \mathrm{abc} → abc
            expr = Regex.Replace(expr, @"\\mathrm\{([^}]*)\}", "$1");
            // \pu{unit} → unit（物理单位命令）
            expr = Regex.Replace(expr, @"\\pu\{([^}]*)\}", "$1");
            // \textbf{abc} → **abc** (后续渲染为粗体)
            expr = Regex.Replace(expr, @"\\textbf\{([^}]*)\}", "**$1**");
            // \frac{a}{b} → (a)/(b)
            expr = Regex.Replace(expr, @"\\frac\{([^}]*)\}\{([^}]*)\}", "($1)/($2)");
            // \sqrt{x} → √(x)
            expr = Regex.Replace(expr, @"\\sqrt\{([^}]*)\}", "√($1)");
            // _{x} → <sub>x</sub>
            expr = Regex.Replace(expr, @"_\{([^}]*)\}", "<sub>$1</sub>");
            // ^{x} → <sup>x</sup>
            expr = Regex.Replace(expr, @"\^\{([^}]*)\}", "<sup>$1</sup>");
            // _x (single char) → <sub>x</sub>
            expr = Regex.Replace(expr, @"_([a-zA-Z0-9])", "<sub>$1</sub>");
            // ^x (single char) → <sup>x</sup>
            expr = Regex.Replace(expr, @"\^([a-zA-Z0-9])", "<sup>$1</sup>");
            // \ln → ln, \log → log, \exp → exp
            expr = Regex.Replace(expr, @"\\(ln|log|exp|sin|cos|tan)", "$1");

            // 希腊字母
            var greekInline = new Dictionary<string, string>
            {
                [@"\gamma"] = "γ", [@"\epsilon"] = "ε", [@"\mu"] = "μ",
                [@"\rho"] = "ρ", [@"\sigma"] = "σ", [@"\delta"] = "δ",
                [@"\Delta"] = "Δ", [@"\Sigma"] = "Σ", [@"\alpha"] = "α",
                [@"\beta"] = "β", [@"\theta"] = "θ", [@"\lambda"] = "λ",
                [@"\omega"] = "ω", [@"\phi"] = "φ", [@"\pi"] = "π",
                [@"\infty"] = "∞", [@"\times"] = "×", [@"\cdot"] = "·",
                [@"\pm"] = "±", [@"\leq"] = "≤", [@"\geq"] = "≥",
                [@"\approx"] = "≈", [@"\neq"] = "≠", [@"\partial"] = "∂"
            };
            foreach (var (k, v) in greekInline)
                expr = expr.Replace(k, v);

            return expr;
        }

        /// <summary>
        /// 将非LaTeX上下标转换为 HTML sub/sup（仅匹配变量后的模式，避免snake_case误匹配）
        /// </summary>
        private string ConvertLatexSubscripts(string text)
        {
            // 带花括号的下标/上标：ε_{Zn,Mg} → ε<sub>Zn,Mg</sub>
            text = Regex.Replace(text, @"(?<=[a-zA-Z0-9γεμρσδαβθλωφπΓΔΣ\)])_\{([^}]+)\}", "<sub>$1</sub>");
            text = Regex.Replace(text, @"(?<=[a-zA-Z0-9γεμρσδαβθλωφπΓΔΣ\)])\^\{([^}]+)\}", "<sup>$1</sup>");
            // 单字符下标/上标：ε_i → ε<sub>i</sub>（仅匹配变量/希腊字母后的模式）
            text = Regex.Replace(text, @"(?<=[a-zA-ZγεμρσδαβθλωφπΓΔΣ\)])_([a-zA-Z0-9])(?![a-zA-Z0-9_])", "<sub>$1</sub>");
            text = Regex.Replace(text, @"(?<=[a-zA-ZγεμρσδαβθλωφπΓΔΣ\)])\^([a-zA-Z0-9])(?![a-zA-Z0-9^])", "<sup>$1</sup>");
            return text;
        }

        /// <summary>
        /// 渲染管线第2-5步：Markdown → RichTextBox 富文本
        /// </summary>
        private void RenderMarkdownToRtb(RichTextBox rtb, string text, Color defaultColor)
        {
            rtb.Clear();
            var baseFont = new Font("Microsoft YaHei UI", 12F);
            var boldFont = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            var subFont = new Font("Microsoft YaHei UI", 9F);
            var supFont = new Font("Microsoft YaHei UI", 9F);
            var monoFont = new Font("Consolas", 11F);
            var tableFont = new Font("Consolas", 11F);
            var h2Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold);
            var h3Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            var h4Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold);
            var h5Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);

            var lines = text.Split('\n');
            bool inTable = false;
            var tableRows = new List<string[]>();
            bool inCodeBlock = false;

            for (int li = 0; li < lines.Length; li++)
            {
                var line = lines[li];

                // 代码块 ```
                if (line.TrimStart().StartsWith("```"))
                {
                    if (inCodeBlock)
                    {
                        inCodeBlock = false;
                        continue;
                    }
                    else
                    {
                        inCodeBlock = true;
                        continue;
                    }
                }

                if (inCodeBlock)
                {
                    if (li > 0 || rtb.TextLength > 0) rtb.AppendText("\n");
                    AppendText(rtb, "  " + line, monoFont, Color.FromArgb(60, 60, 60));
                    continue;
                }

                // 检测表格行（包含 | 的行）
                if (line.TrimStart().StartsWith("|") && line.TrimEnd().EndsWith("|"))
                {
                    var trimmed = line.Trim();
                    if (Regex.IsMatch(trimmed, @"^\|[\s\-:|]+\|$"))
                    {
                        inTable = true;
                        continue;
                    }

                    var cells = trimmed.Split('|', StringSplitOptions.None)
                        .Where(c => !string.IsNullOrEmpty(c.Trim()) || c.Contains(" "))
                        .Select(c => c.Trim())
                        .Where(c => c.Length > 0 || tableRows.Count > 0)
                        .ToArray();
                    if (cells.Length > 0)
                    {
                        tableRows.Add(cells);
                        inTable = true;
                    }
                    continue;
                }

                // 如果之前在表格中，现在遇到非表格行，先渲染表格
                if (inTable && tableRows.Count > 0)
                {
                    RenderTable(rtb, tableRows, tableFont, defaultColor);
                    tableRows.Clear();
                    inTable = false;
                }

                // 水平分隔线 --- 或 *** 或 ___
                if (Regex.IsMatch(line.Trim(), @"^[-*_]{3,}$"))
                {
                    if (rtb.TextLength > 0) rtb.AppendText("\n");
                    AppendText(rtb, "────────────────────", baseFont, Color.FromArgb(200, 200, 200));
                    continue;
                }

                // 标题 ## ### #### #####
                var headingMatch = Regex.Match(line, @"^(#{2,5})\s+(.+)$");
                if (headingMatch.Success)
                {
                    if (rtb.TextLength > 0) rtb.AppendText("\n");
                    var level = headingMatch.Groups[1].Value.Length;
                    var headText = headingMatch.Groups[2].Value;
                    var hFont = level switch { 2 => h2Font, 3 => h3Font, 4 => h4Font, _ => h5Font };
                    var hBoldFont = new Font(hFont, FontStyle.Bold);
                    var hSubFont = new Font(hFont.FontFamily, hFont.Size * 0.75F);
                    var hSupFont = new Font(hFont.FontFamily, hFont.Size * 0.75F);
                    // 标题也支持 sub/sup/bold 格式化
                    AppendFormattedLine(rtb, headText, Color.FromArgb(44, 62, 80),
                        hFont, hBoldFont, hSubFont, hSupFont, monoFont);
                    continue;
                }

                // 列表项 - 或 * 或 1.
                var listMatch = Regex.Match(line, @"^(\s*)([-*•]|\d+\.)\s+(.+)$");
                if (listMatch.Success)
                {
                    if (rtb.TextLength > 0) rtb.AppendText("\n");
                    var indent = listMatch.Groups[1].Value;
                    var bullet = listMatch.Groups[2].Value;
                    var content = listMatch.Groups[3].Value;

                    // 用圆点替换 - 或 *
                    string prefix = (bullet == "-" || bullet == "*") ? indent + "  • " : indent + "  " + bullet + " ";
                    AppendText(rtb, prefix, baseFont, Color.FromArgb(100, 100, 100));
                    AppendFormattedLine(rtb, content, defaultColor, baseFont, boldFont, subFont, supFont, monoFont);
                    continue;
                }

                // 普通行
                if (li > 0 || rtb.TextLength > 0)
                    rtb.AppendText("\n");

                AppendFormattedLine(rtb, line, defaultColor, baseFont, boldFont, subFont, supFont, monoFont);
            }

            // 文件末尾仍有未渲染的表格
            if (tableRows.Count > 0)
            {
                RenderTable(rtb, tableRows, tableFont, defaultColor);
            }
        }

        /// <summary>
        /// 追加一行格式化文本（基于逐标签扫描，支持嵌套 sub/sup/bold/code）
        /// </summary>
        private void AppendFormattedLine(RichTextBox rtb, string line, Color defaultColor,
            Font baseFont, Font boldFont, Font subFont, Font supFont, Font monoFont)
        {
            var tokens = TokenizeLine(line);
            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case TokenType.Bold:
                        AppendText(rtb, token.Text, boldFont, defaultColor);
                        break;
                    case TokenType.Sub:
                        AppendText(rtb, token.Text, subFont, Color.FromArgb(60, 80, 100), charOffset: -3);
                        break;
                    case TokenType.Sup:
                        AppendText(rtb, token.Text, supFont, Color.FromArgb(60, 80, 100), charOffset: 6);
                        break;
                    case TokenType.Code:
                        AppendText(rtb, token.Text, monoFont, Color.FromArgb(180, 50, 50));
                        break;
                    default:
                        AppendText(rtb, token.Text, baseFont, defaultColor);
                        break;
                }
            }
        }

        private enum TokenType { Plain, Bold, Sub, Sup, Code }

        private struct TextToken
        {
            public TokenType Type;
            public string Text;
        }

        /// <summary>
        /// 将一行文本解析为格式 token 列表，支持嵌套的 sub/sup 标签
        /// </summary>
        private List<TextToken> TokenizeLine(string line)
        {
            var result = new List<TextToken>();
            int i = 0;
            int len = line.Length;
            var plainBuf = new System.Text.StringBuilder();

            while (i < len)
            {
                // 检测 **bold**
                if (i + 3 < len && line[i] == '*' && line[i + 1] == '*')
                {
                    FlushPlain(result, plainBuf);
                    int close = line.IndexOf("**", i + 2, StringComparison.Ordinal);
                    if (close > i + 2)
                    {
                        var inner = line.Substring(i + 2, close - i - 2);
                        var innerTokens = TokenizeLine(inner);
                        foreach (var t in innerTokens)
                        {
                            result.Add(new TextToken
                            {
                                Type = t.Type == TokenType.Plain ? TokenType.Bold : t.Type,
                                Text = t.Text
                            });
                        }
                        i = close + 2;
                        continue;
                    }
                }

                // 检测 <sub>
                if (i + 4 < len && line.Substring(i, 5).Equals("<sub>", StringComparison.OrdinalIgnoreCase))
                {
                    FlushPlain(result, plainBuf);
                    int closeIdx = FindMatchingClose(line, i + 5, "sub");
                    if (closeIdx >= 0)
                    {
                        var inner = line.Substring(i + 5, closeIdx - i - 5);
                        var stripped = StripTags(inner);
                        result.Add(new TextToken { Type = TokenType.Sub, Text = stripped });
                        i = closeIdx + 6;
                        continue;
                    }
                }

                // 检测 <sup>
                if (i + 4 < len && line.Substring(i, 5).Equals("<sup>", StringComparison.OrdinalIgnoreCase))
                {
                    FlushPlain(result, plainBuf);
                    int closeIdx = FindMatchingClose(line, i + 5, "sup");
                    if (closeIdx >= 0)
                    {
                        var inner = line.Substring(i + 5, closeIdx - i - 5);
                        var stripped = StripTags(inner);
                        result.Add(new TextToken { Type = TokenType.Sup, Text = stripped });
                        i = closeIdx + 6;
                        continue;
                    }
                }

                // 检测 `code`
                if (line[i] == '`')
                {
                    int close = line.IndexOf('`', i + 1);
                    if (close > i + 1)
                    {
                        FlushPlain(result, plainBuf);
                        result.Add(new TextToken { Type = TokenType.Code, Text = line.Substring(i + 1, close - i - 1) });
                        i = close + 1;
                        continue;
                    }
                }

                plainBuf.Append(line[i]);
                i++;
            }

            FlushPlain(result, plainBuf);
            return result;
        }

        /// <summary>
        /// 找到匹配的关闭标签位置（处理嵌套同名标签）
        /// </summary>
        private int FindMatchingClose(string text, int startFrom, string tagName)
        {
            string openTag = $"<{tagName}>";
            string closeTag = $"</{tagName}>";
            int depth = 1;
            int i = startFrom;

            while (i <= text.Length - closeTag.Length)
            {
                if (text.Substring(i, openTag.Length).Equals(openTag, StringComparison.OrdinalIgnoreCase))
                {
                    depth++;
                    i += openTag.Length;
                }
                else if (text.Substring(i, closeTag.Length).Equals(closeTag, StringComparison.OrdinalIgnoreCase))
                {
                    depth--;
                    if (depth == 0) return i;
                    i += closeTag.Length;
                }
                else
                {
                    i++;
                }
            }
            return -1;
        }

        private string StripTags(string s)
        {
            return Regex.Replace(s, @"</?(?:sub|sup)>", "", RegexOptions.IgnoreCase);
        }

        private void FlushPlain(List<TextToken> result, System.Text.StringBuilder buf)
        {
            if (buf.Length > 0)
            {
                result.Add(new TextToken { Type = TokenType.Plain, Text = buf.ToString() });
                buf.Clear();
            }
        }

        private void AppendText(RichTextBox rtb, string text, Font font, Color color, int charOffset = 0)
        {
            int start = rtb.TextLength;
            rtb.AppendText(text);
            rtb.Select(start, text.Length);
            rtb.SelectionFont = font;
            rtb.SelectionColor = color;
            if (charOffset != 0)
                rtb.SelectionCharOffset = charOffset;
            rtb.Select(rtb.TextLength, 0);
        }

        /// <summary>
        /// 渲染表格为真实的 DataGridView 控件嵌入到气泡中
        /// 替代原来的文本表格，提供真实的表格外观
        /// </summary>
        private void RenderTable(RichTextBox rtb, List<string[]> rows, Font tableFont, Color defaultColor)
        {
            if (rows.Count == 0) return;

            // 在 RichTextBox 后追加换行占位
            if (rtb.TextLength > 0)
                rtb.AppendText("\n");
            AppendText(rtb, "[表格]", new Font("Microsoft YaHei UI", 10F, FontStyle.Italic), Color.FromArgb(160, 160, 160));

            // 记录需要嵌入的表格数据，在气泡创建完成后插入 DataGridView
            var bubble = rtb.Parent as Panel;
            if (bubble == null) return;

            var dgv = CreateTableGridView(rows, tableFont);
            bubble.Controls.Add(dgv);

            // 将 DataGridView 标记为需要定位（在气泡高度计算后定位）
            dgv.Tag = "embedded_table";
        }

        /// <summary>
        /// 创建嵌入式 DataGridView 用于显示表格数据
        /// </summary>
        private DataGridView CreateTableGridView(List<string[]> rows, Font tableFont)
        {
            int colCount = rows.Max(r => r.Length);

            var dgv = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = true,
                AllowUserToResizeColumns = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.None,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(200, 210, 220),
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                Font = new Font("Microsoft YaHei UI", 11F),
                Dock = DockStyle.None,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                TabStop = false,
                EditMode = DataGridViewEditMode.EditProgrammatically
            };

            // 阻止选中高亮干扰视觉
            dgv.DefaultCellStyle.SelectionBackColor = Color.White;
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(44, 62, 80);
            dgv.DefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.RowTemplate.Height = 42;

            // 表头样式（第一行数据作为表头）
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dgv.ColumnHeadersHeight = 42;

            // 交替行颜色
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);
            dgv.RowsDefaultCellStyle.BackColor = Color.White;

            // 添加列（使用第一行作为表头），列宽按比例填满整个表格宽度
            string[] headers = rows.Count > 0 ? rows[0] : Array.Empty<string>();
            for (int c = 0; c < colCount; c++)
            {
                string headerText = c < headers.Length ? StripAllFormatting(headers[c]) : $"列{c + 1}";
                var col = new DataGridViewTextBoxColumn
                {
                    HeaderText = headerText,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    FillWeight = 100,
                    MinimumWidth = 80
                };
                dgv.Columns.Add(col);
            }

            // 添加数据行（从第二行开始）
            for (int ri = 1; ri < rows.Count; ri++)
            {
                var rowData = new string[colCount];
                for (int c = 0; c < colCount; c++)
                    rowData[c] = c < rows[ri].Length ? StripAllFormatting(rows[ri][c]) : "";
                dgv.Rows.Add(rowData);
            }

            // 数据行居中对齐
            foreach (DataGridViewRow row in dgv.Rows)
            {
                row.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // 计算合适的高度
            int totalHeight = dgv.ColumnHeadersHeight;
            foreach (DataGridViewRow row in dgv.Rows)
                totalHeight += row.Height;
            totalHeight += 4; // 边距
            dgv.Height = Math.Min(totalHeight, 800);

            // 用户手动调整行高或表头高度后，自动重算表格和气泡高度
            dgv.RowHeightChanged += (s, e) => RecalcTableAndBubbleHeight((DataGridView)s!);
            dgv.ColumnHeadersHeightChanged += (s, e) => RecalcTableAndBubbleHeight((DataGridView)s!);

            return dgv;
        }

        /// <summary>
        /// 用户拖拽调整行高/表头高后，重新计算 DataGridView 高度和所在气泡高度
        /// </summary>
        private void RecalcTableAndBubbleHeight(DataGridView dgv)
        {
            int newHeight = dgv.ColumnHeadersHeight;
            foreach (DataGridViewRow row in dgv.Rows)
                newHeight += row.Height;
            newHeight += 4;
            dgv.Height = Math.Min(newHeight, 800);

            // 重新计算所有内容块的位置和气泡高度
            if (dgv.Parent is Panel bubble)
            {
                RecalcBlockLayout(bubble);
            }
        }

        /// <summary>
        /// 重新计算气泡中所有内容块（RTB + DGV）的位置和气泡高度
        /// </summary>
        private void RecalcBlockLayout(Panel bubble)
        {
            int contentWidth = bubble.Width - 30;

            // 收集所有内容控件，按位置排序
            var contentControls = new List<Control>();
            foreach (Control child in bubble.Controls)
            {
                if (child is RichTextBox || (child is DataGridView d && d.Tag as string == "embedded_table"))
                    contentControls.Add(child);
            }
            contentControls.Sort((a, b) => a.Top.CompareTo(b.Top));

            int yOffset = 30; // role label 下方
            foreach (var child in contentControls)
            {
                if (child is RichTextBox rtb)
                {
                    rtb.Location = new Point(12, yOffset);
                    yOffset += rtb.Height + 4;
                }
                else if (child is DataGridView d)
                {
                    d.Location = new Point(12, yOffset);
                    yOffset += d.Height + 6;
                }
            }

            bubble.Height = yOffset + 12;
        }

        /// <summary>
        /// 在气泡中定位嵌入的 DataGridView 表格
        /// 在 RichTextBox 高度确定后调用
        /// </summary>
        private void PositionEmbeddedTables(Panel bubble, RichTextBox rtb)
        {
            int yOffset = rtb.Bottom + 4;
            int extraHeight = 0;

            foreach (Control ctrl in bubble.Controls)
            {
                if (ctrl is DataGridView dgv && dgv.Tag as string == "embedded_table")
                {
                    dgv.Location = new Point(12, yOffset);
                    dgv.Width = bubble.Width - 28;
                    yOffset += dgv.Height + 6;
                    extraHeight += dgv.Height + 6;
                }
            }

            if (extraHeight > 0)
            {
                bubble.Height = rtb.Height + 42 + extraHeight;
            }
        }

        /// <summary>
        /// 去掉所有格式标签（**bold**、<sub>、<sup>），用于测量纯文本宽度
        /// </summary>
        private string StripAllFormatting(string s)
        {
            // 去掉 **...**
            s = Regex.Replace(s, @"\*\*([^*]*)\*\*", "$1");
            // 去掉 <sub>...</sub> 和 <sup>...</sup>
            s = Regex.Replace(s, @"</?(?:sub|sup)>", "", RegexOptions.IgnoreCase);
            // 去掉 `code`
            s = Regex.Replace(s, @"`([^`]*)`", "$1");
            return s;
        }

        private int GetDisplayLength(string s)
        {
            int len = 0;
            foreach (var c in s)
                len += (c > 127) ? 2 : 1;
            return len;
        }

        private string PadRight(string s, int totalWidth)
        {
            int padNeeded = totalWidth - GetDisplayLength(s);
            if (padNeeded <= 0) return s + " ";
            return s + new string(' ', padNeeded);
        }

        private int GetRichTextBoxContentHeight(RichTextBox rtb)
        {
            if (rtb.TextLength == 0) return 30;

            var lastCharPos = rtb.GetPositionFromCharIndex(rtb.TextLength - 1);

            using var g = rtb.CreateGraphics();
            var lastLineHeight = g.MeasureString("Ag中", rtb.Font).Height;

            int height = lastCharPos.Y + (int)(lastLineHeight * 1.3);

            return Math.Max(height, 30);
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
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                if (chatContainer == null || chatContainer.IsDisposed) return;
                if (messagesPanel == null || messagesPanel.IsDisposed) return;

                BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (IsDisposed || chatContainer.IsDisposed || messagesPanel.IsDisposed) return;
                        chatContainer.VerticalScroll.Value = chatContainer.VerticalScroll.Maximum;
                        chatContainer.PerformLayout();
                        if (messagesPanel.Controls.Count > 0)
                            chatContainer.ScrollControlIntoView(messagesPanel.Controls[messagesPanel.Controls.Count - 1]);
                    }
                    catch (Exception)
                    {
                        // 滚动操作不应导致崩溃
                    }
                }));
            }
            catch (Exception)
            {
                // BeginInvoke 本身也可能失败（如窗体正在关闭）
            }
        }

        /// <summary>
        /// 获取气泡应有的统一宽度（填满 chatContainer 减去边距和滚动条）
        /// </summary>
        private int GetBubbleWidth()
        {
            int w = chatContainer.ClientSize.Width - messagesPanel.Padding.Horizontal - 16;
            return Math.Max(w, 300);
        }

        /// <summary>
        /// 窗口缩放时重新调整所有气泡及其内部 RichTextBox 的宽度
        /// </summary>
        private void ResizeAllBubbles()
        {
            int targetWidth = GetBubbleWidth();

            messagesPanel.SuspendLayout();
            foreach (Control ctrl in messagesPanel.Controls)
            {
                if (ctrl is Panel bubble)
                {
                    bubble.Width = targetWidth;
                    int contentWidth = targetWidth - 30;

                    // 收集所有内容控件（RTB + DGV），按位置排序
                    var contentControls = new List<Control>();
                    foreach (Control child in bubble.Controls)
                    {
                        if (child is RichTextBox || (child is DataGridView dgv && dgv.Tag as string == "embedded_table"))
                            contentControls.Add(child);
                    }
                    contentControls.Sort((a, b) => a.Top.CompareTo(b.Top));

                    int yOffset = 30; // role label 下方
                    foreach (var child in contentControls)
                    {
                        if (child is RichTextBox rtb)
                        {
                            rtb.Width = contentWidth;
                            rtb.Location = new Point(12, yOffset);
                            var h = GetRichTextBoxContentHeight(rtb);
                            rtb.Height = h + 4;
                            yOffset += rtb.Height + 4;
                        }
                        else if (child is DataGridView dgv)
                        {
                            dgv.Width = contentWidth;
                            dgv.Location = new Point(12, yOffset);
                            yOffset += dgv.Height + 6;
                        }
                    }

                    bubble.Height = yOffset + 12;
                    bubble.Invalidate(); // 重绘圆角边框
                }
                else if (ctrl is Label lbl)
                {
                    // 系统消息 label
                    lbl.Width = targetWidth;
                    lbl.MaximumSize = new Size(targetWidth, 0);
                    using var g = lbl.CreateGraphics();
                    var sz = g.MeasureString(lbl.Text, lbl.Font, lbl.Width - 20);
                    lbl.Height = (int)sz.Height + 16;
                }
            }

            // 流式气泡（流式阶段只有单个RTB，无表格）
            if (_streamingBubble != null)
            {
                _streamingBubble.Width = targetWidth;
                if (_streamingRtb != null)
                {
                    _streamingRtb.Width = targetWidth - 30;
                    RecalcStreamingBubbleHeight();
                }
            }

            messagesPanel.ResumeLayout(true);
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

            int ml = 70, mr = 20, mt = 40, mb = 50;
            var plotRect = new Rectangle(ml, mt, width - ml - mr, height - mt - mb);

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

            using var gridPen = new Pen(Color.FromArgb(230, 230, 230), 1) { DashStyle = DashStyle.Dash };
            using var axisPen = new Pen(Color.FromArgb(100, 100, 100), 1);
            using var axisFont = new Font("Microsoft YaHei UI", 8F);
            using var titleFont = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            using var labelFont = new Font("Microsoft YaHei UI", 9F);

            g.DrawRectangle(axisPen, plotRect);

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

            var titleSz = g.MeasureString(title, titleFont);
            g.DrawString(title, titleFont, Brushes.Black, (width - titleSz.Width) / 2, 8);

            var xlSz = g.MeasureString(xLabel, labelFont);
            g.DrawString(xLabel, labelFont, Brushes.DimGray, (width - xlSz.Width) / 2, height - 22);

            var state = g.Save();
            g.TranslateTransform(14, height / 2f);
            g.RotateTransform(-90);
            var ylSz = g.MeasureString(yLabel, labelFont);
            g.DrawString(yLabel, labelFont, Brushes.DimGray, -ylSz.Width / 2, 0);
            g.Restore(state);

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

                g.FillRectangle(brush, plotRect.Right - 120, legendY, 12, 12);
                g.DrawString(name, axisFont, Brushes.Black, plotRect.Right - 104, legendY - 1);
                legendY += 18;
            }

            return bmp;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 自定义绘制模型下拉列表项：不支持工具调用的模型显示为灰色 + ⚠ 标记
        /// </summary>
        private void CboModel_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            var modelName = cboModel.Items[e.Index]?.ToString() ?? "";
            bool unsupported = ChatAgent.IsToolUnsupportedModel(modelName);

            Color textColor;
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                // 选中状态
                textColor = unsupported ? Color.FromArgb(200, 200, 200) : Color.White;
            }
            else
            {
                // 普通状态
                textColor = unsupported ? Color.FromArgb(170, 170, 170) : Color.FromArgb(44, 62, 80);
            }

            var displayText = unsupported ? $"⚠ {modelName}  (无工具)" : modelName;

            using var brush = new SolidBrush(textColor);
            var font = unsupported
                ? new Font(e.Font ?? cboModel.Font, FontStyle.Italic)
                : (e.Font ?? cboModel.Font);
            e.Graphics.DrawString(displayText, font, brush, e.Bounds.X + 2, e.Bounds.Y + 2);

            if (unsupported && font != e.Font)
                font.Dispose();

            e.DrawFocusRectangle();
        }

        private void UpdateModelList()
        {
            cboModel.Items.Clear();
            var provider = cboProvider.SelectedItem?.ToString() ?? "ollama";
            if (ProviderRegistry.Providers.TryGetValue(provider, out var config))
            {
                // 使用用户覆盖后的生效列表（若用户在 UI 中自定义过，优先用之）
                var effectiveModels = ProviderRegistry.GetEffectiveModelList(provider);
                cboModel.Items.AddRange(effectiveModels);

                // 尽量选中生效的默认模型
                var defaultModel = ProviderRegistry.GetEffectiveDefaultModel(provider);
                var defIdx = cboModel.Items.IndexOf(defaultModel);
                if (defIdx >= 0) cboModel.SelectedIndex = defIdx;
                else if (cboModel.Items.Count > 0) cboModel.SelectedIndex = 0;

                if (txtApiKey != null)
                    txtApiKey.PlaceholderText = config.ApiKeyHint;
                if (txtBaseUrl != null)
                {
                    txtBaseUrl.Text = config.BaseUrl;
                    txtBaseUrl.PlaceholderText = config.BaseUrl;
                }
            }

            UpdateProviderUI(provider);

            if (provider == "ollama")
            {
                _ = TryRefreshModelsAsync();
            }
        }

        /// <summary>
        /// 根据提供商切换 UI
        /// - Ollama：显示服务器地址 + 刷新 + 管理
        /// - OpenAI 兼容（openai/deepseek/kimichat）：显示 API Key + 刷新 + 管理
        /// - Claude/Gemini：仅显示 API Key + 管理（刷新按钮隐藏，这些服务无标准 /models 端点）
        /// </summary>
        private void UpdateProviderUI(string provider)
        {
            bool isOllama = provider == "ollama";
            bool supportsRemoteFetch = provider is "ollama" or "openai" or "deepseek" or "kimichat";

            if (lblUrl != null) lblUrl.Visible = isOllama;
            if (txtBaseUrl != null) txtBaseUrl.Visible = isOllama;
            if (btnRefreshModels != null) btnRefreshModels.Visible = supportsRemoteFetch;

            // 管理按钮始终可见
            if (btnManageModels != null) btnManageModels.Visible = true;

            // 非 Ollama 显示 API Key 输入框
            if (lblKey != null) lblKey.Visible = !isOllama;
            if (txtApiKey != null) txtApiKey.Visible = !isOllama;
        }

        private void BtnManageModels_Click(object? sender, EventArgs e)
        {
            var provider = cboProvider.SelectedItem?.ToString() ?? "";
            var currentKey = txtApiKey?.Text ?? "";
            var currentUrl = txtBaseUrl?.Text ?? "";
            using var dlg = new ModelManagerDialog(provider, currentKey, currentUrl);
            var result = dlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                UpdateModelList();
                if (lblStatus != null)
                {
                    lblStatus.Text = "模型列表已更新";
                    lblStatus.ForeColor = Color.FromArgb(39, 174, 96);
                }
            }
        }

        /// <summary>
        /// 从远程 API 拉取模型列表并合并到下拉框
        /// 支持 ollama / openai / deepseek / kimichat；claude、gemini 无标准 /models 端点，按钮已隐藏
        /// </summary>
        private async Task TryRefreshModelsAsync()
        {
            var provider = cboProvider.SelectedItem?.ToString() ?? "";
            if (lblStatus == null) return;

            string baseUrl = provider == "ollama" ? (txtBaseUrl?.Text?.Trim() ?? "") : "";
            string apiKey = provider == "ollama" ? "" : (txtApiKey?.Text?.Trim() ?? "");

            lblStatus.Text = "获取模型列表...";
            lblStatus.ForeColor = Color.FromArgb(52, 152, 219);

            try
            {
                var models = await LlmBackend.FetchModelsAsync(provider, baseUrl, apiKey);
                if (models != null && models.Length > 0)
                {
                    var currentModel = cboModel.Text;
                    cboModel.Items.Clear();
                    cboModel.Items.AddRange(models);

                    var idx = cboModel.Items.IndexOf(currentModel);
                    if (idx >= 0)
                        cboModel.SelectedIndex = idx;
                    else if (cboModel.Items.Count > 0)
                        cboModel.SelectedIndex = 0;

                    lblStatus.Text = $"已获取 {models.Length} 个模型";
                    lblStatus.ForeColor = Color.FromArgb(39, 174, 96);
                }
                else
                {
                    lblStatus.Text = "未获取到模型，使用默认列表";
                    lblStatus.ForeColor = Color.FromArgb(243, 156, 18);
                }
            }
            catch
            {
                lblStatus.Text = "获取模型失败，使用默认列表";
                lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
            }
        }

        private void BtnConnect_Click(object? sender, EventArgs e)
        {
            var provider = cboProvider.SelectedItem?.ToString() ?? "ollama";
            var model = cboModel.Text;
            var baseUrl = string.IsNullOrWhiteSpace(txtBaseUrl.Text) ? null : txtBaseUrl.Text.Trim();
            var apiKey = string.IsNullOrWhiteSpace(txtApiKey.Text) ? null : txtApiKey.Text.Trim();

            try
            {
                _agent = new ChatAgent(provider, apiKey, model, baseUrl);
                LlmBackend.Current = LlmBackend.Create(provider, apiKey, model, baseUrl);

                _agent.OnToolCall = (name, args) =>
                {
                    if (IsDisposed) return;
                    if (InvokeRequired)
                        BeginInvoke(() => { if (_isSending) ShowThinkingIndicator(); });
                    else
                        if (_isSending) ShowThinkingIndicator();
                };
                _agent.OnChartRequested = (chartData) =>
                {
                    if (IsDisposed) return;
                    if (InvokeRequired)
                        BeginInvoke(() => { if (_isSending) AddChartBubble(chartData); });
                    else
                        if (_isSending) AddChartBubble(chartData);
                };
                _agent.OnTextDelta = (delta) =>
                {
                    if (IsDisposed) return;
                    if (InvokeRequired)
                        BeginInvoke(() => { if (_isSending) AppendToStreamingBubble(delta); });
                    else
                        if (_isSending) AppendToStreamingBubble(delta);
                };
                _agent.OnStreamComplete = (fullText) =>
                {
                    if (IsDisposed) return;
                    if (InvokeRequired)
                        BeginInvoke(() => { if (_isSending) FinalizeStreamingBubble(fullText); });
                    else
                        if (_isSending) FinalizeStreamingBubble(fullText);
                };

                btnConnect.Text = "重连";

                if (_agent.ToolsSupported)
                {
                    lblStatus.Text = $"已连接: {model}";
                    lblStatus.ForeColor = Color.FromArgb(39, 174, 96);
                    AddSystemMessage($"已成功连接到 {provider} ({model})");
                }
                else
                {
                    lblStatus.Text = $"已连接: {model} (无工具)";
                    lblStatus.ForeColor = Color.FromArgb(243, 156, 18);
                    AddSystemMessage($"已连接到 {provider} ({model})\n⚠ 该模型不支持工具调用，无法执行热力学计算。仅支持普通对话。\n建议切换到支持工具调用的模型（如 qwen2.5、llama3.2、gemma2 等）。");
                }
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
            if (_isSending)
            {
                // 当前正在发送 → 强制取消
                _cts?.Cancel();

                // 立即重置 UI 状态（不依赖异常传播）
                _isSending = false;
                btnSend.Text = "发送";
                btnSend.BackColor = Color.FromArgb(39, 174, 96);

                // 清理流式气泡和计算中提示
                if (_streamingBubble != null && !_streamingBubble.IsDisposed)
                {
                    if (messagesPanel.Controls.Contains(_streamingBubble))
                    {
                        messagesPanel.Controls.Remove(_streamingBubble);
                        _streamingBubble.Dispose();
                    }
                }
                _streamingBubble = null;
                _streamingRtb = null;
                _streamCharCount = 0;
                RemoveThinkingIndicator();
                AddSystemMessage("对话已取消");

                _cts = null;
                return;
            }
            await SendMessage();
        }

        private async void TxtInput_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (!_isSending)
                    await SendMessage();
            }
        }

        private async Task SendMessage()
        {
            var message = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            if (_agent == null)
            {
                AddSystemMessage("⚠ 请先点击「连接」按钮连接到 LLM 后端。");
                return;
            }

            txtInput.Clear();
            AddUserMessage(message);

            // 切换到"取消"状态
            _isSending = true;
            btnSend.Text = "取消";
            btnSend.BackColor = Color.FromArgb(231, 76, 60);
            _cts = new CancellationTokenSource();

            // 创建流式气泡
            AddStreamingBubble();

            try
            {
                var response = await Task.Run(() => _agent.ChatStreamAsync(message, _cts.Token));
                // FinalizeStreamingBubble 已通过 OnStreamComplete 回调执行
            }
            catch (OperationCanceledException)
            {
                FinalizeStreamingBubble("");
                RemoveThinkingIndicator();
                AddSystemMessage("对话已取消");
            }
            catch (Exception ex)
            {
                FinalizeStreamingBubble("");
                RemoveThinkingIndicator();
                AddSystemMessage($"错误: {ex.Message}");
            }
            finally
            {
                try
                {
                    // 确保即使在异常情况下也清理流式气泡状态
                    if (_streamingBubble != null)
                        FinalizeStreamingBubble("");
                    RemoveThinkingIndicator();
                }
                catch (Exception)
                {
                    // 安全忽略，防止 finally 块中的异常变为未处理异常
                }

                _isSending = false;
                btnSend.Text = "发送";
                btnSend.BackColor = Color.FromArgb(39, 174, 96);
                _cts = null;
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            // Dispose 所有控件避免内存泄漏
            foreach (Control ctrl in messagesPanel.Controls)
                ctrl.Dispose();
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
