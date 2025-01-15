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
    }
}
