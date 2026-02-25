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
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));   // Description + stats
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Table
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));  // Add form

            // === Row 0: Description + search ===
            var topPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            topPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var descPanel = new Panel { Dock = DockStyle.Fill };
            var lblDesc = new Label
            {
                Text = "管理 AI 助手的知识库。添加的知识将自动注入到 AI 的系统提示词中，帮助 AI 更好地理解您的需求。",
                Font = new Font("Microsoft YaHei UI", 9.5F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblStats = new Label
            {
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185),
                Dock = DockStyle.Right,
                Width = 120,
                TextAlign = ContentAlignment.MiddleRight
            };
            descPanel.Controls.Add(lblDesc);
            descPanel.Controls.Add(lblStats);
            topPanel.Controls.Add(descPanel, 0, 0);

            // Search
            var searchPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 8, 0, 8) };
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
                Width = 140,
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

            // === Row 2: Add / Delete form ===
            var formPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12, 8, 12, 8)
            };

            var formTitle = new Label
            {
                Text = "添加知识",
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Dock = DockStyle.Top,
                Height = 26
            };

            var inputRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0)
            };
            inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));  // Category
            inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // Content
            inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));   // Add btn
            inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));  // Delete btns
            inputRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Category combo
            var catPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 6, 0) };
            cboCategory = new ComboBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboCategory.Items.AddRange(new object[]
            {
                "preference — 默认计算设置",
                "alloy_system — 常用合金体系",
                "calculation — 计算规则与经验",
                "general — 其他"
            });
            cboCategory.SelectedIndex = 0;
            catPanel.Controls.Add(cboCategory);
            inputRow.Controls.Add(catPanel, 0, 0);

            // Content textbox
            var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 6, 0) };
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
            inputRow.Controls.Add(contentPanel, 1, 0);

            // Add button
            var addPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 6, 8) };
            btnAdd = new Button
            {
                Text = "添加",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAdd.Click += (s, e) => AddMemory();
            addPanel.Controls.Add(btnAdd);
            inputRow.Controls.Add(addPanel, 2, 0);

            // Delete buttons
            var delPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(0, 4, 0, 4),
                WrapContents = false
            };
            btnDelete = new Button
            {
                Text = "删除选中",
                Width = 168,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 4)
            };
            btnDelete.Click += (s, e) => DeleteSelected();

            btnDeleteAll = new Button
            {
                Text = "清空全部",
                Width = 168,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(231, 76, 60) },
                BackColor = Color.White,
                ForeColor = Color.FromArgb(231, 76, 60),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDeleteAll.Click += (s, e) => DeleteAll();

            delPanel.Controls.Add(btnDelete);
            delPanel.Controls.Add(btnDeleteAll);
            inputRow.Controls.Add(delPanel, 3, 0);

            formPanel.Controls.Add(inputRow);
            formPanel.Controls.Add(formTitle);
            mainLayout.Controls.Add(formPanel, 0, 2);

            Controls.Add(mainLayout);
        }

        #endregion

        #region Data

        private static readonly Dictionary<string, string> CategoryLabels = new()
        {
            ["preference"] = "默认计算设置",
            ["alloy_system"] = "常用合金体系",
            ["calculation"] = "计算规则与经验",
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

        private void AddMemory()
        {
            var content = txtContent.Text.Trim();
            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("请输入知识内容。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var categoryText = cboCategory.SelectedItem?.ToString() ?? "";
            var category = categoryText.Split('—')[0].Trim().Split(' ')[0].Trim();

            _memory.SaveMemory(content, category);
            txtContent.Clear();
            LoadData();
        }

        private void DeleteSelected()
        {
            if (dgvMemories.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择要删除的知识条目。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var content = dgvMemories.SelectedRows[0].Cells["colContent"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(content)) return;

            var result = MessageBox.Show($"确定删除这条知识？\n\n\"{(content.Length > 80 ? content[..80] + "..." : content)}\"",
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

            // Delete all one by one
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
