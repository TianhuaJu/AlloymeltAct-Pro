using AlloyAct_Pro.Controls;

namespace AlloyAct_Pro
{
    public partial class Form1 : Form
    {
        // Panels
        private readonly ActivityPanel activityPanel = new ActivityPanel();
        private readonly ActivityCoefficientPanel coefficientPanel = new ActivityCoefficientPanel();
        private readonly InteractionCoefficientPanel interactionPanel = new InteractionCoefficientPanel();
        private readonly InfiniteDilutionPanel infiniteDilutionPanel = new InfiniteDilutionPanel();
        private readonly SecondOrderPanel secondOrderPanel = new SecondOrderPanel();
        private readonly UnitConvertPanel unitConvertPanel = new UnitConvertPanel();
        private readonly LiquidusPanel liquidusPanel = new LiquidusPanel();
        private readonly DatabasePanel databasePanel = new DatabasePanel();
        private readonly ChatPanel chatPanel = new ChatPanel();
        private readonly KnowledgePanel knowledgePanel = new KnowledgePanel();
        private readonly DftPanel dftPanel = new DftPanel();

        private UserControl activePanel;
        private Button activeNavButton;

        public Form1()
        {
            InitializeComponent();
            SetupPanels();
            // Navigate to Activity by default
            NavigateTo(activityPanel, btnActivity);
        }

        private void SetupPanels()
        {
            UserControl[] panels = { activityPanel, coefficientPanel, interactionPanel,
                                     infiniteDilutionPanel, secondOrderPanel,
                                     liquidusPanel, unitConvertPanel, databasePanel,
                                     dftPanel, chatPanel, knowledgePanel };
            foreach (var p in panels)
            {
                p.Dock = DockStyle.Fill;
                p.Visible = false;
                contentPanel.Controls.Add(p);
            }
        }

        private void NavigateTo(UserControl panel, Button navButton)
        {
            // Hide current
            if (activePanel != null)
                activePanel.Visible = false;

            // Deactivate previous nav button
            if (activeNavButton != null)
            {
                activeNavButton.BackColor = AppTheme.SidebarBg;
                activeNavButton.ForeColor = AppTheme.SidebarText;
            }

            // Show new panel
            panel.Visible = true;
            activePanel = panel;

            // Activate nav button
            navButton.BackColor = AppTheme.SidebarActiveBg;
            navButton.ForeColor = AppTheme.SidebarActiveText;
            activeNavButton = navButton;

            // Update page title
            string title = panel switch
            {
                ActivityPanel p => p.PageTitle,
                ActivityCoefficientPanel p => p.PageTitle,
                InteractionCoefficientPanel p => p.PageTitle,
                InfiniteDilutionPanel p => p.PageTitle,
                SecondOrderPanel p => p.PageTitle,
                LiquidusPanel p => p.PageTitle,
                UnitConvertPanel p => p.PageTitle,
                DatabasePanel p => p.PageTitle,
                ChatPanel p => p.PageTitle,
                KnowledgePanel p => p.PageTitle,
                DftPanel p => p.PageTitle,
                _ => "AlloyAct Pro"
            };
            lblPageTitle.Text = title;
        }

        private void BtnActivity_Click(object sender, EventArgs e)
        {
            NavigateTo(activityPanel, btnActivity);
        }

        private void BtnCoefficient_Click(object sender, EventArgs e)
        {
            NavigateTo(coefficientPanel, btnCoefficient);
        }

        private void BtnInteraction_Click(object sender, EventArgs e)
        {
            NavigateTo(interactionPanel, btnInteraction);
        }

        private void BtnInfiniteDilution_Click(object sender, EventArgs e)
        {
            NavigateTo(infiniteDilutionPanel, btnInfiniteDilution);
        }

        private void BtnSecondOrder_Click(object sender, EventArgs e)
        {
            NavigateTo(secondOrderPanel, btnSecondOrder);
        }

        private void BtnUnitConvert_Click(object sender, EventArgs e)
        {
            NavigateTo(unitConvertPanel, btnUnitConvert);
        }

        private void BtnLiquidus_Click(object sender, EventArgs e)
        {
            NavigateTo(liquidusPanel, btnLiquidus);
        }

        private void BtnDatabase_Click(object sender, EventArgs e)
        {
            NavigateTo(databasePanel, btnDatabase);
        }

