public class ProcessingOptions
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

    public ProcessingOptions()
    {
        var defaultSettings = new Settings();
        OutputFolder = defaultSettings.OutputFolder;
        JpegQuality = defaultSettings.JpegQuality;
        AutoResize = defaultSettings.AutoResize;
        AutoCorrectAspectRatio = defaultSettings.AutoCorrectAspectRatio;
        MaxWidth = defaultSettings.MaxWidth;
        MaxHeight = defaultSettings.MaxHeight;
        EnableMultiResolution = defaultSettings.EnableMultiResolution;
        UseLocalWebServer = defaultSettings.UseLocalWebServer;
        WebServerPort = defaultSettings.WebServerPort;
    }

    public ProcessingOptions(Settings settings)
    {
        OutputFolder = settings.OutputFolder;
        JpegQuality = settings.JpegQuality;
        AutoResize = settings.AutoResize;
        AutoCorrectAspectRatio = settings.AutoCorrectAspectRatio;
        MaxWidth = settings.MaxWidth;
        MaxHeight = settings.MaxHeight;
        EnableMultiResolution = settings.EnableMultiResolution;
        UseLocalWebServer = settings.UseLocalWebServer;
        WebServerPort = settings.WebServerPort;
    }
}