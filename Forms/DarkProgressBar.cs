using FacebookPanoPrepper.Helpers;
using System.Drawing;
using System.Windows.Forms;

namespace FacebookPanoPrepper.Controls
{
    public class DarkProgressBar : ProgressBar
    {
        public DarkProgressBar()
        {
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rec = e.ClipRectangle;

            rec.Width = (int)(rec.Width * ((double)Value / Maximum));
            if (ProgressBarRenderer.IsSupported)
            {
                // Draw background - adjusted to be lighter (45,45,45 instead of 30,30,30)
                e.Graphics.FillRectangle(new SolidBrush(ThemeManager.IsDarkMode ?
                    Color.FromArgb(45, 45, 45) : SystemColors.Control), e.ClipRectangle);

                // Draw progress
                e.Graphics.FillRectangle(new SolidBrush(ThemeManager.IsDarkMode ?
                    Color.FromArgb(0, 120, 215) : SystemColors.Highlight), 0, 0, rec.Width, rec.Height);
            }
        }
    }
}