using AlloyAct_Pro.LLM;
using System.Text.Json;

namespace AlloyAct_Pro.Controls
{
    /// <summary>
    /// 知识学习面板 — 管理 AI 助手的记忆/知识条目
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
        private Button btnDelete = null!;
        private Button btnDeleteAll = null!;
        private TextBox txtSearch = null!;
        private Label lblStats = null!;

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
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));  // Add form

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

            // Search
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

            // === Row 2: Add / Delete form (2 rows) ===
            var formPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12, 6, 12, 6)
            };

            var formLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));  // Title
            formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Content input
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // Category + buttons

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

            // Content input (full width)
            var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 4) };
            txtContent = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 10F),
                PlaceholderText = "输入知识内容，例如：我常用的温度是1873K、默认合金体系是Fe基、计算活度时默认用Wagner模型...",
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

            // Bottom row: Category + Add + Delete buttons
            var bottomRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(0)
            };
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));  // Category
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));   // Add btn
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // Spacer
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));  // Delete btn
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));  // DeleteAll btn
            bottomRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Category combo
            var catPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 6, 2) };
            cboCategory = new ComboBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DropDownWidth = 260
            };
            cboCategory.Items.AddRange(new object[]
            {
                "knowledge \u2014 \u77e5\u8bc6",
                "preference \u2014 \u8bbe\u7f6e",
                "alloy_system \u2014 \u5408\u91d1\u4f53\u7cfb",
                "calculation \u2014 \u8ba1\u7b97\u7ecf\u9a8c",
                "general \u2014 \u5176\u4ed6"
            });
            cboCategory.SelectedIndex = 0;
            catPanel.Controls.Add(cboCategory);
            bottomRow.Controls.Add(catPanel, 0, 0);

            // Add button
            var addPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 6, 2) };
            btnAdd = new Button
            {
                Text = "\u6dfb\u52a0",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAdd.Click += (s, e) => AddMemory();
            addPanel.Controls.Add(btnAdd);
            bottomRow.Controls.Add(addPanel, 1, 0);

            // Spacer
            bottomRow.Controls.Add(new Panel { Dock = DockStyle.Fill }, 2, 0);

            // Delete selected
            var delPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 4, 2) };
            btnDelete = new Button
            {
                Text = "\u5220\u9664\u9009\u4e2d",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDelete.Click += (s, e) => DeleteSelected();
            delPanel.Controls.Add(btnDelete);
            bottomRow.Controls.Add(delPanel, 3, 0);

            // Delete all
            var delAllPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 0, 2) };
            btnDeleteAll = new Button
            {
                Text = "\u6e05\u7a7a\u5168\u90e8",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(231, 76, 60) },
                BackColor = Color.White,
                ForeColor = Color.FromArgb(231, 76, 60),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDeleteAll.Click += (s, e) => DeleteAll();
            delAllPanel.Controls.Add(btnDeleteAll);
            bottomRow.Controls.Add(delAllPanel, 4, 0);

            formLayout.Controls.Add(bottomRow, 0, 2);

            formPanel.Controls.Add(formLayout);
            mainLayout.Controls.Add(formPanel, 0, 2);

            Controls.Add(mainLayout);
        }

        #endregion

        #region Data

        private static readonly Dictionary<string, string> CategoryLabels = new()
        {
            ["preference"] = "\u8bbe\u7f6e",
            ["alloy_system"] = "\u5408\u91d1\u4f53\u7cfb",
            ["calculation"] = "\u8ba1\u7b97\u7ecf\u9a8c",
            ["knowledge"] = "\u77e5\u8bc6",
            ["general"] = "\u5176\u4ed6"
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
            dgvMemories.Rows.Clear();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var count = root.GetProperty("count").GetInt32();
                lblStats.Text = $"\u5171 {count} \u6761\u77e5\u8bc6";

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
                lblStats.Text = "\u5171 0 \u6761\u77e5\u8bc6";
            }
        }

        private void AddMemory()
        {
            var content = txtContent.Text.Trim();
            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("\u8bf7\u8f93\u5165\u77e5\u8bc6\u5185\u5bb9\u3002", "\u63d0\u793a", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var categoryText = cboCategory.SelectedItem?.ToString() ?? "";
            var category = categoryText.Split('\u2014')[0].Trim().Split(' ')[0].Trim();

            _memory.SaveMemory(content, category);
            txtContent.Clear();
            LoadData();
        }

        private void DeleteSelected()
        {
            if (dgvMemories.SelectedRows.Count == 0)
            {
                MessageBox.Show("\u8bf7\u5148\u9009\u62e9\u8981\u5220\u9664\u7684\u77e5\u8bc6\u6761\u76ee\u3002", "\u63d0\u793a", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var content = dgvMemories.SelectedRows[0].Cells["colContent"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(content)) return;

            var result = MessageBox.Show($"\u786e\u5b9a\u5220\u9664\u8fd9\u6761\u77e5\u8bc6\uff1f\n\n\"{(content.Length > 80 ? content[..80] + "..." : content)}\"",
                "\u786e\u8ba4\u5220\u9664", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            _memory.DeleteMemory(content);
            LoadData();
        }

        private void DeleteAll()
        {
            if (dgvMemories.Rows.Count == 0) return;

            var result = MessageBox.Show("\u786e\u5b9a\u6e05\u7a7a\u5168\u90e8\u77e5\u8bc6\u6761\u76ee\uff1f\u6b64\u64cd\u4f5c\u4e0d\u53ef\u64a4\u9500\u3002",
                "\u786e\u8ba4\u6e05\u7a7a", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
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
                MessageBox.Show("\u6ca1\u6709\u6570\u636e\u53ef\u5bfc\u51fa\u3002", "\u63d0\u793a", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                writer.WriteLine("\u5206\u7c7b,\u77e5\u8bc6\u5185\u5bb9,\u66f4\u65b0\u65f6\u95f4");
                foreach (DataGridViewRow row in dgvMemories.Rows)
                {
                    var cat = Escape(row.Cells[0].Value?.ToString() ?? "");
                    var content = Escape(row.Cells[1].Value?.ToString() ?? "");
                    var updated = Escape(row.Cells[2].Value?.ToString() ?? "");
                    writer.WriteLine($"{cat},{content},{updated}");
                }
                MessageBox.Show($"\u5df2\u5bfc\u51fa\u5230: {sfd.FileName}", "\u5bfc\u51fa\u6210\u529f", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"\u5bfc\u51fa\u5931\u8d25: {ex.Message}", "\u9519\u8bef", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string Escape(string s) => $"\"{s.Replace("\"", "\"\"")}\"";

        #endregion
    }
}
