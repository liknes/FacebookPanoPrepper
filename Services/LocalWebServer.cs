using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

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
            _app.UseDefaultFiles();
            _app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(_rootPath),
                RequestPath = ""
            });

            // Configure the URLs to listen on
            _app.Urls.Add($"http://localhost:{_port}");
        }

        public async Task StartAsync()
        {
            await _app.StartAsync();
        }

        public void Dispose()
        {
            _app?.StopAsync().Wait();
            _app?.DisposeAsync().AsTask().Wait();
        }
    }
}