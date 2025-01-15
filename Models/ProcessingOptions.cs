namespace FacebookPanoPrepper.Models
{
    public class ProcessingOptions
    {
        public bool AutoResize { get; set; } = true;
        public bool AutoCorrectAspectRatio { get; set; } = true;
        public int MaxWidth { get; set; } = 30000;
        public int MaxHeight { get; set; } = 15000;
        public int JpegQuality { get; set; } = 95;
        public string OutputFolder { get; set; } = "360_processed";
    }
}
