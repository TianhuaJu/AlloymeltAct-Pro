namespace AlloyAct_Pro
{
    public partial class Form1 : Form
    {
        ActivityFm actFm = new ActivityFm();
        ActivityCoefficientFm coefficientFm = new ActivityCoefficientFm();
        ActivityInteractionCoefficientFm ActivityInteractionCoefficientFm = new ActivityInteractionCoefficientFm();
        ActivityCoefficientAtInfiniteDilution activityCoefficientAtInfiniteDilution = new ActivityCoefficientAtInfiniteDilution();
        public Form1()
        {
            InitializeComponent();
        }

        private void Activity_Click(object sender, EventArgs e)
        {
            //活度计算窗口

            if (actFm.IsDisposed)
            {
                actFm = new ActivityFm();
                actFm.Show();
            }
            else
            {
                if (!actFm.Visible)

                {
                    actFm.Show();
                }
                else if (actFm.WindowState == FormWindowState.Minimized)
                {
                    actFm.WindowState = FormWindowState.Normal;
                }
            }
            this.WindowState = FormWindowState.Minimized;
        }

        private void ActivityCoeff_Click(object sender, EventArgs e)
        {
            //活度系数计算窗口
            if (coefficientFm.IsDisposed)
            {
                coefficientFm = new ActivityCoefficientFm();
                coefficientFm.Show();
            }
            else
            {
                if (!coefficientFm.Visible)

                {
                    coefficientFm.Show();
                }
                else if (coefficientFm.WindowState == FormWindowState.Minimized)
                {
                    coefficientFm.WindowState = FormWindowState.Normal;
                }
            }
            this.WindowState = FormWindowState.Minimized;
        }

        private void InteractionCoeff_Click(object sender, EventArgs e)
        {
            //活度相互作用系数计算窗口
            if (ActivityInteractionCoefficientFm.IsDisposed)
            {
                ActivityInteractionCoefficientFm = new ActivityInteractionCoefficientFm();
                ActivityInteractionCoefficientFm.Show();
            }
            else
            {
                if (!ActivityInteractionCoefficientFm.Visible)

                {
                    ActivityInteractionCoefficientFm.Show();
                }
                else if (ActivityInteractionCoefficientFm.WindowState == FormWindowState.Minimized)
                {
                    ActivityInteractionCoefficientFm.WindowState = FormWindowState.Normal;
                }
            }
            this.WindowState = FormWindowState.Minimized;
        }

        private void ActivityCoefficientAtInfinitely_Click(object sender, EventArgs e)
        {
            if (activityCoefficientAtInfiniteDilution.IsDisposed)
            {
                ActivityCoefficientAtInfiniteDilution activityCoefficientAtInfiniteDilution = new ActivityCoefficientAtInfiniteDilution();
                activityCoefficientAtInfiniteDilution.Show();
            }
            else
            {
                if (!activityCoefficientAtInfiniteDilution.Visible)
                {
                    activityCoefficientAtInfiniteDilution.Show();
                }
                else if (activityCoefficientAtInfiniteDilution.WindowState == FormWindowState.Minimized)
                {
                    activityCoefficientAtInfiniteDilution.WindowState = FormWindowState.Normal;
                }
            }
            this.WindowState = FormWindowState.Minimized;

        }
    }
}