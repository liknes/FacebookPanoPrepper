using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FacebookPanoPrepper.Forms;
using FacebookPanoPrepper.Services;
using FacebookPanoPrepper.Models;
using System.Text.Json;

namespace FacebookPanoPrepper;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var services = ConfigureServices();

        if (args.Contains("--gui") || args.Length == 0)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = services.GetRequiredService<MainForm>();
            Application.Run(form);
        }
        else
        {
            var processor = services.GetRequiredService<ImageProcessingService>();
            RunConsoleMode(args, processor);
        }
    }

    private static void RunConsoleMode(string[] args, ImageProcessingService processor)
    {
        Console.WriteLine("Facebook Pano Prepper - Console Mode");
        Console.WriteLine("===================================");

        if (args.Length < 1)
        {
            Console.WriteLine("Usage: FacebookPanoPrepper <folder_path>");
            return;
        }

        string folderPath = args[0];
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine("Error: Folder not found!");
            return;
        }

        var files = Directory.GetFiles(folderPath, "*.*")
            .Where(f => new[] { ".jpg", ".jpeg" }
            .Contains(Path.GetExtension(f).ToLower()))
            .ToList();

        if (!files.Any())
        {
            Console.WriteLine("No JPEG images found in the specified folder.");
            return;
        }

        Console.WriteLine($"Found {files.Count} image(s) to process.");

        foreach (var file in files)
        {
            Console.WriteLine($"\nProcessing: {Path.GetFileName(file)}");
            try
            {
                var outputPath = Path.Combine(
                    Path.GetDirectoryName(file) ?? string.Empty,
                    "360_" + Path.GetFileName(file)
                );

                var progress = new Progress<int>(percent =>
                {
                    Console.Write($"\rProgress: {percent}%");
                });

                var report = processor.ProcessImageAsync(file, outputPath, progress).Result;
                Console.WriteLine(report.GetSummary());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        Console.WriteLine("\nProcessing complete!");
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Load settings if they exist
        ProcessingOptions options;
        if (File.Exists("settings.json"))
        {
            try
            {
                string json = File.ReadAllText("settings.json");
                options = JsonSerializer.Deserialize<ProcessingOptions>(json)
                    ?? new ProcessingOptions();
            }
            catch (Exception)
            {
                // If there's any error reading the settings, use defaults
                options = new ProcessingOptions();
            }
        }
        else
        {
            options = new ProcessingOptions();
        }

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        services.AddSingleton(options);
        services.AddSingleton<ImageProcessingService>();
        services.AddTransient<MainForm>();

        return services.BuildServiceProvider();
    }
}