using AlloyAct_Pro.LLM;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UglyToad.PdfPig;

namespace AlloyAct_Pro.Controls
{
    /// <summary>
    /// 知识学习面板 — 管理 AI 助手的记忆/知识条目，支持多格式文献导入与 AI 学习
    /// </summary>
    public class KnowledgePanel : UserControl
    {
        public string PageTitle => "Knowledge Base";

        private readonly MemoryStore _memory = new();

        // Controls
        private DataGridView dgvMemories = null!;
        private ComboBox cboCategory = null!;
        private TextBox txtContent = null!;
        private Button btnAdd = null!;
        private Button btnImport = null!;
        private Button btnLearn = null!;
        private Button btnDelete = null!;
        private Button btnDeleteAll = null!;
        private TextBox txtSearch = null!;
        private Label lblStats = null!;
        private Label lblImportStatus = null!;

        // Robust row tracking for delete
        private int _trackedRowIndex = -1;
        private bool _isRefreshing;

        // Imported text / images for AI learning
        private string _importedText = "";
        private string _importedFileName = "";
        private List<MessageImage> _importedImages = new();

        // Cancellation for AI learning
        private CancellationTokenSource? _learnCts;

        public KnowledgePanel()
        {
            InitializeUI();
            LoadData();
        }

        #region UI

