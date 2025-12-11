namespace AlloyAct_Pro
{
    public partial class Form1 : Form
    {
        // 子窗体引用（延迟初始化）
        private ActivityFm _activityFm;
        private ActivityCoefficientFm _coefficientFm;
        private ActivityInteractionCoefficientFm _interactionCoefficientFm;
        private ActivityCoefficientAtInfiniteDilution _infiniteDilutionFm;

        public Form1()
        {
            InitializeComponent();
        }

        private void Activity_Click(object sender, EventArgs e)
        {
            // 活度计算窗体
            UIHelper.ShowOrActivateForm(ref _activityFm);
            this.WindowState = FormWindowState.Minimized;
        }

        private void ActivityCoeff_Click(object sender, EventArgs e)
        {
            // 活度系数计算窗体
            UIHelper.ShowOrActivateForm(ref _coefficientFm);
            this.WindowState = FormWindowState.Minimized;
        }

        private void InteractionCoeff_Click(object sender, EventArgs e)
        {
            // 活度相互作用系数计算窗体
            UIHelper.ShowOrActivateForm(ref _interactionCoefficientFm);
            this.WindowState = FormWindowState.Minimized;
        }

        private void ActivityCoefficientAtInfinitely_Click(object sender, EventArgs e)
        {
            // 无限稀活度系数计算窗体
            UIHelper.ShowOrActivateForm(ref _infiniteDilutionFm);
            this.WindowState = FormWindowState.Minimized;
        }
    }
}