        private void BtnChat_Click(object sender, EventArgs e)
        {
            NavigateTo(chatPanel, btnChat);
        }

        private void BtnKnowledge_Click(object sender, EventArgs e)
        {
            NavigateTo(knowledgePanel, btnKnowledge);
        }

        private void BtnDft_Click(object sender, EventArgs e)
        {
            NavigateTo(dftPanel, btnDft);
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (activePanel is ActivityPanel ap) ap.ExportToExcel();
            else if (activePanel is ActivityCoefficientPanel acp) acp.ExportToExcel();
            else if (activePanel is InteractionCoefficientPanel icp) icp.ExportToExcel();
            else if (activePanel is InfiniteDilutionPanel idp) idp.ExportToExcel();
            else if (activePanel is SecondOrderPanel sop) sop.ExportToExcel();
            else if (activePanel is LiquidusPanel lp) lp.ExportToExcel();
            else if (activePanel is UnitConvertPanel ucp) ucp.ExportToExcel();
            else if (activePanel is DatabasePanel dbp) dbp.ExportToExcel();
            else if (activePanel is DftPanel dp) dp.ExportToExcel();
            else if (activePanel is ChatPanel cp) cp.ExportToExcel();
            else if (activePanel is KnowledgePanel kp) kp.ExportToExcel();
        }

