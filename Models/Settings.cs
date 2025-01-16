public class Settings
{
    public string OutputFolder { get; set; }
    public int JpegQuality { get; set; }
    public bool AutoResize { get; set; }
    public bool AutoCorrectAspectRatio { get; set; }
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }
    public bool EnableMultiResolution { get; set; }
    public bool UseLocalWebServer { get; set; }
    public int WebServerPort { get; set; }

    public Settings()
    {
        OutputFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "360 Panoramas"
        );
        JpegQuality = 85;
        AutoResize = true;
        AutoCorrectAspectRatio = true;
        MaxWidth = 4096;
        MaxHeight = 2048;
        EnableMultiResolution = false;
        UseLocalWebServer = false;
        WebServerPort = 8080;
    }
}