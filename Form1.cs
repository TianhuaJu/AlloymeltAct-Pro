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
                helpImage = Properties.Resources.\u6d3b\u5ea6;
                helpTitle = "Activity Calculation - Help";
            }
            else if (activePanel is ActivityCoefficientPanel)
            {
                helpImage = Properties.Resources.\u6d3b\u5ea6\u7cfb\u6570;
                helpTitle = "Activity Coefficient - Help";
            }
            else if (activePanel is InteractionCoefficientPanel)
            {
                helpImage = Properties.Resources.\u6d3b\u5ea6\u76f8\u4e92\u4f5c\u7528\u7cfb\u6570;
                helpTitle = "Interaction Coefficient - Help";
            }
            else if (activePanel is InfiniteDilutionPanel)
            {
                helpImage = Properties.Resources.\u65e0\u9650\u7a00\u6d3b\u5ea6\u7cfb\u6570;
                helpTitle = "Infinite Dilution - Help";
            }
            else if (activePanel is SecondOrderPanel)
            {
                helpImage = Properties.Resources.\u6d3b\u5ea6\u76f8\u4e92\u4f5c\u7528\u7cfb\u6570;
                helpTitle = "Second-Order Interaction - Help";
            }

            if (helpImage != null)
            {
                ShowHelpDialog(helpTitle, helpImage);
            }
            else if (activePanel is LiquidusPanel)
            {
                MessageBox.Show(
                    "\u6db2\u76f8\u7ebf\u6e29\u5ea6\u9884\u6d4b (Liquidus Temperature)\n\n" +
                    "\u57fa\u4e8e Schr\u00f6der-van Laar \u65b9\u7a0b\u7ed3\u5408\u6d3b\u5ea6\u4ea4\u4e92\u4f5c\u7528\u6a21\u578b\uff0c\u9884\u6d4b\u5408\u91d1\u7194\u4f53\u7684\u6db2\u76f8\u7ebf\u6e29\u5ea6\u3002\n\n" +
                    "\u64cd\u4f5c\u6b65\u9aa4\uff1a\n" +
                    "\u2022  \u9009\u62e9\u57fa\u4f53\u5143\u7d20\uff08\u6eb6\u5242\uff09\n" +
                    "\u2022  \u8f93\u5165\u5408\u91d1\u6210\u5206\uff08\u6469\u5c14\u5206\u6570\uff09\uff0c\u683c\u5f0f\u5982 Mn0.02Si0.01C0.005\n" +
                    "\u2022  \u53c2\u8003\u6e29\u5ea6\u7528\u4e8e\u521d\u59cb\u6d3b\u5ea6\u4f30\u7b97\n\n" +
                    "\u8f93\u51fa\u7ed3\u679c\uff1a\n" +
                    "\u2022  T_liquidus \u2014 \u5206\u522b\u57fa\u4e8e Wagner/Darken/Elliot \u6a21\u578b\u7684\u6db2\u76f8\u7ebf\u6e29\u5ea6\n" +
                    "\u2022  \u0394T \u2014 \u51dd\u56fa\u70b9\u964d\u4f4e\u503c\n" +
                    "\u2022  a_solvent \u2014 \u6eb6\u5242\u6d3b\u5ea6\n\n" +
                    "\u7406\u8bba\u57fa\u7840\uff1a ln(a_solvent) = (\u0394Hf/R) \u00d7 (1/Tm \u2212 1/T)",
                    "\u6db2\u76f8\u7ebf\u6e29\u5ea6 - \u5e2e\u52a9",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (activePanel is DatabasePanel)
            {
                MessageBox.Show(
                    "\u6570\u636e\u5e93\u7ba1\u7406 (Database Management)\n\n" +
                    "\u67e5\u770b\u548c\u7f16\u8f91 Miedema \u6a21\u578b\u53c2\u6570\u53ca\u5b9e\u9a8c\u503c\u3002\n\n" +
                    "\u4e09\u4e2a\u5b50\u9875\u9762\uff1a\n" +
                    "\u2022  Miedema\u53c2\u6570 \u2014 \u5143\u7d20\u5c5e\u6027 (\u03c6, nws, V, u, \u03b1_\u03b2, \u6742\u5316\u503c, Tm, Tb \u7b49)\n" +
                    "\u2022  \u4ea4\u4e92\u4f5c\u7528\u7cfb\u6570 \u2014 \u4e00\u9636\u5b9e\u9a8c\u503c (\u03b5\u1d62\u02b2 / e\u1d62\u02b2)\n" +
                    "\u2022  \u65e0\u9650\u7a00\u6d3b\u5ea6\u7cfb\u6570 \u2014 \u65e0\u9650\u7a00\u91ca\u6d3b\u5ea6\u7cfb\u6570 (ln\u03b3\u1d62\u2070)\n\n" +
                    "\u64cd\u4f5c\u8bf4\u660e\uff1a\n" +
                    "\u2022  \u8f93\u5165\u5bc6\u7801\u540e\u8fdb\u5165\u7f16\u8f91\u6a21\u5f0f\n" +
                    "\u2022  \u4f7f\u7528\u7b5b\u9009\u6846\u6309\u5143\u7d20\u7b26\u53f7\u641c\u7d22\n" +
                    "\u2022  \u76f4\u63a5\u5728\u8868\u683c\u4e2d\u4fee\u6539\u6570\u503c\n" +
                    "\u2022  \u70b9\u51fb\u300c\u4fdd\u5b58\u4fee\u6539\u300d\u5199\u56de\u6570\u636e\u5e93",
                    "\u6570\u636e\u5e93\u7ba1\u7406 - \u5e2e\u52a9",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (activePanel is ChatPanel)
            {
                MessageBox.Show(
                    "AI \u52a9\u624b (AI Assistant)\n\n" +
                    "\u7528\u81ea\u7136\u8bed\u8a00\u4e0eAI\u5bf9\u8bdd\uff0c\u6267\u884c\u70ed\u529b\u5b66\u8ba1\u7b97\u548c\u5408\u91d1\u5206\u6790\u3002\n\n" +
                    "\u8fde\u63a5\u8bbe\u7f6e\uff1a\n" +
                    "\u2022  \u9009\u62e9LLM\u63d0\u4f9b\u5546\uff08OpenAI/Claude/Gemini/DeepSeek/Kimi/Ollama\u7b49\uff09\n" +
                    "\u2022  \u9009\u62e9\u6a21\u578b\uff0c\u70b9\u51fb\u5237\u65b0\u6309\u94ae\u83b7\u53d6\u53ef\u7528\u6a21\u578b\u5217\u8868\n" +
                    "\u2022  \u586b\u5165API Key\uff08\u672c\u5730Ollama\u65e0\u9700\u586b\u5199\uff09\n" +
                    "\u2022  \u70b9\u51fb\u300c\u8fde\u63a5\u300d\u521d\u59cb\u5316AI\u52a9\u624b\n\n" +
                    "\u4f7f\u7528\u65b9\u6cd5\uff1a\n" +
                    "\u2022  \u5728\u8f93\u5165\u6846\u8f93\u5165\u95ee\u9898\uff0cCtrl+Enter \u6216\u70b9\u51fb\u300c\u53d1\u9001\u300d\n" +
                    "\u2022  AI\u5c06\u81ea\u52a8\u8c03\u7528\u8ba1\u7b97\u5de5\u5177\u5e76\u8fd4\u56de\u7ed3\u679c\n" +
                    "\u2022  \u652f\u6301\u591a\u8f6e\u5bf9\u8bdd\uff0c\u70b9\u51fb\u300c\u6e05\u7a7a\u300d\u91cd\u7f6e\u5bf9\u8bdd\n\n" +
                    "\u652f\u6301\u7684\u8ba1\u7b97\uff1a\n" +
                    "\u2022  \u6d3b\u5ea6\u3001\u6d3b\u5ea6\u7cfb\u6570\u3001\u4ea4\u4e92\u4f5c\u7528\u7cfb\u6570\uff081\u9636+2\u9636\uff09\n" +
                    "\u2022  \u65e0\u9650\u7a00\u6d3b\u5ea6\u7cfb\u6570\u3001\u6db2\u76f8\u7ebf\u6e29\u5ea6\u3001\u5355\u4f4d\u6362\u7b97\n" +
                    "\u2022  \u56fe\u8868\u7ed8\u5236\uff08\u6d3b\u5ea6\u968f\u6210\u5206/\u6e29\u5ea6\u53d8\u5316\u66f2\u7ebf\uff09",
                    "AI\u52a9\u624b - \u5e2e\u52a9",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (activePanel is KnowledgePanel)
            {
                MessageBox.Show(
                    "\u77e5\u8bc6\u5e93 (Knowledge Base)\n\n" +
                    "\u7ba1\u7406AI\u52a9\u624b\u7684\u77e5\u8bc6\u548c\u8bb0\u5fc6\uff0c\u77e5\u8bc6\u6761\u76ee\u4f1a\u81ea\u52a8\u6ce8\u5165AI\u7684\u7cfb\u7edf\u63d0\u793a\u8bcd\u3002\n\n" +
                    "\u77e5\u8bc6\u5206\u7c7b\uff1a\n" +
                    "\u2022  knowledge \u2014 \u51b6\u91d1/\u6750\u6599\u79d1\u5b66\u77e5\u8bc6\uff08\u70ed\u529b\u5b66\u6570\u636e\u3001\u516c\u5f0f\u3001\u76f8\u56fe\u7b49\uff09\n" +
                    "\u2022  preference \u2014 \u9ed8\u8ba4\u8ba1\u7b97\u8bbe\u7f6e\uff08\u6e29\u5ea6\u3001\u6a21\u578b\u504f\u597d\uff09\n" +
                    "\u2022  alloy_system \u2014 \u5e38\u7528\u5408\u91d1\u4f53\u7cfb\n" +
                    "\u2022  calculation \u2014 \u8ba1\u7b97\u89c4\u5219\u548c\u7ecf\u9a8c\n" +
                    "\u2022  general \u2014 \u5176\u4ed6\u77e5\u8bc6\n\n" +
                    "\u6587\u732e\u5bfc\u5165\u4e0eAI\u5b66\u4e60\uff1a\n" +
                    "\u2022  \u300c\u5bfc\u5165\u6587\u732e\u300d\u652f\u6301 PDF\u3001Word(.docx)\u3001TXT\u3001MD\u3001CSV\n" +
                    "\u2022  \u626b\u63cf\u7248PDF\u81ea\u52a8\u63d0\u53d6\u56fe\u7247\uff0c\u7528AI\u89c6\u89c9\u8bc6\u522b\n" +
                    "\u2022  \u300c AI\u5b66\u4e60\u300d\u5f39\u51fa\u5b66\u4e60\u91cd\u70b9\u8bbe\u7f6e\uff0c\u53ef\u6307\u5b9a\u63d0\u53d6\u65b9\u5411\n" +
                    "\u2022  \u5173\u952e\u8bcd\u81ea\u52a8\u5b9a\u4f4d\u76f8\u5173\u6bb5\u843d\uff0c\u63d0\u9ad8\u5b66\u4e60\u6548\u7387\n" +
                    "\u2022  \u540c\u4e00\u6587\u6863\u53ef\u591a\u6b21\u5b66\u4e60\u4e0d\u540c\u77e5\u8bc6\u70b9\n\n" +
                    "\u5b58\u50a8\u4f4d\u7f6e\uff1a ~/.alloyact/memories.json",
                    "\u77e5\u8bc6\u5e93 - \u5e2e\u52a9",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (activePanel is DftPanel)
            {
                MessageBox.Show(
                    "DFT\u6570\u636e\u5bfc\u5165 (DFT Data Import)\n\n" +
                    "\u5bfc\u5165\u5bc6\u5ea6\u6cdb\u51fd\u7406\u8bba (DFT) \u8ba1\u7b97\u7ed3\u679c\uff0c\u7528\u4e8e\u4e0e Miedema \u6a21\u578b\u53c2\u6570\u5bf9\u6bd4\u3002\n\n" +
                    "\u64cd\u4f5c\u65b9\u5f0f\uff1a\n" +
                    "\u2022  \u300c\u5bfc\u5165\u6587\u4ef6\u300d\u2014 \u5bfc\u5165\u5355\u4e2aDFT\u8f93\u51fa\u6587\u4ef6\n" +
                    "\u2022  \u300c\u5bfc\u5165\u6587\u4ef6\u5939\u300d\u2014 \u6279\u91cf\u5bfc\u5165\u6574\u4e2a\u76ee\u5f55\u7684DFT\u6587\u4ef6\n" +
                    "\u2022  \u300c\u6e05\u7a7a\u300d\u2014 \u5220\u9664\u6240\u6709\u5df2\u5bfc\u5165\u6570\u636e\n\n" +
                    "\u652f\u6301\u591a\u79cdDFT\u8f6f\u4ef6\u7684\u8f93\u51fa\u683c\u5f0f\u3002\n" +
                    "\u5bfc\u5165\u540e\u53ef\u5728\u8868\u683c\u4e2d\u67e5\u770b\u7ed3\u679c\u8be6\u60c5\uff0c\u5e76\u652f\u6301\u5bfc\u51fa\u5230Excel\u3002",
                    "DFT\u6570\u636e\u5bfc\u5165 - \u5e2e\u52a9",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (activePanel is UnitConvertPanel)
            {
                MessageBox.Show(
                    "\u5355\u4f4d\u6362\u7b97 (Unit Conversion)\n\n" +
                    "\u5728\u8d28\u91cf\u767e\u5206\u6bd4 (wt%) \u548c\u539f\u5b50\u5206\u6570 (\u6469\u5c14\u5206\u6570) \u4e4b\u95f4\u4e92\u76f8\u8f6c\u6362\u3002\n\n" +
                    "\u64cd\u4f5c\u6b65\u9aa4\uff1a\n" +
                    "\u2022  \u9009\u62e9\u57fa\u4f53\u5143\u7d20\uff08\u6eb6\u5242\uff09\u548c\u6eb6\u8d28\u5143\u7d20\n" +
                    "\u2022  \u9009\u62e9\u8f6c\u6362\u65b9\u5411\uff1a wt% \u2192 \u6469\u5c14\u5206\u6570 \u6216 \u6469\u5c14\u5206\u6570 \u2192 wt%\n" +
                    "\u2022  \u8f93\u5165\u6570\u503c\u5e76\u70b9\u51fb\u300c\u8f6c\u6362\u300d\n\n" +
                    "\u516c\u5f0f\uff1a\n" +
                    "\u2022  x_i = (w_i/M_i) / \u03a3(w_j/M_j)   [wt% \u2192 \u6469\u5c14\u5206\u6570]\n" +
                    "\u2022  w_i = (x_i\u00d7M_i) / \u03a3(x_j\u00d7M_j) \u00d7 100%   [\u6469\u5c14\u5206\u6570 \u2192 wt%]",
                    "\u5355\u4f4d\u6362\u7b97 - \u5e2e\u52a9",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    "AlloyAct Pro \u5e2e\u52a9\n\n" +
                    "\u8bf7\u5207\u6362\u5230\u5177\u4f53\u529f\u80fd\u9875\u9762\u540e\u70b9\u51fb\u5e2e\u52a9\u6309\u94ae\uff0c\u67e5\u770b\u8be5\u529f\u80fd\u7684\u8be6\u7ec6\u8bf4\u660e\u3002",
                    "AlloyAct Pro - \u5e2e\u52a9",
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
