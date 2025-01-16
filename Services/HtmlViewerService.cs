using System.Globalization;
using System.Runtime;
using System.Text;
using FacebookPanoPrepper.Models;

namespace FacebookPanoPrepper.Services
{
    public class HtmlViewerService
    {
        private readonly Settings _settings;

        public HtmlViewerService(Settings settings)
        {
            _settings = settings;
        }

        private string GetBatchFolders(string outputPath)
        {
            try
            {
                // Get the parent directory of the current batch
                var currentBatchDir = Path.GetDirectoryName(outputPath);
                var rootDir = Path.GetDirectoryName(currentBatchDir);

                // Log the paths for debugging
                Console.WriteLine($"Current file: {outputPath}");
                Console.WriteLine($"Current batch dir: {currentBatchDir}");
                Console.WriteLine($"Root dir: {rootDir}");

                // Get all batch directories from the root
                var batchDirs = Directory.GetDirectories(rootDir)
                                        .Where(d => Path.GetFileName(d).StartsWith("Batch_"))
                                        .OrderByDescending(d => d)  // Most recent first
                                        .Select(d => new
                                        {
                                            Path = d,
                                            Name = Path.GetFileName(d),
                                            Current = d == currentBatchDir,
                                            Info = GetBatchInfo(d)
                                        })
                                        .ToList(); // Materialize the query

                // Log found batches
                Console.WriteLine($"Found {batchDirs.Count} batch directories:");
                foreach (var dir in batchDirs)
                {
                    Console.WriteLine($"- {dir.Name} (Current: {dir.Current})");
                }

                var sb = new StringBuilder();
                foreach (var dir in batchDirs)
                {
                    var activeClass = dir.Current ? "active" : "";

                    if (_settings.UseLocalWebServer)
                    {
                        sb.AppendLine($@"<a href=""#"" onclick=""loadBatch('{dir.Name}', event)"" class=""batch-link {activeClass}"">
                             <div class=""batch-date"">{dir.Info}</div>
                           </a>");
                    }
                    else
                    {
                        sb.AppendLine($@"<a href=""../{dir.Name}/viewer.html"" class=""batch-link {activeClass}"">
                             <div class=""batch-date"">{dir.Info}</div>
                           </a>");
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBatchFolders: {ex}");
                return ""; // Return empty string in case of error
            }
        }


        private string GetBatchInfo(string batchPath)
        {
            try
            {
                var dirInfo = new DirectoryInfo(batchPath);
                var batchName = dirInfo.Name;

                // Extract datetime from batch folder name (format: Batch_yyyy-MM-dd_HHmmss)
                var dateStr = batchName.Replace("Batch_", "").Replace("_", " ");
                if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd HHmmss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime batchDate))
                {
                    return batchDate.ToString("yyyy-MM-dd HH:mm:ss");
                }

                return batchName;
            }
            catch
            {
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

            // Start HTML
            sb.AppendLine(@"<!DOCTYPE html>
<html>
<head>
    <title>360° Panorama Viewer</title>
    <meta charset='utf-8'>
    <meta http-equiv='Cache-Control' content='no-cache, no-store, must-revalidate'>
    <meta http-equiv='Pragma' content='no-cache'>
    <meta http-equiv='Expires' content='0'>
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/pannellum@2.5.6/build/pannellum.css'/>
    <script type='text/javascript' src='https://cdn.jsdelivr.net/npm/pannellum@2.5.6/build/pannellum.js'></script>
    <style>
        body { 
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 0;
            display: flex;
            background: #f5f5f5;
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
        .sidebar-collapsed {
            margin-left: -250px;
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
        .toggle-btn {
            position: fixed;
            left: 10px;
            top: 10px;
            z-index: 1000;
            background: #2c3e50;
            color: white;
            border: none;
            padding: 10px;
            cursor: pointer;
            border-radius: 4px;
            transition: left 0.3s ease;
        }
        .toggle-btn.collapsed {
            left: 260px;
        }
        .image-info {
            color: #666;
            font-size: 0.9em;
            margin-top: 5px;
        }
    </style>
</head>
<body>
    <button id=""toggleSidebar"" class=""toggle-btn"">☰</button>
    <div class=""sidebar"">
        <h2>Batches</h2>
        <div id=""batchList"">Loading...</div>
    </div>
    <div class=""main-content"" id=""contentArea"">");

            // Add panoramas
            progress?.Report("Adding panoramas to viewer...");
            for (int i = 0; i < panoramaFiles.Count; i++)
            {
                var (filePath, resolutions) = panoramaFiles[i];
                var fileName = Path.GetFileName(filePath);
                var batchName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(filePath)));
                var relativePath = _settings.UseLocalWebServer
                    ? $"/{batchName}/images/{fileName}"
                    : fileName;

                sb.AppendLine($@"
        <div class='pano-container'>
            <h2 class='pano-title'>{fileName}</h2>
            <div id='panorama{i}' class='panorama'></div>
            <div class='image-info'>Size: {resolutions.Width}x{resolutions.Height}</div>
        </div>

        <script>
            pannellum.viewer('panorama{i}', {{
                type: 'equirectangular',
                panorama: '{relativePath}',
                autoLoad: true
            }});
        </script>");
            }

            // Add JavaScript for sidebar toggle and batch loading
            if (_settings.UseLocalWebServer)
            {
                sb.AppendLine($@"
    <script>
        // Function to load the batch list
        async function loadBatchList() {{
            try {{
                const response = await fetch(`http://localhost:{_settings.WebServerPort}/api/batches`);
                if (!response.ok) throw new Error('Network response was not ok');
                const batches = await response.json();
                
                const batchList = document.getElementById('batchList');
                batchList.innerHTML = '';
                
                batches.forEach(batch => {{
                    const link = document.createElement('a');
                    link.href = '#';
                    link.className = 'batch-link' + (batch.isCurrent ? ' active' : '');
                    link.onclick = (e) => loadBatch(batch.name, e);
                    
                    const dateDiv = document.createElement('div');
                    dateDiv.className = 'batch-date';
                    dateDiv.textContent = batch.info;
                    
                    link.appendChild(dateDiv);
                    batchList.appendChild(link);
                }});
            }} catch (error) {{
                console.error('Error loading batch list:', error);
                document.getElementById('batchList').innerHTML = 'Error loading batches';
            }}
        }}

        // Function to load a specific batch
        async function loadBatch(batchName, event) {{
            if (event) {{
                event.preventDefault();
            }}
            
            try {{
                const port = {_settings.WebServerPort};
                const url = `http://localhost:${{port}}/${{batchName}}/viewer.html`;
                window.location.href = url;
            }} catch (error) {{
                console.error('Error loading batch:', error);
            }}
        }}

        // Load the batch list when the page loads
        window.addEventListener('load', loadBatchList);

        // Toggle sidebar
        document.getElementById('toggleSidebar').onclick = function() {{
            document.querySelector('.sidebar').classList.toggle('sidebar-collapsed');
            document.querySelector('.main-content').classList.toggle('full-width');
            this.classList.toggle('collapsed');
        }};
    </script>");
            }

            sb.AppendLine(@"
    </div>
</body>
</html>");

            // Write the file
            progress?.Report("Saving viewer file...");
            await File.WriteAllTextAsync(outputPath, sb.ToString());
            progress?.Report("Viewer created successfully!");
        }
    }
    }
