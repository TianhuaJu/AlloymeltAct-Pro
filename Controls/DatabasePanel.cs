using System.Data;
using System.Data.SQLite;

namespace AlloyAct_Pro.Controls
{
    public partial class DatabasePanel : UserControl
    {
        public string PageTitle => "Database Management";

        private const string Password = "uem-thermo";
        private const string DbPath = "Data Source =data\\DataBase.db";
        private bool isUnlocked = false;

        public DatabasePanel()
        {
            InitializeComponent();
        }

        public void ExportToExcel()
        {
            DataGridView? activeDgv = GetActiveDgv();
            if (activeDgv != null)
                myFunctions.saveToExcel(activeDgv);
        }

        public void ResetAll()
        {
            // Nothing to reset for database panel
        }

        private DataGridView? GetActiveDgv()
        {
            if (!isUnlocked) return null;
            return tabControl.SelectedIndex switch
            {
                0 => dgvMiedema,
                1 => dgvFirstOrder,
                2 => dgvLnY0,
                _ => null
            };
        }

        // ===== Password Verification =====
        private void BtnUnlock_Click(object sender, EventArgs e)
        {
            VerifyPassword();
        }

        private void TxtPassword_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                VerifyPassword();
            }
        }

        private void VerifyPassword()
        {
            if (txtPassword.Text == Password)
            {
                isUnlocked = true;
                lockPanel.Visible = false;
                dataPanel.Visible = true;
                LoadAllData();
            }
            else
            {
                lblError.Text = "Incorrect password. Please try again.";
                txtPassword.SelectAll();
                txtPassword.Focus();
            }
        }

        // ===== Data Loading =====
        private void LoadAllData()
        {
            LoadMiedemaData();
            LoadFirstOrderData();
            LoadLnY0Data();
        }

        private void LoadMiedemaData(string? symbolFilter = null)
        {
            dgvMiedema.Columns.Clear();
            dgvMiedema.Rows.Clear();

            string sql = "SELECT Symbol, phi, nws, V, u, alpha_beta, hybirdvalue, isTrans, dHtrans, mass, Tm, Tb, name FROM MiedemaParameter";
            if (!string.IsNullOrWhiteSpace(symbolFilter))
                sql += " WHERE Symbol LIKE '%" + symbolFilter.Replace("'", "''") + "%'";
            sql += " ORDER BY Symbol";

            using var conn = new SQLiteConnection(DbPath);
            conn.Open();
            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            // Setup columns
            dgvMiedema.Columns.Add("Symbol", "Symbol");
            dgvMiedema.Columns.Add("phi", "\u03C6");
            dgvMiedema.Columns.Add("nws", "n_WS\u207D\u00B9\u2044\u00B3\u207E");
            dgvMiedema.Columns.Add("V", "V\u207D\u00B2\u2044\u00B3\u207E");
            dgvMiedema.Columns.Add("u", "u");
            dgvMiedema.Columns.Add("alpha_beta", "Hybrid");
            dgvMiedema.Columns.Add("hybirdvalue", "Hybrid Val");
            dgvMiedema.Columns.Add("isTrans", "isTrans");
            dgvMiedema.Columns.Add("dHtrans", "dH_Trans");
            dgvMiedema.Columns.Add("mass", "Mass");
            dgvMiedema.Columns.Add("Tm", "Tm");
            dgvMiedema.Columns.Add("Tb", "Tb");
            dgvMiedema.Columns.Add("name", "Name");

            // Symbol column read-only (primary key)
            dgvMiedema.Columns["Symbol"].ReadOnly = true;
            dgvMiedema.Columns["Symbol"].DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);

            while (reader.Read())
            {
                var row = new object[13];
                for (int i = 0; i < 13; i++)
                    row[i] = reader.IsDBNull(i) ? "" : reader.GetValue(i);
                dgvMiedema.Rows.Add(row);
            }
        }

        private void LoadFirstOrderData(string? solvFilter = null)
        {
            dgvFirstOrder.Columns.Clear();
            dgvFirstOrder.Rows.Clear();

            string sql = "SELECT solv, solui, soluj, eji, sji, Rank, T, reference FROM first_order";
            if (!string.IsNullOrWhiteSpace(solvFilter))
                sql += " WHERE solv LIKE '%" + solvFilter.Replace("'", "''") + "%'";
            sql += " ORDER BY solv, solui, soluj";

            using var conn = new SQLiteConnection(DbPath);
            conn.Open();
            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            dgvFirstOrder.Columns.Add("solv", "Solvent (k)");
            dgvFirstOrder.Columns.Add("solui", "Solute (i)");
            dgvFirstOrder.Columns.Add("soluj", "Solute (j)");
            dgvFirstOrder.Columns.Add("eji", "e\u1D62\u02B2 (wt%)");
            dgvFirstOrder.Columns.Add("sji", "\u03B5\u1D62\u02B2 (mol)");
            dgvFirstOrder.Columns.Add("Rank", "Rank");
            dgvFirstOrder.Columns.Add("T", "T");
            dgvFirstOrder.Columns.Add("reference", "Reference");

            // Key columns read-only
            dgvFirstOrder.Columns["solv"].ReadOnly = true;
            dgvFirstOrder.Columns["solv"].DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgvFirstOrder.Columns["solui"].ReadOnly = true;
            dgvFirstOrder.Columns["solui"].DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgvFirstOrder.Columns["soluj"].ReadOnly = true;
            dgvFirstOrder.Columns["soluj"].DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);

            // Wider reference column
            dgvFirstOrder.Columns["reference"].FillWeight = 200;

            while (reader.Read())
            {
                var row = new object[8];
                for (int i = 0; i < 8; i++)
                    row[i] = reader.IsDBNull(i) ? "" : reader.GetValue(i);
                dgvFirstOrder.Rows.Add(row);
            }
        }

        private void LoadLnY0Data(string? solvFilter = null)
        {
            dgvLnY0.Columns.Clear();
            dgvLnY0.Rows.Clear();

            string sql = "SELECT solv, solui, lnYi0, Yi0, T FROM lnY0";
            if (!string.IsNullOrWhiteSpace(solvFilter))
                sql += " WHERE solv LIKE '%" + solvFilter.Replace("'", "''") + "%'";
            sql += " ORDER BY solv, solui";

            using var conn = new SQLiteConnection(DbPath);
            conn.Open();
            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            dgvLnY0.Columns.Add("solv", "Solvent (k)");
            dgvLnY0.Columns.Add("solui", "Solute (i)");
            dgvLnY0.Columns.Add("lnYi0", "ln\u03B3\u1D62\u2070");
            dgvLnY0.Columns.Add("Yi0", "\u03B3\u1D62\u2070");
            dgvLnY0.Columns.Add("T", "T");

            // Key columns read-only
            dgvLnY0.Columns["solv"].ReadOnly = true;
            dgvLnY0.Columns["solv"].DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgvLnY0.Columns["solui"].ReadOnly = true;
            dgvLnY0.Columns["solui"].DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);

            while (reader.Read())
            {
                var row = new object[5];
                for (int i = 0; i < 5; i++)
                    row[i] = reader.IsDBNull(i) ? "" : reader.GetValue(i);
                dgvLnY0.Rows.Add(row);
            }
        }

        // ===== Save =====
        private void BtnSave_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to save all changes to the database?\nThis action cannot be undone.",
                "Confirm Save",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                switch (tabControl.SelectedIndex)
                {
                    case 0: SaveMiedemaData(); break;
                    case 1: SaveFirstOrderData(); break;
                    case 2: SaveLnY0Data(); break;
                }
                MessageBox.Show("Data saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveMiedemaData()
        {
            using var conn = new SQLiteConnection(DbPath);
            conn.Open();
            using var trans = conn.BeginTransaction();

            foreach (DataGridViewRow row in dgvMiedema.Rows)
            {
                if (row.IsNewRow) continue;
                string symbol = row.Cells["Symbol"].Value?.ToString() ?? "";
                if (string.IsNullOrEmpty(symbol)) continue;

                string sql = "UPDATE MiedemaParameter SET phi=@phi, nws=@nws, V=@V, u=@u, " +
                    "alpha_beta=@ab, hybirdvalue=@hv, isTrans=@it, dHtrans=@dh, mass=@mass, Tm=@tm, Tb=@tb, name=@name " +
                    "WHERE Symbol=@symbol";
                using var cmd = new SQLiteCommand(sql, conn, trans);
                cmd.Parameters.AddWithValue("@symbol", symbol);
                cmd.Parameters.AddWithValue("@phi", ParseDouble(row.Cells["phi"].Value));
                cmd.Parameters.AddWithValue("@nws", ParseDouble(row.Cells["nws"].Value));
                cmd.Parameters.AddWithValue("@V", ParseDouble(row.Cells["V"].Value));
                cmd.Parameters.AddWithValue("@u", ParseDouble(row.Cells["u"].Value));
                cmd.Parameters.AddWithValue("@ab", row.Cells["alpha_beta"].Value?.ToString() ?? "");
                cmd.Parameters.AddWithValue("@hv", ParseDouble(row.Cells["hybirdvalue"].Value));
                cmd.Parameters.AddWithValue("@it", ParseBool(row.Cells["isTrans"].Value));
                cmd.Parameters.AddWithValue("@dh", ParseDouble(row.Cells["dHtrans"].Value));
                cmd.Parameters.AddWithValue("@mass", ParseDouble(row.Cells["mass"].Value));
                cmd.Parameters.AddWithValue("@tm", ParseDouble(row.Cells["Tm"].Value));
                cmd.Parameters.AddWithValue("@tb", ParseDouble(row.Cells["Tb"].Value));
                cmd.Parameters.AddWithValue("@name", row.Cells["name"].Value?.ToString() ?? "");
                cmd.ExecuteNonQuery();
            }
            trans.Commit();
        }

        private void SaveFirstOrderData()
        {
            using var conn = new SQLiteConnection(DbPath);
            conn.Open();
            using var trans = conn.BeginTransaction();

            foreach (DataGridViewRow row in dgvFirstOrder.Rows)
            {
                if (row.IsNewRow) continue;
                string solv = row.Cells["solv"].Value?.ToString() ?? "";
                string solui = row.Cells["solui"].Value?.ToString() ?? "";
                string soluj = row.Cells["soluj"].Value?.ToString() ?? "";
                if (string.IsNullOrEmpty(solv)) continue;

                string sql = "UPDATE first_order SET eji=@eji, sji=@sji, Rank=@rank, T=@t, reference=@ref " +
                    "WHERE solv=@solv AND solui=@solui AND soluj=@soluj";
                using var cmd = new SQLiteCommand(sql, conn, trans);
                cmd.Parameters.AddWithValue("@solv", solv);
                cmd.Parameters.AddWithValue("@solui", solui);
                cmd.Parameters.AddWithValue("@soluj", soluj);
                cmd.Parameters.AddWithValue("@eji", row.Cells["eji"].Value?.ToString() ?? "");
                cmd.Parameters.AddWithValue("@sji", row.Cells["sji"].Value?.ToString() ?? "");
                cmd.Parameters.AddWithValue("@rank", row.Cells["Rank"].Value?.ToString() ?? "");
                cmd.Parameters.AddWithValue("@t", row.Cells["T"].Value?.ToString() ?? "");
                cmd.Parameters.AddWithValue("@ref", row.Cells["reference"].Value?.ToString() ?? "");
                cmd.ExecuteNonQuery();
            }
            trans.Commit();
        }

        private void SaveLnY0Data()
        {
            using var conn = new SQLiteConnection(DbPath);
            conn.Open();
            using var trans = conn.BeginTransaction();

            foreach (DataGridViewRow row in dgvLnY0.Rows)
            {
                if (row.IsNewRow) continue;
                string solv = row.Cells["solv"].Value?.ToString() ?? "";
                string solui = row.Cells["solui"].Value?.ToString() ?? "";
                if (string.IsNullOrEmpty(solv)) continue;

                string sql = "UPDATE lnY0 SET lnYi0=@lnyi0, Yi0=@yi0, T=@t WHERE solv=@solv AND solui=@solui";
                using var cmd = new SQLiteCommand(sql, conn, trans);
                cmd.Parameters.AddWithValue("@solv", solv);
                cmd.Parameters.AddWithValue("@solui", solui);
                cmd.Parameters.AddWithValue("@lnyi0", row.Cells["lnYi0"].Value?.ToString() ?? "");
                cmd.Parameters.AddWithValue("@yi0", row.Cells["Yi0"].Value?.ToString() ?? "");
                cmd.Parameters.AddWithValue("@t", row.Cells["T"].Value?.ToString() ?? "");
                cmd.ExecuteNonQuery();
            }
            trans.Commit();
        }

        // ===== Filter =====
        private void BtnFilterMiedema_Click(object sender, EventArgs e) => LoadMiedemaData(txtFilterMiedema.Text.Trim());
        private void BtnShowAllMiedema_Click(object sender, EventArgs e) { txtFilterMiedema.Text = ""; LoadMiedemaData(); }
        private void BtnFilterFirstOrder_Click(object sender, EventArgs e) => LoadFirstOrderData(txtFilterFirstOrder.Text.Trim());
        private void BtnShowAllFirstOrder_Click(object sender, EventArgs e) { txtFilterFirstOrder.Text = ""; LoadFirstOrderData(); }
        private void BtnFilterLnY0_Click(object sender, EventArgs e) => LoadLnY0Data(txtFilterLnY0.Text.Trim());
        private void BtnShowAllLnY0_Click(object sender, EventArgs e) { txtFilterLnY0.Text = ""; LoadLnY0Data(); }

        // ===== Refresh =====
        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadAllData();
            MessageBox.Show("Data refreshed from database.", "Refresh", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ===== Helpers =====
        private static double ParseDouble(object? val)
        {
            if (val == null) return 0;
            if (double.TryParse(val.ToString(), out double d)) return d;
            return 0;
        }

        private static bool ParseBool(object? val)
        {
            if (val == null) return false;
            string s = val.ToString()?.ToLower() ?? "";
            return s == "true" || s == "1" || s == "yes";
        }
    }
}
