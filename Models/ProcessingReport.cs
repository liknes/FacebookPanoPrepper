using FacebookPanoPrepper.Helpers;
using System.Text;

namespace FacebookPanoPrepper.Models
{
    public record ImageSpecs(
        int Width,
        int Height,
        long FileSizeBytes,
        double AspectRatio,
        string Format
    );

    public class ProcessingReport
    {
        public string FileName { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public bool Success { get; set; }
        public List<string> Warnings { get; set; } = new();
        public ImageSpecs? OriginalSpecs { get; set; }
        public ImageSpecs? ProcessedSpecs { get; set; }
        public DateTime ProcessedDate { get; set; }

        public string GetSummary()
        {
            var summary = new StringBuilder();
            summary.AppendLine($"File: {FileName}");
            summary.AppendLine($"Status: {(Success ? "✓ Success" : "✗ Failed")}");

            if (Success)
            {
                summary.AppendLine($"Output: {OutputPath}");
            }

            if (OriginalSpecs != null)
            {
                summary.AppendLine("Original Specs:");
                summary.AppendLine($"  Resolution: {OriginalSpecs.Width}x{OriginalSpecs.Height}");
                summary.AppendLine($"  Size: {OriginalSpecs.FileSizeBytes / 1024 / 1024}MB");
                summary.AppendLine($"  Aspect Ratio: {OriginalSpecs.AspectRatio:F2}:1");
            }

            if (Warnings.Any())
            {
                summary.AppendLine("Warnings:");
                foreach (var warning in Warnings)
                {
                    summary.AppendLine($"  ⚠ {warning}");
                }
            }

            return summary.ToString();
        }

        public string GetRichTextSummary()
        {
            var summary = new StringBuilder();

            // File header
            summary.AppendLine("╔══════════════════════════════════════");
            summary.AppendLine($"║ File: {FileName}");

            // Status with color
            string statusColor = Success ?
                $"|c{ThemeManager.GetSuccessColor().ToArgb()}|" :
                $"|c{ThemeManager.GetErrorColor().ToArgb()}|";
            summary.AppendLine($"║ Status: {statusColor}{(Success ? "✓ Success" : "✗ Failed")}|");

            if (Success)
            {
                summary.AppendLine($"║ Output: {OutputPath}");
            }

            if (OriginalSpecs != null)
            {
                summary.AppendLine("║");
                summary.AppendLine("║ Original Specs:");
                summary.AppendLine($"║   Resolution: {OriginalSpecs.Width}x{OriginalSpecs.Height}");
                summary.AppendLine($"║   Size: {OriginalSpecs.FileSizeBytes / 1024 / 1024}MB");
                summary.AppendLine($"║   Aspect Ratio: {OriginalSpecs.AspectRatio:F2}:1");
            }

            if (Warnings.Any())
            {
                summary.AppendLine("║");
                summary.AppendLine("║ Warnings:");
                foreach (var warning in Warnings)
                {
                    summary.AppendLine($"║   {$"|c{ThemeManager.GetWarningColor().ToArgb()}|⚠|"} {warning}");
                }
            }

            summary.AppendLine("╚══════════════════════════════════════");
            summary.AppendLine();

            return summary.ToString();
        }
    }
}