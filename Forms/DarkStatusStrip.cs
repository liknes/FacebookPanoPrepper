using FacebookPanoPrepper.Helpers;

namespace FacebookPanoPrepper.Forms
{
    public class DarkStatusStrip : StatusStrip
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            var colors = ThemeManager.GetCurrentScheme();
            this.BackColor = colors.StatusStripBackground;

            foreach (ToolStripItem item in Items)
            {
                if (item is ToolStripProgressBar progressBar)
                {
                    progressBar.BackColor = colors.ScrollBarBackground;
                    progressBar.ForeColor = colors.StatusProgressBar;
                }
            }

            base.OnPaint(e);
        }
    }
}