        private void BtnHelp_Click(object sender, EventArgs e)
        {
            // Show context-sensitive help based on active panel
            Image? helpImage = null;
            string helpTitle = "Help";

            if (activePanel is ActivityPanel)
            {
                helpImage = Properties.Resources.活度;
                helpTitle = "Activity Calculation - Help";
            }
            else if (activePanel is ActivityCoefficientPanel)
            {
                helpImage = Properties.Resources.活度系数;
                helpTitle = "Activity Coefficient - Help";
            }
            else if (activePanel is InteractionCoefficientPanel)
            {
                helpImage = Properties.Resources.活度相互作用系数;
                helpTitle = "Interaction Coefficient - Help";
            }
            else if (activePanel is InfiniteDilutionPanel)
            {
                helpImage = Properties.Resources.无限稀活度系数;
                helpTitle = "Infinite Dilution - Help";
            }
            else if (activePanel is SecondOrderPanel)
            {
                // No dedicated help image for second-order; show interaction help
                helpImage = Properties.Resources.活度相互作用系数;
                helpTitle = "Second-Order Interaction - Help";
            }

            if (helpImage != null)
            {
                ShowHelpDialog(helpTitle, helpImage);
            }
            else if (activePanel is LiquidusPanel)
            {
                MessageBox.Show(
                    "Liquidus Temperature Prediction:\n\n" +
                    "Predicts the liquidus temperature of an alloy melt based on the\n" +
                    "Schr\u00f6der-van Laar equation combined with activity interaction models.\n\n" +
                    "\u2022  Select the matrix (solvent) element\n" +
                    "\u2022  Enter alloy composition in mole fraction (e.g., Mn0.02Si0.01)\n" +
                    "\u2022  Reference temperature is used for initial activity estimation\n\n" +
                    "Output: T_liquidus from Wagner, Darken and Elliot models,\n" +
                    "freezing point depression (\u0394T), and solvent activity.\n\n" +
                    "Theory: ln(a_solvent) = (\u0394Hf/R) \u00d7 (1/Tm \u2212 1/T)",
                    "Liquidus Temperature - Help",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (activePanel is DatabasePanel)
            {
                MessageBox.Show(
                    "Database Management:\n\n" +
                    "View and edit the underlying Miedema model parameters and experimental values.\n\n" +
                    "\u2022  Miedema Parameters \u2014 Element properties (\u03C6, nws, V, u, etc.)\n" +
                    "\u2022  Interaction Coeff. \u2014 1st-order experimental values (\u03B5\u1D62\u02B2 / e\u1D62\u02B2)\n" +
                    "\u2022  Infinite Dilution \u2014 Activity coefficients at infinite dilution (ln\u03B3\u1D62\u2070)\n\n" +
                    "Use Filter to search by element symbol.\n" +
                    "Click Save Changes to write edits back to the database.",
                    "Database Management - Help",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (activePanel is ChatPanel)
            {
                MessageBox.Show(
                    "AI Assistant:\n\n" +
                    "Use natural language to perform thermodynamic calculations.\n\n" +
                    "\u2022  Select LLM provider and model\n" +
                    "\u2022  Enter API key (not needed for local Ollama)\n" +
                    "\u2022  Click Connect to initialize\n" +
                    "\u2022  Type your question and press Ctrl+Enter or click Send\n\n" +
                    "Supported: Activity, activity coefficient, interaction coefficient,\n" +
                    "infinite dilution, liquidus temperature, unit conversion, and chart plotting.",
                    "AI Assistant - Help",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (activePanel is KnowledgePanel)
            {
                MessageBox.Show(
                    "Knowledge Base:\n\n" +
                    "Manage the AI assistant's knowledge and preferences.\n" +
                    "Knowledge entries are automatically injected into the AI's system prompt.\n\n" +
                    "\u2022  preference \u2014 Default calculation settings (e.g., temperature, model)\n" +
                    "\u2022  alloy_system \u2014 Frequently used alloy systems\n" +
                    "\u2022  calculation \u2014 Calculation rules and experience\n" +
                    "\u2022  general \u2014 Other knowledge\n\n" +
                    "Stored at: ~/.alloyact/memories.json",
                    "Knowledge Base - Help",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    "Unit Conversion:\n\n" +
                    "Convert between weight percentage (wt%) and atom fraction (mole fraction).\n\n" +
                    "\u2022  Select matrix and solute elements\n" +
                    "\u2022  Choose conversion direction\n" +
                    "\u2022  Enter value and click Convert",
                    "Unit Conversion - Help",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void ShowHelpDialog(string title, Image image)
        {
            var helpForm = new Form();
            helpForm.Text = title;
            helpForm.StartPosition = FormStartPosition.CenterParent;
            helpForm.Size = new Size(image.Width + 40, image.Height + 60);
            helpForm.MinimizeBox = false;
            helpForm.MaximizeBox = false;
            helpForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            helpForm.BackColor = Color.White;

            var pictureBox = new PictureBox();
            pictureBox.Image = image;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.Padding = new Padding(10);

            helpForm.Controls.Add(pictureBox);
            helpForm.ShowDialog(this);
        }

        private void BtnAbout_Click(object sender, EventArgs e)
        {
            var aboutForm = new Form();
            aboutForm.Text = "About AlloyAct Pro";
            aboutForm.Size = new Size(520, 600);
            aboutForm.StartPosition = FormStartPosition.CenterParent;
            aboutForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            aboutForm.MinimizeBox = false;
            aboutForm.MaximizeBox = false;
            aboutForm.BackColor = Color.White;
            aboutForm.Icon = this.Icon;

            var mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.AutoScroll = true;
            mainPanel.Padding = new Padding(28, 20, 28, 10);

            // App name
            var lblAppName = new Label();
            lblAppName.Text = "AlloyAct Pro";
            lblAppName.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
            lblAppName.ForeColor = Color.FromArgb(44, 62, 80);
            lblAppName.AutoSize = true;
            lblAppName.Location = new Point(28, 20);

            // Subtitle
            var lblSubtitle = new Label();
            lblSubtitle.Text = "Alloy Melt Activity Calculator   v2.0";
            lblSubtitle.Font = new Font("Microsoft YaHei UI", 11F);
            lblSubtitle.ForeColor = Color.FromArgb(100, 100, 100);
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new Point(30, 56);

            // Model name - prominent
            var lblModel = new Label();
            lblModel.Text = "Based on UEM-Miedema Framework Model";
            lblModel.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            lblModel.ForeColor = Color.FromArgb(41, 128, 185);
            lblModel.AutoSize = true;
            lblModel.Location = new Point(28, 96);

            // Separator
            var sep1 = new Label();
            sep1.BorderStyle = BorderStyle.Fixed3D;
            sep1.Location = new Point(28, 130);
            sep1.Size = new Size(440, 2);

            // Parameters section
            var lblParams = new Label();
            lblParams.Text = "Thermodynamic Parameters:";
            lblParams.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            lblParams.ForeColor = Color.FromArgb(44, 62, 80);
            lblParams.AutoSize = true;
            lblParams.Location = new Point(28, 144);

            var lblParamList = new Label();
            lblParamList.Text =
                "\u2022  Infinite Dilution Activity Coefficient  (ln\u03B3\u1D62\u2070)\n" +
                "\u2022  1st-order Interaction Coefficient  (\u03B5\u1D62\u02B2)\n" +
                "\u2022  2nd-order Interaction Coefficient  (\u03C1\u1D62\u02B2\u1D4F)\n" +
                "\u2022  Activity Coefficient  (ln\u03B3\u1D62)\n" +
                "\u2022  Activity  (a\u1D62)";
            lblParamList.Font = new Font("Microsoft YaHei UI", 10.5F);
            lblParamList.ForeColor = Color.FromArgb(60, 60, 60);
            lblParamList.AutoSize = true;
            lblParamList.Location = new Point(36, 174);

            // Advanced section
            var lblAdvanced = new Label();
            lblAdvanced.Text = "Advanced:";
            lblAdvanced.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            lblAdvanced.ForeColor = Color.FromArgb(44, 62, 80);
            lblAdvanced.AutoSize = true;
            lblAdvanced.Location = new Point(28, 298);

            var lblAdvancedList = new Label();
            lblAdvancedList.Text = "\u2022  Liquidus Temperature  (T\u2097\u1D62\u2091)";
            lblAdvancedList.Font = new Font("Microsoft YaHei UI", 10.5F);
            lblAdvancedList.ForeColor = Color.FromArgb(60, 60, 60);
            lblAdvancedList.AutoSize = true;
            lblAdvancedList.Location = new Point(36, 328);

            // Tools section
            var lblTools = new Label();
            lblTools.Text = "Tools:";
            lblTools.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            lblTools.ForeColor = Color.FromArgb(44, 62, 80);
            lblTools.AutoSize = true;
            lblTools.Location = new Point(28, 358);

            var lblToolList = new Label();
            lblToolList.Text =
                "\u2022  Database Management\n" +
                "\u2022  Unit Conversion  (wt% \u2194 atom fraction)";
            lblToolList.Font = new Font("Microsoft YaHei UI", 10.5F);
            lblToolList.ForeColor = Color.FromArgb(60, 60, 60);
            lblToolList.AutoSize = true;
            lblToolList.Location = new Point(36, 388);

            // Separator 2
            var sep2 = new Label();
            sep2.BorderStyle = BorderStyle.Fixed3D;
            sep2.Location = new Point(28, 432);
            sep2.Size = new Size(440, 2);

            // References
            var lblRef = new Label();
            lblRef.Text = "References:";
            lblRef.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            lblRef.ForeColor = Color.FromArgb(44, 62, 80);
            lblRef.AutoSize = true;
            lblRef.Location = new Point(28, 444);

            var lblRefList = new Label();
            lblRefList.Text =
                "[1] Ju T H, Ding X Y, Zhang L, et al. ISIJ International, 2020, 60(11): 2416-2424.\n" +
                "[2] \u5C45\u5929\u534E, \u7B49. \u91D1\u5C5E\u5B66\u62A5, 2023, 59(11): 1533-1540.\n" +
                "[3] Kang Y-B. Metall. Mater. Trans. B, 2020, 51(2): 795-804.";
            lblRefList.Font = new Font("Microsoft YaHei UI", 9F);
            lblRefList.ForeColor = Color.FromArgb(100, 100, 100);
            lblRefList.AutoSize = true;
            lblRefList.MaximumSize = new Size(440, 0);
            lblRefList.Location = new Point(30, 470);

            mainPanel.Controls.Add(lblAppName);
            mainPanel.Controls.Add(lblSubtitle);
            mainPanel.Controls.Add(lblModel);
            mainPanel.Controls.Add(sep1);
            mainPanel.Controls.Add(lblParams);
            mainPanel.Controls.Add(lblParamList);
            mainPanel.Controls.Add(lblAdvanced);
            mainPanel.Controls.Add(lblAdvancedList);
            mainPanel.Controls.Add(lblTools);
            mainPanel.Controls.Add(lblToolList);
            mainPanel.Controls.Add(sep2);
            mainPanel.Controls.Add(lblRef);
            mainPanel.Controls.Add(lblRefList);

            aboutForm.Controls.Add(mainPanel);
            aboutForm.ShowDialog(this);
        }
    }
}
