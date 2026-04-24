using AlloyAct_Pro.LLM;

namespace AlloyAct_Pro.Controls
{
    /// <summary>
    /// 模型管理对话框：允许用户为每个提供商增删改模型条目、标记工具调用支持
    /// 保存到 ModelOverrideStore，下次启动仍生效
    /// </summary>
    internal class ModelManagerDialog : Form
    {
        private ComboBox cboProvider = null!;
        private TextBox txtBaseUrl = null!;
        private TextBox txtApiKey = null!;
        private Label lblApiKey = null!;
        private Label lblBaseUrl = null!;
        private DataGridView dgvModels = null!;
        private Label lblStatus = null!;

        private string _currentProvider = "";

        public ModelManagerDialog(string? initialProvider = null, string? initialApiKey = null, string? initialBaseUrl = null)
        {
            InitializeDialog();
            if (!string.IsNullOrWhiteSpace(initialProvider))
            {
                var idx = cboProvider.Items.IndexOf(initialProvider);
                if (idx >= 0) cboProvider.SelectedIndex = idx;
            }
            if (!string.IsNullOrWhiteSpace(initialApiKey)) txtApiKey.Text = initialApiKey;
            if (!string.IsNullOrWhiteSpace(initialBaseUrl)) txtBaseUrl.Text = initialBaseUrl;
        }

