using FacebookPanoPrepper.Helpers;
using System.Text;

namespace FacebookPanoPrepper.Models
{
    public class BatchProcessingReport
    {
        public int TotalFiles { get; set; }
        public int SuccessfulFiles { get; set; }
        public List<ProcessingReport> Reports { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan ProcessingTime { get; set; }

        public string GetSummary()
        {
            var summary = new StringBuilder();

            // Use the current theme text color
            string textColor = $"|c{ThemeManager.GetTextColor().ToArgb()}|";

            summary.Append(textColor)
                .AppendLine("Batch Processing Summary")
                .AppendLine("=======================")
                .AppendLine($"Total Files: {TotalFiles}")
                .AppendLine($"Successfully Processed: {SuccessfulFiles}")
                .AppendLine($"Failed: {TotalFiles - SuccessfulFiles}")
                .AppendLine($"Processing Time: {ProcessingTime.TotalSeconds:F1} seconds")
                .AppendLine($"Average Time per File: {(TotalFiles > 0 ? ProcessingTime.TotalSeconds / TotalFiles : 0):F1} seconds");

            return summary.ToString();
        }
    }
}
