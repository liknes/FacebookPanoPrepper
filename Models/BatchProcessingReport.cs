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
            summary.AppendLine("Batch Processing Summary");
            summary.AppendLine("=======================");
            summary.AppendLine($"Total Files: {TotalFiles}");
            summary.AppendLine($"Successfully Processed: {SuccessfulFiles}");
            summary.AppendLine($"Failed: {TotalFiles - SuccessfulFiles}");
            summary.AppendLine($"Processing Time: {ProcessingTime.TotalSeconds:F1} seconds");
            summary.AppendLine($"Average Time per File: {(ProcessingTime.TotalSeconds / TotalFiles):F1} seconds");

            return summary.ToString();
        }
    }
}