        private void InitializeUI()
        {
            BackColor = AppTheme.ContentBg;
            Padding = new Padding(20);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));   // Stats + search
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Table
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));  // Add form

            // === Row 0: Stats + search ===
            var topPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            topPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            lblStats = new Label
            {
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            topPanel.Controls.Add(lblStats, 0, 0);

            var searchPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 6, 0, 6) };
            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 10F),
                PlaceholderText = "搜索知识条目...",
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) => FilterData();
            searchPanel.Controls.Add(txtSearch);
            topPanel.Controls.Add(searchPanel, 1, 0);

            mainLayout.Controls.Add(topPanel, 0, 0);

            // === Row 1: DataGridView ===
            var tablePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 4, 0, 4)
            };

            dgvMemories = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                MultiSelect = false,
                AllowUserToResizeRows = false
            };
            AppTheme.StyleDataGridView(dgvMemories);
            dgvMemories.ColumnHeadersHeight = 40;
            dgvMemories.RowTemplate.Height = 36;
            dgvMemories.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 10F);
            dgvMemories.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvMemories.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvMemories.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // Track row selection via multiple events for robustness
            dgvMemories.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0) _trackedRowIndex = e.RowIndex;
            };
            dgvMemories.SelectionChanged += (s, e) =>
            {
                if (!_isRefreshing && dgvMemories.CurrentRow != null)
                    _trackedRowIndex = dgvMemories.CurrentRow.Index;
            };
            dgvMemories.RowEnter += (s, e) =>
            {
                if (!_isRefreshing && e.RowIndex >= 0)
                    _trackedRowIndex = e.RowIndex;
            };

            dgvMemories.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCategory",
                HeaderText = "分类",
                Width = 130,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            });
            dgvMemories.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colContent",
                HeaderText = "知识内容",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            dgvMemories.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colUpdated",
                HeaderText = "更新时间",
                Width = 150,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            });

            tablePanel.Controls.Add(dgvMemories);
            mainLayout.Controls.Add(tablePanel, 0, 1);

            // === Row 2: Add form ===
            var formPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(14, 8, 14, 8)
            };

            var formLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // Title + import status
            formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Content input
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Buttons row

            // Title row (title left, import status right)
            var titleRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
            titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            titleRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var formTitle = new Label
            {
                Text = "添加知识条目  (Ctrl+Enter 快捷添加)",
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };
            titleRow.Controls.Add(formTitle, 0, 0);

            lblImportStatus = new Label
            {
                Text = "",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = Color.FromArgb(39, 174, 96),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomRight,
                AutoEllipsis = true
            };
            titleRow.Controls.Add(lblImportStatus, 1, 0);
            formLayout.Controls.Add(titleRow, 0, 0);

            // Content input
            var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 6) };
            txtContent = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 10F),
                PlaceholderText = "输入知识内容，或导入文献后点击「AI学习」提取知识...",
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            txtContent.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    AddMemory();
                }
            };
            contentPanel.Controls.Add(txtContent);
            formLayout.Controls.Add(contentPanel, 0, 1);

            // Button row — logical order: [导入文献] [AI学习] | [Category] [添加] | spacer | [删除选中] [清空全部]
            var btnRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = 1,
                Padding = new Padding(0)
            };
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));  // 0: Import
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));  // 1: AI Learn
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));  // 2: Category
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));   // 3: Add
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // 4: Spacer
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));  // 5: Delete
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));  // 6: DeleteAll
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));    // 7: End pad
            btnRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // 0: Import button
            btnImport = CreateButton("导入文献", Color.FromArgb(41, 128, 185), Color.White);
            btnImport.Click += (s, e) => ImportFile();
            btnRow.Controls.Add(WrapBtn(btnImport, 0, 4, 4, 4), 0, 0);

            // 1: AI Learn button
            btnLearn = CreateButton("AI学习", Color.FromArgb(142, 68, 173), Color.White);
            btnLearn.Click += (s, e) => LearnFromImport();
            btnRow.Controls.Add(WrapBtn(btnLearn, 0, 4, 6, 4), 1, 0);

            // 2: Category
            var catWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 6, 4) };
            cboCategory = new ComboBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DropDownWidth = 260
            };
            cboCategory.Items.AddRange(new object[]
            {
                "knowledge — 知识",
                "preference — 设置",
                "alloy_system — 合金体系",
                "calculation — 计算经验",
                "general — 其他"
            });
            cboCategory.SelectedIndex = 0;
            catWrap.Controls.Add(cboCategory);
            btnRow.Controls.Add(catWrap, 2, 0);

            // 3: Add button
            btnAdd = CreateButton("添加", Color.FromArgb(39, 174, 96), Color.White);
            btnAdd.Click += (s, e) => AddMemory();
            btnRow.Controls.Add(WrapBtn(btnAdd, 0, 4, 4, 4), 3, 0);

            // 4: Spacer
            btnRow.Controls.Add(new Panel { Dock = DockStyle.Fill }, 4, 0);

            // 5: Delete selected
            btnDelete = CreateButton("删除选中", Color.FromArgb(231, 76, 60), Color.White);
            btnDelete.Click += (s, e) => DeleteSelected();
            btnRow.Controls.Add(WrapBtn(btnDelete, 0, 4, 4, 4), 5, 0);

            // 6: Delete all
            btnDeleteAll = new Button
            {
                Text = "清空全部",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(231, 76, 60) },
                BackColor = Color.White,
                ForeColor = Color.FromArgb(231, 76, 60),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDeleteAll.Click += (s, e) => DeleteAll();
            btnRow.Controls.Add(WrapBtn(btnDeleteAll, 0, 4, 0, 4), 6, 0);

            formLayout.Controls.Add(btnRow, 0, 2);
            formPanel.Controls.Add(formLayout);
            mainLayout.Controls.Add(formPanel, 0, 2);

            Controls.Add(mainLayout);
        }

        private static Button CreateButton(string text, Color bg, Color fg) => new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            BackColor = bg,
            ForeColor = fg,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        private static Panel WrapBtn(Button btn, int left, int top, int right, int bottom)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(left, top, right, bottom) };
            p.Controls.Add(btn);
            return p;
        }

        #endregion

        #region Data

        private static readonly Dictionary<string, string> CategoryLabels = new()
        {
            ["preference"] = "设置",
            ["alloy_system"] = "合金体系",
            ["calculation"] = "计算经验",
            ["knowledge"] = "知识",
            ["general"] = "其他"
        };

        private void LoadData()
        {
            var json = _memory.RecallMemories();
            PopulateGrid(json);
        }

        private void FilterData()
        {
            var keyword = txtSearch.Text.Trim();
            var json = _memory.RecallMemories(string.IsNullOrEmpty(keyword) ? null : keyword);
            PopulateGrid(json);
        }

        private void PopulateGrid(string json)
        {
            _isRefreshing = true;
            dgvMemories.Rows.Clear();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var count = root.GetProperty("count").GetInt32();
                lblStats.Text = $"共 {count} 条知识";

                if (root.TryGetProperty("memories", out var memories) && memories.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in memories.EnumerateArray())
                    {
                        var cat = item.GetProperty("Category").GetString() ?? "general";
                        var content = item.GetProperty("Content").GetString() ?? "";
                        var updated = item.GetProperty("updated").GetString() ?? "";
                        var label = CategoryLabels.GetValueOrDefault(cat, cat);
                        dgvMemories.Rows.Add(label, content, updated);
                    }
                }
            }
            catch
            {
                lblStats.Text = "共 0 条知识";
            }
            finally
            {
                _isRefreshing = false;
            }

            // After refresh, auto-track first row if rows exist
            if (dgvMemories.Rows.Count > 0)
                _trackedRowIndex = 0;
            else
                _trackedRowIndex = -1;
        }

        private string GetSelectedCategory()
        {
            var text = cboCategory.SelectedItem?.ToString() ?? "";
            return text.Split('—')[0].Trim().Split(' ')[0].Trim();
        }

        private void AddMemory()
        {
            var content = txtContent.Text.Trim();
            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("请输入知识内容。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _memory.SaveMemory(content, GetSelectedCategory());
            txtContent.Clear();
            LoadData();
        }

        #endregion

        #region Import (PDF / Word / TXT / CSV / MD)

        private void ImportFile()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "导入文献 / 知识文件",
                Filter = "所有支持格式|*.pdf;*.docx;*.txt;*.md;*.csv|" +
                         "PDF文件|*.pdf|" +
                         "Word文档|*.docx|" +
                         "文本文件|*.txt;*.md;*.csv|" +
                         "所有文件|*.*",
                Multiselect = false
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var ext = Path.GetExtension(ofd.FileName).ToLowerInvariant();
                string text = "";
                _importedImages.Clear();

                switch (ext)
                {
                    case ".pdf":
                        text = ReadPdfText(ofd.FileName);
                        // If text extraction fails, try extracting page images (scanned PDF)
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            _importedImages = ExtractPdfImages(ofd.FileName);
                            if (_importedImages.Count > 0)
                            {
                                _importedText = "";
                                _importedFileName = Path.GetFileName(ofd.FileName);
                                var kb = new FileInfo(ofd.FileName).Length / 1024.0;

                                txtContent.Text = $"[扫描版PDF: {_importedImages.Count} 页图片，需AI识别]";
                                lblImportStatus.Text = $"已导入扫描版: {_importedFileName} ({_importedImages.Count}页) — 点击「AI学习」识别";
                                lblImportStatus.ForeColor = Color.FromArgb(142, 68, 173);

                                MessageBox.Show(
                                    $"检测到扫描版PDF\n" +
                                    $"文件: {_importedFileName}\n" +
                                    $"大小: {kb:F1} KB, {_importedImages.Count} 页图片\n\n" +
                                    "点击「AI学习」→ AI将识别图片中的文字并提取知识点\n" +
                                    "(需要先在AI助手页面连接支持图片的模型)",
                                    "扫描版PDF导入",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }
                        }
                        break;
                    case ".docx":
                        text = ReadDocxText(ofd.FileName);
                        break;
                    default: // .txt, .md, .csv, etc.
                        text = File.ReadAllText(ofd.FileName).Trim();
                        break;
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show("文件内容为空或无法提取文本。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _importedText = text;
                _importedFileName = Path.GetFileName(ofd.FileName);
                var charCount = text.Length;
                var sizeKB = new FileInfo(ofd.FileName).Length / 1024.0;

                // Show preview in textbox
                txtContent.Text = text.Length > 500 ? text[..500] + "..." : text;

                lblImportStatus.Text = $"已导入: {_importedFileName} ({sizeKB:F1} KB, {charCount}字) — 点击「AI学习」提取知识";
                lblImportStatus.ForeColor = Color.FromArgb(39, 174, 96);

                MessageBox.Show(
                    $"文件: {_importedFileName}\n" +
                    $"大小: {sizeKB:F1} KB, {charCount} 字\n\n" +
                    "可以选择:\n" +
                    "  • 点击「AI学习」— AI智能提取关键知识点\n" +
                    "  • 点击「添加」— 将文本框内容直接添加为知识条目",
                    "文献导入成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Read text from a PDF file using PdfPig
        /// </summary>
        private static string ReadPdfText(string path)
        {
            var sb = new StringBuilder();
            using var document = PdfDocument.Open(path);
            foreach (var page in document.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return sb.ToString().Trim();
        }

        /// <summary>
        /// Read text from a .docx file by parsing the XML inside the ZIP archive
        /// </summary>
        private static string ReadDocxText(string path)
        {
            using var zip = ZipFile.OpenRead(path);
            var entry = zip.GetEntry("word/document.xml");
            if (entry == null) return "";

            using var stream = entry.Open();
            var xdoc = XDocument.Load(stream);
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            var paragraphs = xdoc.Descendants(w + "p")
                .Select(p => string.Join("", p.Descendants(w + "t").Select(t => t.Value)))
                .Where(s => !string.IsNullOrWhiteSpace(s));

            return string.Join("\n", paragraphs);
        }

        /// <summary>
        /// Extract page images from a scanned PDF (all pages)
        /// </summary>
        private static List<MessageImage> ExtractPdfImages(string path)
        {
            var result = new List<MessageImage>();
            try
            {
                using var document = PdfDocument.Open(path);
                foreach (var page in document.GetPages())
                {
                    foreach (var image in page.GetImages())
                    {
                        try
                        {
                            if (image.TryGetPng(out var pngBytes))
                            {
                                result.Add(new MessageImage { Data = pngBytes, MimeType = "image/png" });
                                break; // One image per page for scanned PDFs
                            }
                            else
                            {
                                // Raw bytes might be JPEG
                                var raw = image.RawBytes.ToArray();
                                if (raw.Length > 100)
                                {
                                    var mime = IsJpeg(raw) ? "image/jpeg" : "image/png";
                                    result.Add(new MessageImage { Data = raw, MimeType = mime });
                                    break;
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// Check if raw bytes start with JPEG magic bytes (FF D8 FF)
        /// </summary>
        private static bool IsJpeg(byte[] data) =>
            data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;

        #endregion

        #region AI Learning

        private const int ImageBatchSize = 5;    // Fewer pages per call = higher recognition accuracy
        private const int TextBatchSize = 20000;  // Ollama local models handle long context well
        private const int TextOverlap = 800;      // Generous overlap to avoid cutting formulas/tables

        /// <summary>
        /// Result from a knowledge extraction batch, including points and a running summary.
        /// </summary>
        private class BatchResult
        {
            public List<string> Points { get; set; } = new();
            public string Summary { get; set; } = "";
        }

        private async void LearnFromImport()
        {
            bool hasImages = _importedImages.Count > 0;
            var textToLearn = !string.IsNullOrWhiteSpace(_importedText) ? _importedText : txtContent.Text.Trim();

            if (!hasImages && string.IsNullOrWhiteSpace(textToLearn))
            {
                MessageBox.Show("请先导入文献或在文本框中输入内容。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (LlmBackend.Current == null)
            {
                MessageBox.Show("请先在「AI助手」页面连接AI模型，然后再使用AI学习功能。", "未连接AI模型",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // === Ask user for learning focus ===
            var userFocus = ShowLearningFocusDialog();
            if (userFocus == null) return;  // User cancelled

            var category = GetSelectedCategory();
            btnLearn.Enabled = false;
            btnLearn.Text = "学习中...";
            _learnCts = new CancellationTokenSource();
            var ct = _learnCts.Token;

            try
            {
                var allPoints = new List<string>();
                var failedBatches = new List<(int Num, string Err)>();
                string prevSummary = "";

                if (hasImages)
                {
                    // --- LLM视觉识别：每次少量页面确保识别精度 ---
                    int totalPages = _importedImages.Count;
                    int totalBatches = (totalPages + ImageBatchSize - 1) / ImageBatchSize;

                    for (int b = 0; b < totalBatches; b++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var batch = _importedImages.Skip(b * ImageBatchSize).Take(ImageBatchSize).ToList();
                        int fromPage = b * ImageBatchSize + 1;
                        int toPage = Math.Min((b + 1) * ImageBatchSize, totalPages);

                        lblImportStatus.Text = $"AI视觉识别第 {fromPage}-{toPage} 页 (共{totalPages}页, 批次{b + 1}/{totalBatches})...";
                        lblImportStatus.ForeColor = Color.FromArgb(142, 68, 173);

                        try
                        {
                            var result = await ExtractKnowledgeWithContext(
                                batch, null, b + 1, totalBatches, prevSummary, userFocus, ct);
                            allPoints.AddRange(result.Points);
                            prevSummary = result.Summary;
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            failedBatches.Add((b + 1, ex.Message));
                        }
                    }
                }
                else
                {
                    // --- 文本提取：关键词过滤 + 分批 + 上下文延续 ---
                    var textChunks = SplitTextIntoChunks(textToLearn, TextBatchSize, TextOverlap);

                    // Keyword filtering: only process chunks relevant to user's focus
                    if (!string.IsNullOrWhiteSpace(userFocus))
                    {
                        var filtered = FilterChunksByKeywords(textChunks, userFocus);
                        if (filtered.Count > 0 && filtered.Count < textChunks.Count)
                        {
                            lblImportStatus.Text = $"关键词定位：从{textChunks.Count}段中筛选出{filtered.Count}段相关内容...";
                            lblImportStatus.ForeColor = Color.FromArgb(41, 128, 185);
                            await Task.Delay(500, ct); // Brief pause to show status
                            textChunks = filtered;
                        }
                    }

                    int totalBatches = textChunks.Count;

                    for (int b = 0; b < totalBatches; b++)
                    {
                        ct.ThrowIfCancellationRequested();

                        lblImportStatus.Text = totalBatches == 1
                            ? "AI正在分析文献，提取知识点..."
                            : $"AI正在分析文献 (批次{b + 1}/{totalBatches}, 共{textToLearn.Length}字)...";
                        lblImportStatus.ForeColor = Color.FromArgb(142, 68, 173);

                        try
                        {
                            var result = await ExtractKnowledgeWithContext(
                                null, textChunks[b], b + 1, totalBatches, prevSummary, userFocus, ct);
                            allPoints.AddRange(result.Points);
                            prevSummary = result.Summary;
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            failedBatches.Add((b + 1, ex.Message));
                        }
                    }
                }

                // Report failed batches
                if (failedBatches.Count > 0)
                {
                    var failMsg = $"{failedBatches.Count} 个批次处理失败：\n";
                    foreach (var (num, err) in failedBatches)
                        failMsg += $"  批次 {num}: {err}\n";
                    failMsg += "\n已成功提取的知识点仍可保存。";
                    MessageBox.Show(failMsg, "部分批次失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                // Exact dedup
                allPoints = allPoints.Distinct(StringComparer.Ordinal).ToList();

                // Final synthesis: LLM-based dedup and quality check (≥10 points)
                if (allPoints.Count >= 10)
                {
                    lblImportStatus.Text = "AI正在整理去重知识点...";
                    try
                    {
                        allPoints = await SynthesizeKnowledgePoints(allPoints, ct);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Synthesis failed — proceed with raw deduplicated points
                    }
                }

                if (allPoints.Count == 0)
                {
                    MessageBox.Show("未能从AI回复中提取有效知识点。", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Preview and confirm
                var preview = $"AI从文献中提取了 {allPoints.Count} 个知识点。\n\n前5条预览:\n";
                for (int i = 0; i < Math.Min(5, allPoints.Count); i++)
                {
                    var p = allPoints[i];
                    preview += $"  {i + 1}. {(p.Length > 80 ? p[..80] + "..." : p)}\n";
                }

                var confirm = MessageBox.Show(preview + "\n确定保存到知识库？", "AI学习结果",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;

                int saved = 0;
                foreach (var point in allPoints)
                {
                    _memory.SaveMemory(point, category);
                    saved++;
                }

                // Keep document loaded for multi-round learning
                LoadData();

                var docName = !string.IsNullOrEmpty(_importedFileName) ? _importedFileName : "文本";
                lblImportStatus.Text = $"已保存{saved}条 | 文档「{docName}」仍可继续学习其他内容";
                lblImportStatus.ForeColor = Color.FromArgb(39, 174, 96);

                MessageBox.Show(
                    $"成功保存 {saved} 条知识到知识库。\n\n" +
                    $"文档「{docName}」仍保留在内存中。\n" +
                    "如需提取其他知识点，可再次点击「AI学习」并输入新的学习重点。",
                    "AI学习完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                lblImportStatus.Text = "AI学习已取消";
                lblImportStatus.ForeColor = Color.FromArgb(231, 76, 60);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI学习失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblImportStatus.Text = "AI学习失败";
                lblImportStatus.ForeColor = Color.FromArgb(231, 76, 60);
            }
            finally
            {
                btnLearn.Enabled = true;
                btnLearn.Text = "AI学习";
                _learnCts?.Dispose();
                _learnCts = null;
            }
        }

        // ─── User learning focus dialog ─────────────────────────────────

        /// <summary>
        /// Show a dialog for the user to specify what the AI should focus on.
        /// Returns the focus text, or null if cancelled.
        /// </summary>
        private static string? ShowLearningFocusDialog()
        {
            using var dlg = new Form
            {
                Text = "AI学习重点设置",
                Size = new Size(520, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Padding = new Padding(16)
            };

            var lbl = new Label
            {
                Text = "请输入希望AI重点学习的内容方向（留空则提取全部知识）：",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold)
            };

            var hint = new Label
            {
                Text = "示例：\"提取所有热力学公式和交互作用系数\" 或 \"关注Fe-C-Si体系的活度数据\"",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                ForeColor = Color.Gray
            };

            var txt = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Microsoft YaHei UI", 10F),
                PlaceholderText = "例如：提取书中所有的热力学公式、交互作用系数表、相图数据..."
            };

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0)
            };

            var btnCancel = new Button
            {
                Text = "取消",
                Width = 80,
                Height = 34,
                DialogResult = DialogResult.Cancel
            };
            var btnOk = new Button
            {
                Text = "开始学习",
                Width = 100,
                Height = 34,
                BackColor = Color.FromArgb(142, 68, 173),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;

            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnOk);

            dlg.Controls.Add(txt);
            dlg.Controls.Add(hint);
            dlg.Controls.Add(lbl);
            dlg.Controls.Add(btnPanel);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            if (dlg.ShowDialog() == DialogResult.OK)
                return txt.Text.Trim();  // Empty string = no specific focus
            return null;  // Cancelled
        }

        // ─── Domain-specific system prompt ───────────────────────────────

        private static string BuildDomainSystemPrompt(bool isImageMode, string userFocus)
        {
            var sb = new StringBuilder();
            sb.AppendLine("你是一个冶金与材料科学知识提取专家，专门从学术文献中提取可用于热力学计算的关键知识。");

            if (isImageMode)
            {
                sb.AppendLine();
                sb.AppendLine("## 图片识别准确性要求（最高优先级）");
                sb.AppendLine("你正在识别扫描文档图片。请务必确保以下内容的识别精度：");
                sb.AppendLine("- **数字**：仔细辨认每一个数字，特别注意 0 和 O、1 和 l、6 和 b、8 和 B 的区分");
                sb.AppendLine("- **公式中的除号**：`/` 表示除法或分数。例如 `12/T` 表示 12÷T（T通常为温度K）");
                sb.AppendLine("  常见格式：`ΔG = -12345/T + 6.78T`，其中 `-12345/T` 表示 -12345除以T");
                sb.AppendLine("- **上下标**：注意区分上标和下标，如 e_C^Si（硅对碳的交互作用参数）");
                sb.AppendLine("- **希腊字母**：准确识别 α β γ δ ε ΔG ΔH ΔS 等符号");
                sb.AppendLine("- **负号与减号**：确认数值前的 - 是负号还是公式中的减号");
                sb.AppendLine("- **小数点**：仔细识别小数点位置，0.014 和 0.14 差别巨大");
                sb.AppendLine("- **表格数据**：按行列准确读取，保持数据与行/列标题的对应关系");
                sb.AppendLine("- 如果某处数字或公式无法确定，请用 [?] 标记，不要猜测");
            }

            // User's specific learning focus
            if (!string.IsNullOrWhiteSpace(userFocus))
            {
                sb.AppendLine();
                sb.AppendLine("## 用户指定的学习重点");
                sb.AppendLine($"用户要求重点关注：{userFocus}");
                sb.AppendLine("请优先提取与上述要求相关的知识，其他内容可以略过。");
            }

            sb.AppendLine();
            sb.AppendLine("## 提取重点领域");
            sb.AppendLine("请特别关注以下类型的知识：");
            sb.AppendLine("1. **热力学数据**：反应的ΔG、ΔH、ΔS值，及其温度依赖关系");
            sb.AppendLine("   公式格式示例：ΔG° = -123456/T + 45.67T (J/mol)，注意 /T 表示除以温度T");
            sb.AppendLine("2. **相图信息**：共晶温度/组成、液相线/固相线数据、相变温度");
            sb.AppendLine("3. **合金成分与性能**：合金体系、元素对性能的影响规律");
            sb.AppendLine("4. **交互作用系数**：Wagner交互作用参数 ε 或 e 值，注明溶剂、溶质、温度");
            sb.AppendLine("5. **活度系数**：各组分的活度系数γ值，注明模型和条件");
            sb.AppendLine("6. **工艺参数**：熔炼温度、脱氧/脱硫条件、精炼参数");
            sb.AppendLine("7. **反应方程式**：化学反应式及其平衡常数K值");
            sb.AppendLine("8. **物理常数**：熔点、密度、摩尔质量、热容等基础数据");
            sb.AppendLine();
            sb.AppendLine("## 输出格式要求");
            sb.AppendLine("1. 每个知识点独立成行，一行一个");
            sb.AppendLine("2. 用简洁的中文表述（英文原文请翻译，保留关键术语英文原名）");
            sb.AppendLine("3. 数值数据务必保留：具体数值、单位、条件（温度、压力、浓度范围等）");
            sb.AppendLine("4. 不要编号，不要加前缀符号（如-、•等）");
            sb.AppendLine("5. 忽略参考文献列表、图表编号、页眉页脚、致谢等非知识性内容");
            sb.AppendLine("6. 公式保留原始数学表达，用 / 表示除法，如：ΔG° = -29790/T - 6.49T + 63.18 (J/mol)");
            sb.AppendLine("7. 尽可能多地提取知识点，不要遗漏重要的定量数据");
            return sb.ToString();
        }

        // ─── Batch extraction with cross-batch context ───────────────────

        private static async Task<BatchResult> ExtractKnowledgeWithContext(
            List<MessageImage>? images, string? text, int batchNum, int totalBatches,
            string previousSummary, string userFocus, CancellationToken ct)
        {
            bool isImage = images != null && images.Count > 0;
            var systemPrompt = BuildDomainSystemPrompt(isImage, userFocus);

            // Build user message with optional cross-batch context
            var ub = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(previousSummary))
            {
                ub.AppendLine("【前文已提取的知识主题摘要】");
                ub.AppendLine(previousSummary);
                ub.AppendLine();
                ub.AppendLine("请避免重复以上已提取的内容，专注于新的知识点。");
                ub.AppendLine();
            }

            if (isImage)
            {
                ub.Append($"请仔细识别以下 {images!.Count} 页文档图片，特别注意数字、公式和表格的精确识别，并提取关键知识点");
            }
            else
                ub.Append("请从以下文献片段中提取关键知识点");

            if (totalBatches > 1) ub.Append($"（第{batchNum}/{totalBatches}批）");
            ub.AppendLine("：");

            if (!isImage && text != null)
            {
                ub.AppendLine();
                ub.AppendLine(text);
            }

            // Request a summary for context continuity (only when multi-batch)
            if (totalBatches > 1)
            {
                ub.AppendLine();
                ub.AppendLine("---");
                ub.AppendLine("在所有知识点之后，请另起一行以 [摘要] 开头，" +
                    "用1-2句话概括本批次提取的主要知识主题（不超过100字），用于给下一批次提供上下文。");
            }

            var userMsg = new ChatMessage
            {
                Role = "user",
                Content = ub.ToString(),
                Images = isImage ? images : null
            };

            var messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "system", Content = systemPrompt },
                userMsg
            };

            var response = await LlmBackend.Current!.ChatAsync(messages, null, ct);

            if (string.IsNullOrWhiteSpace(response.Content))
                return new BatchResult();

            // Parse: separate knowledge points from [摘要] summary
            var lines = response.Content.Split('\n');
            var points = new List<string>();
            var summaryParts = new StringBuilder();
            bool inSummary = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("[摘要]") || line.StartsWith("摘要：") || line.StartsWith("摘要:"))
                {
                    inSummary = true;
                    var s = Regex.Replace(line, @"^\[?摘要\]?[：:]?\s*", "");
                    if (!string.IsNullOrWhiteSpace(s)) summaryParts.AppendLine(s);
                    continue;
                }
                if (inSummary) { summaryParts.AppendLine(line); continue; }

                // Clean knowledge point line
                var cleaned = Regex.Replace(line, @"^\d+[\.\)、]\s*", "");
                cleaned = cleaned.TrimStart('-', '•', '*', '·', '>', ' ', '\t');
                if (cleaned.Length >= 5)
                    points.Add(cleaned);
            }

            // Build cumulative summary (cap at 500 chars)
            var newSummary = summaryParts.ToString().Trim();
            var cumulative = string.IsNullOrWhiteSpace(previousSummary)
                ? newSummary
                : previousSummary + "\n" + newSummary;
            if (cumulative.Length > 500)
                cumulative = "..." + cumulative[^500..];

            return new BatchResult { Points = points, Summary = cumulative };
        }

        // ─── Final synthesis pass ────────────────────────────────────────

        private static async Task<List<string>> SynthesizeKnowledgePoints(
            List<string> rawPoints, CancellationToken ct)
        {
            const int maxPerCall = 100;
            if (rawPoints.Count > maxPerCall)
            {
                var merged = new List<string>();
                for (int i = 0; i < rawPoints.Count; i += maxPerCall)
                {
                    var chunk = rawPoints.Skip(i).Take(maxPerCall).ToList();
                    merged.AddRange(await SynthesizeBatch(chunk, ct));
                }
                return merged;
            }
            return await SynthesizeBatch(rawPoints, ct);
        }

        private static async Task<List<string>> SynthesizeBatch(List<string> points, CancellationToken ct)
        {
            var systemPrompt =
                "你是一个知识库质量控制专家。请整理以下知识点：\n\n" +
                "1. 合并含义相同或高度相似的条目（保留更完整的表述）\n" +
                "2. 合并可组合的相关数据（如同一体系不同温度的数据可合并）\n" +
                "3. 删除过于笼统、缺乏具体信息的条目\n" +
                "4. 确保每条都包含完整条件信息（温度、浓度、体系等）\n" +
                "5. 修正明显的翻译错误或表述不清的内容\n\n" +
                "输出格式：每行一个知识点，不加编号和前缀符号。";

            var pointsList = string.Join("\n", points.Select((p, i) => $"{i + 1}. {p}"));

            var messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "system", Content = systemPrompt },
                new ChatMessage { Role = "user", Content = $"请整理以下 {points.Count} 个知识点：\n\n{pointsList}" }
            };

            var response = await LlmBackend.Current!.ChatAsync(messages, null, ct);
            if (string.IsNullOrWhiteSpace(response.Content))
                return points;

            var synthesized = response.Content.Split('\n')
                .Select(l => l.Trim())
                .Select(l => Regex.Replace(l, @"^\d+[\.\)、]\s*", ""))
                .Select(l => l.TrimStart('-', '•', '*', '·', '>', ' ', '\t'))
                .Where(l => l.Length >= 5)
                .ToList();

            // Safety: if synthesis removed >70% of points, keep originals
            if (synthesized.Count < points.Count * 0.3)
                return points;

            return synthesized;
        }

        // ─── Text chunking with overlap ──────────────────────────────────

        private static List<string> SplitTextIntoChunks(string text, int maxChunkSize, int overlap = 0)
        {
            if (text.Length <= maxChunkSize)
                return new List<string> { text };

            var chunks = new List<string>();
            int pos = 0;
            while (pos < text.Length)
            {
                int end = Math.Min(pos + maxChunkSize, text.Length);
                if (end < text.Length)
                {
                    // Try to break at paragraph boundary
                    int breakAt = text.LastIndexOf("\n\n", end, Math.Min(end - pos, 2000));
                    if (breakAt <= pos) breakAt = text.LastIndexOf('\n', end, Math.Min(end - pos, 1000));
                    if (breakAt > pos) end = breakAt + 1;
                }
                chunks.Add(text[pos..end]);
                int nextPos = end - overlap;
                if (nextPos <= pos) nextPos = end;
                pos = nextPos;
            }
            return chunks;
        }

        // ─── Keyword filtering for focused learning ──────────────────────

        /// <summary>
        /// Extract keywords from user's focus description, then filter text chunks
        /// to only those containing at least one keyword. This lets the AI skip
        /// irrelevant sections and focus on what the user wants.
        /// </summary>
        private static List<string> FilterChunksByKeywords(List<string> chunks, string userFocus)
        {
            // Extract keywords: split on common delimiters, keep meaningful terms
            var keywords = Regex.Split(userFocus, @"[\s,，、;；.。!！?？\-\+\(\)\[\]（）【】""''\""]+")
                .Select(w => w.Trim())
                .Where(w => w.Length >= 2)  // Skip single chars
                .Where(w => !IsStopWord(w))
                .ToList();

            if (keywords.Count == 0)
                return chunks; // No valid keywords, return all

            // Score each chunk by keyword hits
            var scored = chunks.Select((chunk, idx) =>
            {
                var lower = chunk.ToLowerInvariant();
                int hits = keywords.Count(kw => lower.Contains(kw.ToLowerInvariant()));
                return (Chunk: chunk, Index: idx, Hits: hits);
            }).ToList();

            // Keep chunks with at least one keyword hit
            var matched = scored.Where(s => s.Hits > 0)
                .OrderBy(s => s.Index)  // Preserve original order
                .Select(s => s.Chunk)
                .ToList();

            // If no chunks matched, return all (don't filter out everything)
            return matched.Count > 0 ? matched : chunks;
        }

        /// <summary>
        /// Common stop words to exclude from keyword filtering.
        /// </summary>
        private static bool IsStopWord(string word)
        {
            var stops = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "的", "了", "和", "与", "或", "是", "在", "有", "中", "为", "对", "等",
                "所有", "全部", "所述", "相关", "以及", "如何", "什么", "哪些",
                "提取", "学习", "关注", "重点", "数据", "信息", "内容", "知识",
                "the", "and", "or", "of", "in", "for", "to", "is", "are", "at",
                "all", "from", "with", "about", "this", "that", "these", "those"
            };
            return stops.Contains(word);
        }

        #endregion

        #region Delete

        /// <summary>
        /// Find the target row using every available method
        /// </summary>
        private DataGridViewRow? FindSelectedRow()
        {
            // 1. SelectedRows (standard DataGridView selection)
            if (dgvMemories.SelectedRows.Count > 0)
                return dgvMemories.SelectedRows[0];

            // 2. CurrentRow (persists after focus loss)
            if (dgvMemories.CurrentRow != null)
                return dgvMemories.CurrentRow;

            // 3. Tracked index from CellClick / SelectionChanged / RowEnter
            if (_trackedRowIndex >= 0 && _trackedRowIndex < dgvMemories.Rows.Count)
                return dgvMemories.Rows[_trackedRowIndex];

            return null;
        }

        private void DeleteSelected()
        {
            if (dgvMemories.Rows.Count == 0) return;

            var row = FindSelectedRow();
            if (row == null)
            {
                MessageBox.Show("请先在表格中点击选择要删除的知识条目。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var content = row.Cells["colContent"].Value?.ToString() ?? "";
            var brief = string.IsNullOrEmpty(content) ? "(空内容)" :
                        content.Length > 80 ? content[..80] + "..." : content;

            var result = MessageBox.Show($"确定删除这条知识？\n\n\"{brief}\"",
                "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            _memory.DeleteMemory(content);
            _trackedRowIndex = -1;
            LoadData();
        }

        private void DeleteAll()
        {
            if (dgvMemories.Rows.Count == 0) return;

            var result = MessageBox.Show("确定清空全部知识条目？此操作不可撤销。",
                "确认清空", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            for (int i = dgvMemories.Rows.Count - 1; i >= 0; i--)
            {
                var content = dgvMemories.Rows[i].Cells["colContent"].Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(content))
                    _memory.DeleteMemory(content);
            }
            _trackedRowIndex = -1;
            LoadData();
        }

        #endregion

        #region Export

        public void ExportToExcel()
        {
            if (dgvMemories.Rows.Count == 0)
            {
                MessageBox.Show("没有数据可导出。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "CSV Files|*.csv",
                FileName = $"AlloyAct_Knowledge_{DateTime.Now:yyyyMMdd}.csv"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var writer = new StreamWriter(sfd.FileName, false, Encoding.UTF8);
                writer.WriteLine("分类,知识内容,更新时间");
                foreach (DataGridViewRow row in dgvMemories.Rows)
                {
                    var cat = Escape(row.Cells[0].Value?.ToString() ?? "");
                    var content = Escape(row.Cells[1].Value?.ToString() ?? "");
                    var updated = Escape(row.Cells[2].Value?.ToString() ?? "");
                    writer.WriteLine($"{cat},{content},{updated}");
                }
                MessageBox.Show($"已导出到: {sfd.FileName}", "导出成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string Escape(string s) => $"\"{s.Replace("\"", "\"\"")}\"";

        #endregion
    }
}
