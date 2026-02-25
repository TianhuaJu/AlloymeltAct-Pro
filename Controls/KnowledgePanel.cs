using AlloyAct_Pro.LLM;
using System.Text.Json;

namespace AlloyAct_Pro.Controls
{
    /// <summary>
    /// 知识学习面板 — 管理 AI 助手的记忆/知识条目，支持文献导入
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
        private Button btnDelete = null!;
        private Button btnDeleteAll = null!;
        private TextBox txtSearch = null!;
        private Label lblStats = null!;

        // Track selected row index (survives focus change to buttons)
        private int _lastSelectedRowIndex = -1;

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
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));  // Add form

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

            // Track selection so it survives focus change
            dgvMemories.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0) _lastSelectedRowIndex = e.RowIndex;
            };
            dgvMemories.SelectionChanged += (s, e) =>
            {
                if (dgvMemories.CurrentRow != null)
                    _lastSelectedRowIndex = dgvMemories.CurrentRow.Index;
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
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));  // Title
            formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Content input
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Buttons row

            // Title
            var formTitle = new Label
            {
                Text = "添加知识条目  (Ctrl+Enter 快捷添加)",
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };
            formLayout.Controls.Add(formTitle, 0, 0);

            // Content input
            var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 6) };
            txtContent = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 10F),
                PlaceholderText = "输入知识内容，例如：我常用的温度是1873K、默认合金体系是Fe基...",
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

            // Button row
            var btnRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 1,
                Padding = new Padding(0)
            };
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));  // Category
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));   // Add
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));  // Import
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // Spacer
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));  // Delete
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));  // DeleteAll
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));    // End pad
            btnRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Category
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
            btnRow.Controls.Add(catWrap, 0, 0);

            // Add button
            btnAdd = CreateButton("添加", Color.FromArgb(39, 174, 96), Color.White);
            btnAdd.Click += (s, e) => AddMemory();
            btnRow.Controls.Add(WrapBtn(btnAdd, 0, 4, 4, 4), 1, 0);

            // Import button
            btnImport = CreateButton("导入文献", Color.FromArgb(41, 128, 185), Color.White);
            btnImport.Click += (s, e) => ImportFile();
            btnRow.Controls.Add(WrapBtn(btnImport, 0, 4, 4, 4), 2, 0);

            // Spacer
            btnRow.Controls.Add(new Panel { Dock = DockStyle.Fill }, 3, 0);

            // Delete selected
            btnDelete = CreateButton("删除选中", Color.FromArgb(231, 76, 60), Color.White);
            btnDelete.Click += (s, e) => DeleteSelected();
            btnRow.Controls.Add(WrapBtn(btnDelete, 0, 4, 4, 4), 4, 0);

            // Delete all
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
            btnRow.Controls.Add(WrapBtn(btnDeleteAll, 0, 4, 0, 4), 5, 0);

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
            _lastSelectedRowIndex = -1;
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

        private void ImportFile()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "导入文献 / 知识文件",
                Filter = "文本文件|*.txt;*.md;*.csv|所有文件|*.*",
                Multiselect = false
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var text = File.ReadAllText(ofd.FileName).Trim();
                if (string.IsNullOrEmpty(text))
                {
                    MessageBox.Show("文件内容为空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Split into paragraphs (double newline or single newline with content)
                var paragraphs = SplitIntoParagraphs(text);
                var category = GetSelectedCategory();

                var preview = $"文件: {Path.GetFileName(ofd.FileName)}\n" +
                              $"大小: {new FileInfo(ofd.FileName).Length / 1024.0:F1} KB\n" +
                              $"将导入 {paragraphs.Count} 条知识，分类: {CategoryLabels.GetValueOrDefault(category, category)}\n\n" +
                              $"前3条预览:\n";
                for (int i = 0; i < Math.Min(3, paragraphs.Count); i++)
                {
                    var p = paragraphs[i];
                    preview += $"  {i + 1}. {(p.Length > 60 ? p[..60] + "..." : p)}\n";
                }

                var result = MessageBox.Show(preview + "\n确定导入？", "导入确认",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;

                int count = 0;
                foreach (var para in paragraphs)
                {
                    _memory.SaveMemory(para, category);
                    count++;
                }

                LoadData();
                MessageBox.Show($"成功导入 {count} 条知识。", "导入完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Split text into meaningful paragraphs, skipping empty lines.
        /// Paragraphs shorter than 10 chars are merged with the next one.
        /// </summary>
        private static List<string> SplitIntoParagraphs(string text)
        {
            var lines = text.Split('\n')
                .Select(l => l.TrimEnd('\r').Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            var result = new List<string>();
            var buffer = "";

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(buffer))
                {
                    buffer = line;
                }
                else if (buffer.Length < 10)
                {
                    // Short fragment — merge
                    buffer += " " + line;
                }
                else
                {
                    result.Add(buffer);
                    buffer = line;
                }
            }
            if (!string.IsNullOrEmpty(buffer))
                result.Add(buffer);

            return result;
        }

        private void DeleteSelected()
        {
            if (_lastSelectedRowIndex < 0 || _lastSelectedRowIndex >= dgvMemories.Rows.Count)
            {
                MessageBox.Show("请先在表格中点击选择要删除的知识条目。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dgvMemories.Rows[_lastSelectedRowIndex];
            var content = row.Cells["colContent"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(content)) return;

            var brief = content.Length > 80 ? content[..80] + "..." : content;
            var result = MessageBox.Show($"确定删除这条知识？\n\n\"{brief}\"",
                "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            _memory.DeleteMemory(content);
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
                using var writer = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                writer.WriteLine("分类,知识内容,更新时间");
                foreach (DataGridViewRow row in dgvMemories.Rows)
                {
                    var cat = Escape(row.Cells[0].Value?.ToString() ?? "");
                    var content = Escape(row.Cells[1].Value?.ToString() ?? "");
                    var updated = Escape(row.Cells[2].Value?.ToString() ?? "");
                    writer.WriteLine($"{cat},{content},{updated}");
                }
                MessageBox.Show($"已导出到: {sfd.FileName}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
