using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace FacebookPanoPrepper.Services
{
    public class LocalWebServer : IDisposable
    {
        private readonly WebApplication _app;
        private readonly string _rootPath;
        private readonly int _port;
        public string BaseUrl => $"http://localhost:{_port}";

        public LocalWebServer(string rootPath, int port)
        {
            _rootPath = rootPath;
            _port = port;

            var builder = WebApplication.CreateBuilder();

            // Add CORS services
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            _app = builder.Build();

            // Configure the HTTP request pipeline
            _app.UseCors();

            // Add cache control middleware
            _app.Use(async (context, next) =>
            {
                context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
                await next();
            });

            // Add batches API endpoint
            _app.MapGet("/api/batches", (HttpContext context) =>
            {
                try
                {
                    var currentPath = context.Request.Headers["Referer"].ToString();
                    var currentBatch = "";
                    if (!string.IsNullOrEmpty(currentPath))
                    {
                        var uri = new Uri(currentPath);
                        var segments = uri.Segments;
                        currentBatch = segments.FirstOrDefault(s => s.StartsWith("Batch_"))?.TrimEnd('/');
                    }

                    var batches = Directory.GetDirectories(_rootPath)
                        .Where(d => Path.GetFileName(d).StartsWith("Batch_"))
                        .OrderByDescending(d => d)
                        .Select(d => new
                        {
                            name = Path.GetFileName(d),
                            info = GetBatchInfo(d),
                            isCurrent = Path.GetFileName(d) == currentBatch
                        });

                    return Results.Json(batches);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            // Serve static files
            _app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(_rootPath),
                RequestPath = "",
                ServeUnknownFileTypes = true,
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    ctx.Context.Response.Headers["Pragma"] = "no-cache";
                    ctx.Context.Response.Headers["Expires"] = "0";
                }
            });

            // Configure the URLs to listen on
            _app.Urls.Add($"http://localhost:{_port}");
        }

        private string GetBatchInfo(string batchPath)
        {
            try
            {
                var dirInfo = new DirectoryInfo(batchPath);
                var batchName = dirInfo.Name;
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

        public async Task StartAsync()
        {
            Console.WriteLine($"Starting web server at {BaseUrl}");
            Console.WriteLine($"Serving files from: {_rootPath}");
            await _app.StartAsync();
        }

        public void Dispose()
        {
            try
            {
                _app?.StopAsync().Wait();
                _app?.DisposeAsync().AsTask().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing web server: {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_app != null)
            {
                try
                {
                    await _app.StopAsync();
                    await _app.DisposeAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing web server: {ex.Message}");
                }
            }
        }
    }
}