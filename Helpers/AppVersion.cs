using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacebookPanoPrepper.Helpers
{
    public static class AppVersion
    {
        public const string Version = "1.0.0";
        public static string FullVersion => $"v{Version}";

        // Semantic version components
        public static readonly int Major = 1;
        public static readonly int Minor = 0;
        public static readonly int Patch = 0;

        // Build date
        public static readonly DateTime BuildDate = new DateTime(2025, 1, 15);
    }
}
