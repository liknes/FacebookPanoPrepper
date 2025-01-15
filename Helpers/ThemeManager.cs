namespace FacebookPanoPrepper.Helpers
{
    public static class ThemeManager
    {
        // Public color access
        public static Color GetSuccessColor() => IsDarkMode ? ColorSchemes.Dark.Success : ColorSchemes.Light.Success;
        public static Color GetErrorColor() => IsDarkMode ? ColorSchemes.Dark.Error : ColorSchemes.Light.Error;
        public static Color GetWarningColor() => IsDarkMode ? ColorSchemes.Dark.Warning : ColorSchemes.Light.Warning;
        public static Color GetTextColor() => IsDarkMode ? ColorSchemes.Dark.Text : ColorSchemes.Light.Text;
        public static Color GetBackgroundColor() => IsDarkMode ? ColorSchemes.Dark.Background : ColorSchemes.Light.Background;
        public static Color GetDropZoneColor() => IsDarkMode ? ColorSchemes.Dark.DropZone : ColorSchemes.Light.DropZone;
        public static Color GetSectionColor() => IsDarkMode ? ColorSchemes.Dark.Section : ColorSchemes.Light.Section;

        private static class ColorSchemes
        {
            public static readonly ColorScheme Light = new ColorScheme
            {
                Background = Color.White,
                Text = Color.Black,
                Success = Color.Green,
                Error = Color.Red,
                Warning = Color.Orange,
                DropZone = Color.WhiteSmoke,
                Section = Color.FromArgb(240, 240, 240)
            };

            public static readonly ColorScheme Dark = new ColorScheme
            {
                Background = Color.FromArgb(32, 32, 32),
                Text = Color.FromArgb(240, 240, 240),
                Success = Color.LightGreen,
                Error = Color.IndianRed,
                Warning = Color.Gold,
                DropZone = Color.FromArgb(45, 45, 48),
                Section = Color.FromArgb(50, 50, 50)
            };
        }

        private class ColorScheme
        {
            public Color Background { get; set; }
            public Color Text { get; set; }
            public Color Success { get; set; }
            public Color Error { get; set; }
            public Color Warning { get; set; }
            public Color DropZone { get; set; }
            public Color Section { get; set; }
        }

        public static bool IsDarkMode { get; private set; } = false;

        public static void ApplyTheme(Form form, bool darkMode)
        {
            IsDarkMode = darkMode;
            ApplyThemeToControl(form);
        }

        private static void ApplyThemeToControl(Control control)
        {
            control.BackColor = GetBackgroundColor();
            control.ForeColor = GetTextColor();

            if (control is RichTextBox rtb)
            {
                rtb.BackColor = GetSectionColor();
            }
            else if (control is Panel panel && panel.Name == "dropPanel")
            {
                panel.BackColor = GetDropZoneColor();
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child);
            }
        }
    }
}