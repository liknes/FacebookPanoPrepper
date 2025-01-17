namespace FacebookPanoPrepper.Helpers
{
    public static class AppVersion
    {
        public const string Version = "1.3.0";
        public static string FullVersion => $"v{Version}";

        // Semantic version components
        public static readonly int Major = 1;
        public static readonly int Minor = 3;
        public static readonly int Patch = 0;

        // Build date
        public static readonly DateTime BuildDate = new DateTime(2025, 1, 17);
    }
}