        private void InitializeDialog()
        {
            Text = "模型管理";
            ClientSize = new Size(780, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(700, 480);
            MinimizeBox = false;
            MaximizeBox = true;
            BackColor = Color.White;
            ShowInTaskbar = false;
            Font = AppTheme.BodyFont;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(16)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));   // Provider row
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));   // URL / Key row
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Grid
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));   // Buttons

            // === Row 0: Provider ===
            var providerRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false
            };
            providerRow.Controls.Add(new Label
            {
                Text = "提供商:",
                Font = AppTheme.BodyFont,
                AutoSize = true,
                Margin = new Padding(0, 8, 4, 0)
            });
            cboProvider = new ComboBox
            {
                Font = AppTheme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 150,
                Margin = new Padding(0, 4, 12, 0)
            };
            cboProvider.Items.AddRange(ProviderRegistry.GetProviderNames());
            cboProvider.SelectedIndexChanged += (s, e) => OnProviderChanged();
            providerRow.Controls.Add(cboProvider);

            lblStatus = new Label
            {
                Text = "",
                Font = new Font("Microsoft YaHei UI", 9F),
                AutoSize = true,
                Margin = new Padding(8, 8, 0, 0),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            providerRow.Controls.Add(lblStatus);
            root.Controls.Add(providerRow, 0, 0);

            // === Row 1: Base URL / API Key ===
            var urlRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false
            };
            lblBaseUrl = new Label
            {
                Text = "Base URL:",
                Font = AppTheme.BodyFont,
                AutoSize = true,
                Margin = new Padding(0, 8, 4, 0)
            };
            urlRow.Controls.Add(lblBaseUrl);
            txtBaseUrl = new TextBox
            {
                Font = AppTheme.BodyFont,
                Width = 260,
                Margin = new Padding(0, 4, 12, 0)
            };
            urlRow.Controls.Add(txtBaseUrl);

            lblApiKey = new Label
            {
                Text = "API Key:",
                Font = AppTheme.BodyFont,
                AutoSize = true,
                Margin = new Padding(0, 8, 4, 0)
            };
            urlRow.Controls.Add(lblApiKey);
            txtApiKey = new TextBox
            {
                Font = AppTheme.BodyFont,
                UseSystemPasswordChar = true,
                Width = 200,
                Margin = new Padding(0, 4, 0, 0),
                PlaceholderText = "（可选，用于从 API 拉取模型列表）"
            };
            urlRow.Controls.Add(txtApiKey);
            root.Controls.Add(urlRow, 0, 1);

            // === Row 2: Grid ===
            dgvModels = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = true,
                Font = AppTheme.BodyFont
            };
            AppTheme.StyleDataGridView(dgvModels);
            dgvModels.RowHeadersVisible = true;
            dgvModels.AllowUserToAddRows = true;

            var colName = new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "模型名",
                FillWeight = 45,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding = new Padding(6, 0, 6, 0),
                    Font = AppTheme.BodyFont
                }
            };
            var colTools = new DataGridViewCheckBoxColumn
            {
                Name = "SupportsTools",
                HeaderText = "工具调用",
                FillWeight = 15
            };
            var colNote = new DataGridViewTextBoxColumn
            {
                Name = "Note",
                HeaderText = "备注",
                FillWeight = 40,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding = new Padding(6, 0, 6, 0),
                    Font = AppTheme.BodyFont
                }
            };
            dgvModels.Columns.AddRange(colName, colTools, colNote);
            root.Controls.Add(dgvModels, 0, 2);

            // === Row 3: Buttons ===
            var btnRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = false,
                Padding = new Padding(0, 12, 0, 0)
            };

            var btnCancel = MakeBtn("取消", Color.FromArgb(149, 165, 166));
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnSave = MakeBtn("保存", Color.FromArgb(39, 174, 96));
            btnSave.Click += BtnSave_Click;

            var btnReset = MakeBtn("还原默认", Color.FromArgb(230, 126, 34));
            btnReset.Click += BtnReset_Click;

            var btnFetch = MakeBtn("🔄 从 API 获取", Color.FromArgb(52, 152, 219));
            btnFetch.Width = 130;
            btnFetch.Click += BtnFetch_Click;

            var btnRemove = MakeBtn("删除所选", Color.FromArgb(231, 76, 60));
            btnRemove.Click += BtnRemove_Click;

            var btnAdd = MakeBtn("添加", Color.FromArgb(52, 152, 219));
            btnAdd.Click += BtnAdd_Click;

            btnRow.Controls.AddRange(new Control[] { btnCancel, btnSave, btnReset, btnFetch, btnRemove, btnAdd });
            root.Controls.Add(btnRow, 0, 3);

            Controls.Add(root);

            if (cboProvider.Items.Count > 0) cboProvider.SelectedIndex = 0;
        }

        private Button MakeBtn(string text, Color bg)
        {
            var b = new Button
            {
                Text = text,
                Font = AppTheme.CalcBtnFont,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 36),
                Margin = new Padding(6, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private void OnProviderChanged()
        {
            _currentProvider = cboProvider.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(_currentProvider)) return;

            if (ProviderRegistry.Providers.TryGetValue(_currentProvider, out var cfg))
            {
                if (string.IsNullOrWhiteSpace(txtBaseUrl.Text) || !UserEditedBaseUrl())
                    txtBaseUrl.Text = cfg.BaseUrl;
            }

            LoadGridForProvider(_currentProvider);
        }

        private bool UserEditedBaseUrl()
        {
            // 每次切换提供商都强制同步默认 URL，简化逻辑
            return false;
        }

        private void LoadGridForProvider(string provider)
        {
            dgvModels.Rows.Clear();

            var ov = ModelOverrideStore.Instance.GetOverride(provider);
            if (ov != null && ov.Models.Count > 0)
            {
                foreach (var m in ov.Models)
                    dgvModels.Rows.Add(m.Name, m.SupportsTools, m.Note);
                lblStatus.Text = $"当前使用自定义列表（{ov.Models.Count} 个模型）";
                lblStatus.ForeColor = Color.FromArgb(41, 128, 185);
            }
            else if (ProviderRegistry.Providers.TryGetValue(provider, out var cfg))
            {
                foreach (var name in cfg.ModelList)
                {
                    bool supports = !ChatAgent.IsToolUnsupportedModel(name);
                    dgvModels.Rows.Add(name, supports, "");
                }
                lblStatus.Text = $"当前使用内置列表（{cfg.ModelList.Length} 个模型）";
                lblStatus.ForeColor = Color.FromArgb(100, 100, 100);
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            dgvModels.Rows.Add("", true, "");
            // 滚动到最后一行并进入编辑
            if (dgvModels.Rows.Count > 0)
            {
                var idx = dgvModels.Rows.Count - 1;
                dgvModels.CurrentCell = dgvModels.Rows[idx].Cells["Name"];
                dgvModels.BeginEdit(true);
            }
        }

        private void BtnRemove_Click(object? sender, EventArgs e)
        {
            var rows = dgvModels.SelectedRows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
            foreach (var r in rows)
                dgvModels.Rows.Remove(r);
        }

        private async void BtnFetch_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentProvider)) return;
            lblStatus.Text = "正在从 API 获取模型列表...";
            lblStatus.ForeColor = Color.FromArgb(52, 152, 219);

            try
            {
                var models = await LlmBackend.FetchModelsAsync(
                    _currentProvider,
                    txtBaseUrl.Text.Trim(),
                    txtApiKey.Text.Trim());

                if (models == null || models.Length == 0)
                {
                    lblStatus.Text = "未返回任何模型（可能需要 API Key 或网络不可用）";
                    lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
                    return;
                }

                // 现有名字集合，用于去重合并
                var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (DataGridViewRow row in dgvModels.Rows)
                {
                    if (row.IsNewRow) continue;
                    var n = row.Cells["Name"].Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(n)) existing.Add(n);
                }

                int added = 0;
                foreach (var name in models)
                {
                    if (string.IsNullOrWhiteSpace(name) || existing.Contains(name)) continue;
                    bool supports = !ChatAgent.IsToolUnsupportedModel(name);
                    dgvModels.Rows.Add(name, supports, "来自 API");
                    existing.Add(name);
                    added++;
                }

                lblStatus.Text = $"API 返回 {models.Length} 个模型，新增 {added} 个";
                lblStatus.ForeColor = Color.FromArgb(39, 174, 96);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"获取失败: {ex.Message}";
                lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
            }
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentProvider)) return;
            var confirm = MessageBox.Show(
                $"确定将提供商「{_currentProvider}」的模型列表还原为内置默认吗？\n\n自定义条目将被删除（不可撤销）。",
                "确认还原",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            ModelOverrideStore.Instance.ResetProvider(_currentProvider);
            LoadGridForProvider(_currentProvider);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentProvider))
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            var models = new List<ModelEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataGridViewRow row in dgvModels.Rows)
            {
                if (row.IsNewRow) continue;
                var name = row.Cells["Name"].Value?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (seen.Contains(name)) continue;
                seen.Add(name);

                bool supports = row.Cells["SupportsTools"].Value is bool b ? b : true;
                string note = row.Cells["Note"].Value?.ToString() ?? "";
                models.Add(new ModelEntry { Name = name, SupportsTools = supports, Note = note });
            }

            if (models.Count == 0)
            {
                var confirm = MessageBox.Show(
                    "模型列表为空。保存将等同于「还原默认」（使用内置列表）。是否继续？",
                    "空列表确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;
                ModelOverrideStore.Instance.ResetProvider(_currentProvider);
            }
            else
            {
                var ov = new ProviderOverride
                {
                    DefaultModel = models[0].Name,
                    Models = models
                };
                ModelOverrideStore.Instance.SetOverride(_currentProvider, ov);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
