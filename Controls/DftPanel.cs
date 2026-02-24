using AlloyAct_Pro.DFT;

namespace AlloyAct_Pro.Controls
{
    /// <summary>
    /// DFT 数据导入面板 - 支持导入多种 DFT 软件的计算结果
    /// </summary>
    public class DftPanel : UserControl
    {
        public string PageTitle => "DFT Data Import";

        private readonly List<DftResult> _results = new();
        private DataGridView dgvResults = null!;
        private GroupBox grpDetail = null!;
        private Label lblDetailInfo = null!;
        private Button btnImportFile = null!;
        private Button btnImportFolder = null!;
        private Button btnClear = null!;
        private Label lblStatus = null!;

        public DftPanel()
        {
            InitializeUI();
            // 同步到 ThermodynamicTools 的全局引用
            LLM.ThermodynamicTools.DftResults = _results;
        }

        private void InitializeUI()
        {
            this.SuspendLayout();

            // ===== 顶部工具栏 =====
            var toolPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = AppTheme.ContentBg
            };

            btnImportFile = new Button
            {
                Text = "📂 Import File",
                Font = AppTheme.BodyFont,
                BackColor = AppTheme.CalcBtnBg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(140, 34),
                Cursor = Cursors.Hand
            };
            btnImportFile.FlatAppearance.BorderSize = 0;
            btnImportFile.Click += BtnImportFile_Click;

            btnImportFolder = new Button
            {
                Text = "📁 Import Folder",
                Font = AppTheme.BodyFont,
                BackColor = AppTheme.CalcBtnBg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(140, 34),
                Cursor = Cursors.Hand
            };
            btnImportFolder.FlatAppearance.BorderSize = 0;
            btnImportFolder.Click += BtnImportFolder_Click;

            btnClear = new Button
            {
                Text = "🗑 Clear All",
                Font = AppTheme.BodyFont,
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 34),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) =>
            {
                _results.Clear();
                RefreshGrid();
                lblDetailInfo.Text = "";
                lblStatus.Text = "All data cleared.";
            };

            lblStatus = new Label
            {
                Text = $"Supported: {string.Join(", ", DftParserRegistry.GetSupportedSoftware())}",
                Font = new Font(AppTheme.BodyFont.FontFamily, 8.5f),
                ForeColor = Color.FromArgb(127, 140, 141),
                AutoSize = true,
                Padding = new Padding(8, 10, 0, 0)
            };

            toolPanel.Controls.AddRange(new Control[] { btnImportFile, btnImportFolder, btnClear, lblStatus });

            // ===== 结果列表（上半部分） =====
            dgvResults = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            AppTheme.StyleDataGridView(dgvResults);

