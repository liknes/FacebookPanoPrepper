namespace FacebookPanoPrepper
{
    public class DashedBorderPanel : Panel
    {
        public DashedBorderPanel()
        {
            this.SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.Gray)
            {
                DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
            };
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
