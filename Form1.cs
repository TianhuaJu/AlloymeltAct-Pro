using System.Diagnostics;

namespace AlloyAct_Pro
{
    public partial class Form1 : Form
    {
        ActivityFm actFm = new ActivityFm();
        ActivityCoefficientFm coefficientFm = new ActivityCoefficientFm();
        ActivityInteractionCoefficientFm ActivityInteractionCoefficientFm = new ActivityInteractionCoefficientFm();
        public Form1()
        {
            InitializeComponent();
        }

        private void Activity_Click(object sender, EventArgs e)
        {
            //��ȼ��㴰��

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
        }

        private void ActivityCoeff_Click(object sender, EventArgs e)
        {
            //���ϵ�����㴰��
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
        }

        private void InteractionCoeff_Click(object sender, EventArgs e)
        {
            //����໥����ϵ�����㴰��
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
        }
    }
}