            // 定义列
            dgvResults.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Software", HeaderText = "Software", FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "Formula", HeaderText = "Formula", FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "Atoms", HeaderText = "Atoms", FillWeight = 6 },
                new DataGridViewTextBoxColumn { Name = "Energy_eV", HeaderText = "Total Energy (eV)", FillWeight = 16 },
                new DataGridViewTextBoxColumn { Name = "Energy_per_atom", HeaderText = "E/atom (eV)", FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "Volume", HeaderText = "Volume (ų)", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "Converged", HeaderText = "Converged", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "Method", HeaderText = "XC Func.", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "File", HeaderText = "Source File", FillWeight = 16 }
            });

            dgvResults.SelectionChanged += DgvResults_SelectionChanged;

            // ===== 详情面板（下半部分） =====
            grpDetail = new GroupBox
            {
                Text = "Details",
                Font = AppTheme.BodyFont,
                Dock = DockStyle.Bottom,
                Height = 200,
                Padding = new Padding(8)
            };

            lblDetailInfo = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9f),
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft
            };
            grpDetail.Controls.Add(lblDetailInfo);

            // ===== 布局 =====
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300,
                BackColor = AppTheme.ContentBg
            };
            splitContainer.Panel1.Controls.Add(dgvResults);
            splitContainer.Panel2.Controls.Add(grpDetail);

            this.Controls.Add(splitContainer);
            this.Controls.Add(toolPanel);

            this.ResumeLayout();
        }

        // ===== 事件处理 =====

        private void BtnImportFile_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Select DFT Output File",
                Filter = DftParserRegistry.GetFileFilter(),
                Multiselect = true
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            int success = 0, fail = 0;
            foreach (var file in ofd.FileNames)
            {
                if (TryImport(file)) success++;
                else fail++;
            }

            RefreshGrid();
            lblStatus.Text = $"Imported {success} file(s)" + (fail > 0 ? $", {fail} failed" : "");
        }

        private void BtnImportFolder_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Select folder containing DFT output files",
                UseDescriptionForTitle = true
            };

            if (fbd.ShowDialog() != DialogResult.OK) return;

            int success = 0, fail = 0;
            foreach (var file in Directory.GetFiles(fbd.SelectedPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var result = DftParserRegistry.AutoParse(file);
                    if (result != null)
                    {
                        _results.Add(result);
                        success++;
                    }
                }
                catch { fail++; }
            }

            RefreshGrid();
            lblStatus.Text = $"Scanned folder: {success} imported" + (fail > 0 ? $", {fail} failed" : "");
        }

        private bool TryImport(string filePath)
        {
            try
            {
                var result = DftParserRegistry.AutoParse(filePath);
                if (result == null)
                {
                    MessageBox.Show($"Could not identify DFT software for:\n{filePath}",
                        "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                _results.Add(result);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing {Path.GetFileName(filePath)}:\n{ex.Message}",
                    "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void RefreshGrid()
        {
            dgvResults.Rows.Clear();
            foreach (var r in _results)
            {
                dgvResults.Rows.Add(
                    r.SourceSoftware,
                    r.Formula,
                    r.AtomCount,
                    FormatDouble(r.TotalEnergy_eV, "F6"),
                    FormatDouble(r.EnergyPerAtom_eV, "F6"),
                    FormatDouble(r.Volume, "F2"),
                    r.IsConverged ? "✓" : "✗",
                    r.Method,
                    Path.GetFileName(r.SourceFile)
                );
            }
        }

        private void DgvResults_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvResults.SelectedRows.Count == 0 || dgvResults.SelectedRows[0].Index >= _results.Count)
            {
                lblDetailInfo.Text = "";
                return;
            }

            var r = _results[dgvResults.SelectedRows[0].Index];
            var lines = new List<string>
            {
                $"Software:    {r.SourceSoftware}",
                $"Formula:     {r.Formula}  ({r.AtomCount} atoms)",
                $"File:        {r.SourceFile}",
                "",
                "─── Energy ───",
                $"Total Energy:      {FormatDouble(r.TotalEnergy_eV, "F8")} eV  ({FormatDouble(r.TotalEnergy_kJ_mol, "F2")} kJ/mol)",
                $"Energy/atom:       {FormatDouble(r.EnergyPerAtom_eV, "F8")} eV/atom",
                $"Fermi Energy:      {FormatDouble(r.FermiEnergy_eV, "F4")} eV",
                $"Band Gap:          {FormatDouble(r.BandGap_eV, "F4")} eV",
            };

            if (r.FormationEnergy_eV_atom.HasValue)
                lines.Add($"Formation Energy:  {r.FormationEnergy_eV_atom:F6} eV/atom");

            lines.AddRange(new[]
            {
                "",
                "─── Structure ───",
                $"Volume:            {FormatDouble(r.Volume, "F4")} ų",
                $"Lattice:           {(r.LatticeParameters.Length >= 3 ? $"a={r.LatticeParameters[0]:F4} b={r.LatticeParameters[1]:F4} c={r.LatticeParameters[2]:F4} Å" : "N/A")}",
                $"Pressure:          {FormatDouble(r.Pressure_GPa, "F4")} GPa",
                $"Max Force:         {FormatDouble(r.MaxForce_eV_A, "F6")} eV/Å",
                "",
                "─── Calculation ───",
                $"Method:            {r.Method}",
                $"Cutoff:            {FormatDouble(r.EnergyCutoff_eV, "F1")} eV",
                $"K-points:          {r.KPoints}",
                $"Converged:         {(r.IsConverged ? "Yes" : "No")}",
                $"Ion steps:         {r.IonSteps}",
                $"Spin Polarized:    {(r.SpinPolarized ? "Yes" : "No")}",
                $"Magnetization:     {FormatDouble(r.TotalMagnetization, "F4")} μB"
            });

            lblDetailInfo.Text = string.Join(Environment.NewLine, lines);
        }

        private static string FormatDouble(double val, string fmt)
        {
            return double.IsNaN(val) ? "N/A" : val.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);
        }

        public void ExportToExcel()
        {
            if (_results.Count == 0)
            {
                MessageBox.Show("No DFT data to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            myFunctions.saveToExcel(dgvResults);
        }
    }
}
