using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace FacebookPanoPrepper.Services
{
    public class LocalWebServer : IDisposable
    {
        private IWebHost _webHost;
        private readonly string _contentPath;
        private readonly int _port;
        private bool _isRunning;

        public LocalWebServer(string contentPath, int port = 8080)
        {
            _contentPath = contentPath;
            _port = port;
        }

        public string BaseUrl => $"http://localhost:{_port}";

        public async Task StartAsync()
        {
            if (_isRunning) return;

            _webHost = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.ListenLocalhost(_port);
                })
                .Configure(app =>
                {
                    app.UseCors(builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });

                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(_contentPath),
                        ServeUnknownFileTypes = true
                    });
                })
                .Build();

            await _webHost.StartAsync();
            _isRunning = true;
        }

        public async Task StopAsync()
        {
            if (_webHost != null)
            {
                await _webHost.StopAsync();
                _isRunning = false;
            }
        }

        public void Dispose()
        {
            _webHost?.Dispose();
        }
    }
}