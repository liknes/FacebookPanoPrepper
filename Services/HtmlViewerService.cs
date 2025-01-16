using System.Text;
using FacebookPanoPrepper.Models;

namespace FacebookPanoPrepper.Services
{
    public class HtmlViewerService
    {
        private string CreateBase64DataUrl(string imagePath)
        {
            var imageBytes = File.ReadAllBytes(imagePath);
            var base64Image = Convert.ToBase64String(imageBytes);
            return $"data:image/jpeg;base64,{base64Image}";
        }

        public void CreateHtmlViewer(string outputPath, List<(string FilePath, PanoramaResolutions Resolutions)> panoramaFiles)
        {
            var sb = new StringBuilder();

            // Start HTML
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

            // Add each panorama
            for (int i = 0; i < panoramaFiles.Count; i++)
            {
                var (filePath, resolutions) = panoramaFiles[i];
                var fileName = Path.GetFileName(filePath);

                // Create data URLs for each resolution
                var lowResData = resolutions.LowResPath != null ? CreateBase64DataUrl(resolutions.LowResPath) : null;
                var mediumResData = resolutions.MediumResPath != null ? CreateBase64DataUrl(resolutions.MediumResPath) : null;
                var fullResData = CreateBase64DataUrl(resolutions.FullResPath);

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

                var loadingIndicator = document.getElementById('loading{i}');
                var loaded = 'low';
                loadingIndicator.textContent = 'Loading higher resolution...';

                // Start with low/initial resolution
                createViewer('{(lowResData ?? fullResData)}');

                {(mediumResData != null ? $@"
                var mediumImg = new Image();
                mediumImg.onload = function() {{
                    if (loaded === 'low') {{
                        createViewer('{mediumResData}');
                        loaded = 'medium';
                        loadingIndicator.textContent = 'Loading full resolution...';
                    }}
                }};
                mediumImg.src = '{mediumResData}';" : "")}

                var fullImg = new Image();
                fullImg.onload = function() {{
                    createViewer('{fullResData}');
                    loaded = 'full';
                    loadingIndicator.textContent = 'Full resolution loaded';
                    setTimeout(function() {{ 
                        loadingIndicator.style.display = 'none';
                    }}, 2000);
                }};
                fullImg.src = '{fullResData}';
            }})();
        </script>
    </div>");
            }

            // Close HTML
            sb.AppendLine(@"
</body>
</html>");

            // Write the file
            File.WriteAllText(outputPath, sb.ToString());
        }
    }
}