using FacebookPanoPrepper.Models;
using System.Text;

namespace FacebookPanoPrepper.Services
{
    public class HtmlViewerService
    {
        public void CreateHtmlViewer(string outputPath, List<(string FilePath, MultiResImage MultiRes)> panoramaFiles)
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
    </style>
</head>
<body>
    <h1>360° Panorama Gallery</h1>
    <p class='note'>Note: Internet connection required for the 360° viewer to work.</p>");

            // Add each panorama
            for (int i = 0; i < panoramaFiles.Count; i++)
            {
                var (filePath, multiRes) = panoramaFiles[i];
                var fileName = Path.GetFileName(filePath);

                sb.AppendLine($@"
    <div class='pano-container'>
        <h2 class='pano-title'>{fileName}</h2>");

                // Add image info if it's a multi-resolution image
                if (multiRes != null)
                {
                    sb.AppendLine($@"
        <p class='image-info'>Original size: {multiRes.Width}x{multiRes.Height} pixels (Multi-resolution enabled)</p>");
                }

                sb.AppendLine($@"
        <div id='panorama{i}' class='panorama'></div>
        <script>");

                if (multiRes != null)
                {
                    // Multi-resolution configuration
                    var relativePath = Path.GetRelativePath(
                        Path.GetDirectoryName(outputPath),
                        multiRes.BasePath).Replace("\\", "/");

                    sb.AppendLine($@"
            pannellum.viewer('panorama{i}', {{
                type: 'multires',
                multiRes: {{
                    basePath: '{relativePath}',
                    path: '/%l_%x_%y.jpg',
                    fallbackPath: '/fallback/%s.jpg',
                    extension: 'jpg',
                    tileResolution: {multiRes.TileSize},
                    maxLevel: {multiRes.Levels - 1},
                    cubeResolution: {multiRes.Width}
                }},
                autoLoad: true,
                autoRotate: -2,
                compass: true,
                showFullscreenCtrl: true,
                mouseZoom: true
            }});");
                }
                else
                {
                    // Regular panorama configuration
                    var imageBytes = File.ReadAllBytes(filePath);
                    var base64Image = Convert.ToBase64String(imageBytes);
                    var dataUrl = $"data:image/jpeg;base64,{base64Image}";

                    sb.AppendLine($@"
            pannellum.viewer('panorama{i}', {{
                type: 'equirectangular',
                panorama: '{dataUrl}',
                autoLoad: true,
                autoRotate: -2,
                compass: true,
                showFullscreenCtrl: true,
                mouseZoom: true
            }});");
                }

                sb.AppendLine(@"        </script>
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