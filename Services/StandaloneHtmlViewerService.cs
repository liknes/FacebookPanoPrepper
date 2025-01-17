using FacebookPanoPrepper.Models;
using System.Text;

namespace FacebookPanoPrepper.Services
{
    public class StandaloneHtmlViewerService
    {
        public async Task CreateHtmlViewerAsync(string outputPath, List<(string FilePath, PanoramaResolutions Resolutions)> panoramaFiles, IProgress<string> progress = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"<!DOCTYPE html>
<html>
<head>
    <title>360° Panorama Viewer</title>
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/pannellum@2.5.6/build/pannellum.css'/>
    <script type='text/javascript' src='https://cdn.jsdelivr.net/npm/pannellum@2.5.6/build/pannellum.js'></script>
    <style>
        body { 
            font-family: Arial, sans-serif;
            max-width: 1200px;
            margin: 0 auto;
            padding: 20px;
            background: #f5f5f5;
        }
        .pano-container {
            background: white;
            padding: 15px;
            margin: 15px 0;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .pano-title {
            margin-bottom: 10px;
            color: #333;
        }
        .panorama {
            width: 100%;
            height: 500px;
        }
        .note {
            color: #666;
            font-style: italic;
            margin-top: 20px;
        }
        .image-info {
            color: #666;
            font-size: 0.9em;
            margin-top: 5px;
        }
        .loading-indicator {
            color: #666;
            font-size: 0.8em;
            margin-top: 5px;
        }
    </style>
</head>
<body>
    <h1>360° Panorama Gallery</h1>
    <p class='note'>Note: Internet connection required for the 360° viewer to work.</p>");

            for (int i = 0; i < panoramaFiles.Count; i++)
            {
                var (filePath, resolutions) = panoramaFiles[i];
                var fileName = Path.GetFileName(filePath);
                progress?.Report($"Processing {fileName}...");

                var base64Data = await CreateBase64DataUrlAsync(filePath, progress);

                sb.AppendLine($@"
    <div class='pano-container'>
        <h2 class='pano-title'>{fileName}</h2>
        <p class='image-info'>Original size: {resolutions.Width}x{resolutions.Height} pixels</p>
        <div id='panorama{i}' class='panorama'></div>
        <div id='loading{i}' class='loading-indicator'></div>
        <script>
            (function() {{
                var currentViewer = null;
                
                function createViewer(imageUrl) {{
                    if (currentViewer) {{
                        currentViewer.destroy();
                    }}
                    
                    currentViewer = pannellum.viewer('panorama{i}', {{
                        type: 'equirectangular',
                        panorama: imageUrl,
                        autoLoad: true,
                        autoRotate: -2,
                        compass: true,
                        showFullscreenCtrl: true,
                        mouseZoom: true,
                        hfov: 100,
                        multiResMinHfov: 50
                    }});
                }}

                createViewer('{base64Data}');
            }})();
        </script>
    </div>");
            }

            sb.AppendLine(@"
</body>
</html>");

            await File.WriteAllTextAsync(outputPath, sb.ToString());
            progress?.Report("Viewer created successfully!");
        }

        private async Task<string> CreateBase64DataUrlAsync(string imagePath, IProgress<string> progress = null)
        {
            progress?.Report($"Reading {Path.GetFileName(imagePath)}...");

            using var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            using var memoryStream = new MemoryStream();

            byte[] buffer = new byte[81920]; // 80KB buffer for better performance
            long totalBytes = fileStream.Length;
            long bytesRead = 0;
            int count;

            while ((count = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await memoryStream.WriteAsync(buffer, 0, count);
                bytesRead += count;

                var percentage = (int)((bytesRead * 100) / totalBytes);
                progress?.Report($"Processing {Path.GetFileName(imagePath)}: {percentage}%");
            }

            progress?.Report($"Converting {Path.GetFileName(imagePath)} to base64...");
            var base64Image = Convert.ToBase64String(memoryStream.ToArray());
            return $"data:image/jpeg;base64,{base64Image}";
        }
    }
}
