using FacebookPanoPrepper.Controls;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FacebookPanoPrepper.Helpers
{
    public static class ThemeManager
    {
        private class ColorScheme
        {
            public Color Background { get; set; }
            public Color Text { get; set; }
            public Color Success { get; set; }
            public Color Error { get; set; }
            public Color Warning { get; set; }
            public Color DropZone { get; set; }
            public Color Section { get; set; }
            public Color ProgressBarBackground { get; set; }
            public Color ProgressBarForeground { get; set; }
        }

        private static class ColorSchemes
        {
            public static readonly ColorScheme Light = new ColorScheme
            {
                Background = Color.White,
                Text = Color.Black,
                Success = Color.FromArgb(0, 128, 0),      
                Error = Color.FromArgb(192, 0, 0),        
                Warning = Color.FromArgb(176, 124, 0),   
                DropZone = Color.WhiteSmoke,
                Section = Color.FromArgb(240, 240, 240),
                ProgressBarBackground = SystemColors.Control,
                ProgressBarForeground = SystemColors.Highlight
            };

            public static readonly ColorScheme Dark = new ColorScheme
            {
                Background = Color.FromArgb(32, 32, 32),
                Text = Color.FromArgb(220, 220, 220),     
                Success = Color.FromArgb(92, 184, 92),    
                Error = Color.FromArgb(217, 83, 79),      
                Warning = Color.FromArgb(240, 173, 78),   
                DropZone = Color.FromArgb(45, 45, 48),
                Section = Color.FromArgb(50, 50, 50),
                ProgressBarBackground = Color.FromArgb(45, 45, 45),
                ProgressBarForeground = Color.FromArgb(0, 120, 215)
            };
        }

        public static bool IsDarkMode { get; private set; } = false;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref bool attrValue, int attrSize);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr w, IntPtr l);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        // Public color getters
        public static Color GetTextColor() => IsDarkMode ? ColorSchemes.Dark.Text : ColorSchemes.Light.Text;
        public static Color GetSuccessColor() => IsDarkMode ? ColorSchemes.Dark.Success : ColorSchemes.Light.Success;
        public static Color GetErrorColor() => IsDarkMode ? ColorSchemes.Dark.Error : ColorSchemes.Light.Error;
        public static Color GetWarningColor() => IsDarkMode ? ColorSchemes.Dark.Warning : ColorSchemes.Light.Warning;
        public static Color GetBackgroundColor() => IsDarkMode ? ColorSchemes.Dark.Background : ColorSchemes.Light.Background;
        public static Color GetDropZoneColor() => IsDarkMode ? ColorSchemes.Dark.DropZone : ColorSchemes.Light.DropZone;
        public static Color GetSectionColor() => IsDarkMode ? ColorSchemes.Dark.Section : ColorSchemes.Light.Section;

        public static void ApplyTheme(Form form, bool darkMode)
        {
            IsDarkMode = darkMode;

            if (Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000)
            {
                DwmSetWindowAttribute(form.Handle, 20, ref darkMode, Marshal.SizeOf(typeof(bool)));
            }

            ApplyThemeToControl(form);
        }

        private static void ApplyThemeToControl(Control control)
        {
            var colors = IsDarkMode ? ColorSchemes.Dark : ColorSchemes.Light;

            switch (control)
            {
                case FancyDropPanel dropPanel:
                    dropPanel.UpdateColors();
                    break;
                case Label label when label.BackColor == Color.Transparent:
                    // Don't change the background color for transparent labels
                    label.ForeColor = colors.Text;
                    break;
                case RichTextBox rtb:
                    rtb.BackColor = colors.Section;
                    break;
                case DarkProgressBar _:
                    control.Invalidate();
                    break;
                case MenuStrip menuStrip:
                    menuStrip.BackColor = colors.Background;
                    menuStrip.ForeColor = colors.Text;
                    foreach (ToolStripMenuItem item in menuStrip.Items)
                    {
                        ApplyThemeToMenuItem(item, colors);
                    }
                    break;
                case StatusStrip statusStrip:
                    statusStrip.BackColor = colors.Background;
                    statusStrip.ForeColor = colors.Text;
                    foreach (ToolStripItem item in statusStrip.Items)
                    {
                        if (item is ToolStripProgressBar statusProgress)
                        {
                            statusProgress.BackColor = colors.ProgressBarBackground;
                            statusProgress.ForeColor = colors.ProgressBarForeground;
                        }
                    }
                    break;
                default:
                    control.BackColor = colors.Background;
                    control.ForeColor = colors.Text;
                    break;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child);
            }
        }

        private static void ApplyThemeToMenuItem(ToolStripMenuItem item, ColorScheme colors)
        {
            item.BackColor = colors.Background;
            item.ForeColor = colors.Text;

            foreach (ToolStripItem dropDownItem in item.DropDownItems)
            {
                if (dropDownItem is ToolStripMenuItem menuItem)
                {
                    ApplyThemeToMenuItem(menuItem, colors);
                }
            }
        }

        private static void SetProgressBarColor(ProgressBar pBar, Color color)
        {
            SendMessage(pBar.Handle, 0x409, IntPtr.Zero, IntPtr.Zero);
        }
    }
}