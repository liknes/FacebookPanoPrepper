using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FacebookPanoPrepper.Helpers;

namespace FacebookPanoPrepper.Controls
{
    public class FancyDropPanel : Panel
    {
        private Color _patternColor;
        private Color _borderColor;

        public FancyDropPanel()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.DoubleBuffer |
                ControlStyles.AllPaintingInWmPaint,
                true);

            UpdateColors();
        }

        public void UpdateColors()
        {
            if (ThemeManager.IsDarkMode)
            {
                // Dark mode - more subtle dark colors
                _patternColor = Color.FromArgb(40, 200, 200, 200);  // More transparent pattern
                _borderColor = Color.FromArgb(70, 200, 200, 200);   // More subtle border
                this.BackColor = Color.FromArgb(40, 40, 43);        // Slightly darker than the main background
            }
            else
            {
                // Light mode - more subtle light colors
                _patternColor = Color.FromArgb(30, 100, 100, 100);  // More transparent pattern
                _borderColor = Color.FromArgb(60, 100, 100, 100);   // More subtle border
                this.BackColor = Color.FromArgb(245, 245, 245);     // Very light gray
            }
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (e?.Graphics == null) return;

            using (var g = e.Graphics)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Fill background
                using (var backgroundBrush = new SolidBrush(ThemeManager.GetDropZoneColor()))
                {
                    g.FillRectangle(backgroundBrush, this.ClientRectangle);
                }

                // Draw background pattern
                using (var brush = new HatchBrush(HatchStyle.LargeGrid,
                    _patternColor, Color.Transparent))
                {
                    g.FillRectangle(brush, this.ClientRectangle);
                }

                // Draw dashed border
                using (var pen = new Pen(_borderColor, 2))
                {
                    pen.DashStyle = DashStyle.Dash;
                    var rect = new Rectangle(1, 1, Width - 3, Height - 3);
                    g.DrawRectangle(pen, rect);
                }

                DrawDropIcon(g);
            }
        }

        private void DrawDropIcon(Graphics g)
        {
            // Calculate center position
            int iconSize = 40;
            int x = (Width - iconSize) / 2;

            // Position the arrow slightly above center
            int y = (Height / 2) - 40; // Move up by 40 pixels from center

            // Draw arrow
            using (var pen = new Pen(_borderColor, 3))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                // Draw arrow stem
                g.DrawLine(pen,
                    x + iconSize / 2, y,
                    x + iconSize / 2, y + iconSize - 10);

                // Draw arrow head
                g.DrawLines(pen, new Point[] {
            new Point(x + 10, y + iconSize - 20),
            new Point(x + iconSize/2, y + iconSize - 10),
            new Point(x + iconSize - 10, y + iconSize - 20)
        });
            }
        }
    }
}