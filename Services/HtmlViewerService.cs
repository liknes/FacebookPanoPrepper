using System.Text;
using FacebookPanoPrepper.Models;

namespace FacebookPanoPrepper.Services
{
    public class HtmlViewerService
    {
        private string GetBatchFolders(string outputPath)
        {
            var rootDir = Path.GetDirectoryName(Path.GetDirectoryName(outputPath)); // Get parent of batch folder
            var batchDirs = Directory.GetDirectories(rootDir)
                                    .OrderByDescending(d => d) // Most recent first
                                    .Select(d => new
                                    {
                                        Path = d,
                                        Name = Path.GetFileName(d),
                                        Current = d == Path.GetDirectoryName(outputPath),
                                        Info = GetBatchInfo(d)
                                    });

            var sb = new StringBuilder();
            foreach (var dir in batchDirs)
            {
                var activeClass = dir.Current ? "active" : "";
                sb.AppendLine($@"<a href=""javascript:loadBatch('{dir.Name}')"" class=""batch-link {activeClass}"">
                             <div class=""batch-date"">{dir.Info}</div>
                           </a>");
            }
            return sb.ToString();
        }

        private string GetBatchInfo(string batchPath)
        {
            try
            {
                var panoramaCount = Directory.GetFiles(batchPath, "360_*.jpg").Length;
                var dateStr = Path.GetFileName(batchPath);

                // Check if the folder name matches our expected format
                if (dateStr.Length >= 10 && dateStr.Contains('_'))
                {
                    var datePart = dateStr.Split('_')[0];
                    if (DateTime.TryParseExact(datePart, "yyyy-MM-dd", null,
                        System.Globalization.DateTimeStyles.None, out DateTime date))
                    {
                        return $"{date:MMM dd, yyyy} ({panoramaCount} images)";
                    }
                }

                // If date parsing fails, just return the folder name and image count
                return $"{dateStr} ({panoramaCount} images)";
            }
            catch (Exception)
            {
                // If anything goes wrong, return a simple fallback
                return Path.GetFileName(batchPath);
            }
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

        public async Task CreateHtmlViewerAsync(string outputPath, List<(string FilePath, PanoramaResolutions Resolutions)> panoramaFiles, IProgress<string> progress = null)
        {
            var sb = new StringBuilder();

            // Start HTML with sidebar
            sb.AppendLine(@"<!DOCTYPE html>
<html>
<head>
    <title>360° Panorama Viewer</title>
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/pannellum@2.5.6/build/pannellum.css'/>
    <script type='text/javascript' src='https://cdn.jsdelivr.net/npm/pannellum@2.5.6/build/pannellum.js'></script>
    <style>
        body { 
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 0;
            display: flex;
            background: #f5f5f5;
            min-height: 100vh;
        }
        .sidebar {
            width: 250px;
            background: #2c3e50;
            height: 100vh;
            padding: 20px 0;
            overflow-y: auto;
            position: fixed;
            transition: margin-left 0.3s ease;
        }
        .sidebar h2 {
            color: white;
            padding: 0 20px;
            font-size: 16px;
            margin-bottom: 20px;
        }
        .batch-link {
            display: block;
            padding: 15px 20px;
            color: #ecf0f1;
            text-decoration: none;
            transition: background 0.3s;
            border-bottom: 1px solid #34495e;
        }
        .batch-link:hover {
            background: #34495e;
        }
        .batch-link.active {
            background: #3498db;
        }
        .batch-date {
            font-size: 14px;
        }
        .main-content {
            margin-left: 250px;
            padding: 20px;
            flex: 1;
            max-width: 1200px;
            transition: margin-left 0.3s ease;
        }
        .main-content.full-width {
            margin-left: 0;
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
        .loading-indicator::after {
            content: '...';
            animation: dots 1.5s steps(4, end) infinite;
        }
        @keyframes dots {
            0%, 20% { content: ''; }
            40% { content: '.'; }
            60% { content: '..'; }
            80% { content: '...'; }
        }
        .toggle-btn {
            position: fixed;
            left: 250px;
            top: 10px;
            z-index: 100;
            background: #2c3e50;
            color: white;
            border: none;
            padding: 8px 12px;
            cursor: pointer;
            border-radius: 0 4px 4px 0;
            transition: left 0.3s ease;
        }
        .toggle-btn.collapsed {
            left: 0;
        }
        .sidebar-collapsed {
            margin-left: -250px;
        }
    </style>
</head>
<body>
    <button id=""toggleSidebar"" class=""toggle-btn"">≡</button>
    <div class=""sidebar"" id=""sidebar"">
        <h2>Batch Folders</h2>
        " + GetBatchFolders(outputPath) + @"
    </div>
    <div class=""main-content"" id=""mainContent"">
        <div id=""contentArea"">
            <h1>360° Panorama Gallery</h1>
            <p class='note'>Note: Internet connection required for the 360° viewer to work.</p>");

            // Add each panorama
            for (int i = 0; i < panoramaFiles.Count; i++)
            {
                progress?.Report($"Processing panorama {i + 1} of {panoramaFiles.Count}");

                var (filePath, resolutions) = panoramaFiles[i];
                var fileName = Path.GetFileName(filePath);

                // Create data URLs for each resolution
                var lowResData = resolutions.LowResPath != null
                    ? await CreateBase64DataUrlAsync(resolutions.LowResPath, progress)
                    : null;

                var mediumResData = resolutions.MediumResPath != null
                    ? await CreateBase64DataUrlAsync(resolutions.MediumResPath, progress)
                    : null;

                var fullResData = await CreateBase64DataUrlAsync(resolutions.FullResPath, progress);

                // Add panorama container
                sb.AppendLine($@"
    <div class='pano-container'>
        <h2 class='pano-title'>{fileName}</h2>
        <div id='panorama{i}' class='panorama'></div>
        <div class='loading-indicator' id='loadingIndicator{i}'>Loading...</div>
        <div class='image-info'>Size: {resolutions.Width}x{resolutions.Height}</div>
    </div>
    <script>
        (function() {{
            var loaded = 'none';
            var loadingIndicator = document.getElementById('loadingIndicator{i}');

            function createViewer(imageUrl) {{
                pannellum.viewer('panorama{i}', {{
                    type: 'equirectangular',
                    panorama: imageUrl,
                    autoLoad: true
                }});
            }}

            {(lowResData != null ? $@"
                var lowImg = new Image();
                lowImg.onload = function() {{
                    if (loaded === 'none') {{
                        createViewer('{lowResData}');
                        loaded = 'low';
                        loadingIndicator.textContent = 'Loading medium resolution...';
                    }}
                }};
                lowImg.src = '{lowResData}';" : "")}

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
    </script>");
            }

            // Close HTML and add JavaScript (after the loop)
            sb.AppendLine(@"
        </div>
    </div>
    <script>
        // Toggle sidebar
        document.getElementById('toggleSidebar').onclick = function() {
            document.querySelector('.sidebar').classList.toggle('sidebar-collapsed');
            document.querySelector('.main-content').classList.toggle('full-width');
            this.classList.toggle('collapsed');
        };

        // Load batch content
        function loadBatch(batchName) {
            const currentPath = window.location.pathname;
            const rootPath = currentPath.substring(0, currentPath.lastIndexOf('/'));
            const newPath = rootPath.substring(0, rootPath.lastIndexOf('/')) + '/' + batchName + '/viewer.html';
            
            // Update content area
            fetch(newPath)
                .then(response => response.text())
                .then(html => {
                    const parser = new DOMParser();
                    const doc = parser.parseFromString(html, 'text/html');
                    const content = doc.querySelector('#contentArea').innerHTML;
                    document.querySelector('#contentArea').innerHTML = content;
                    
                    // Update active state in menu
                    document.querySelectorAll('.batch-link').forEach(link => {
                        link.classList.remove('active');
                        if(link.getAttribute('href').includes(batchName)) {
                            link.classList.add('active');
                        }
                    });

                    // Update URL without page reload
                    history.pushState({}, '', newPath);
                })
                .catch(error => console.error('Error loading batch:', error));
        }

        // Handle browser back/forward
        window.onpopstate = function(event) {
            const batchName = window.location.pathname.split('/').slice(-2)[0];
            loadBatch(batchName);
        };
    </script>
</body>
</html>");

            // Write the file asynchronously (after everything is built)
            progress?.Report("Saving viewer file...");
            await File.WriteAllTextAsync(outputPath, sb.ToString());
            progress?.Report("Viewer created successfully!");
        }
        }
    }
