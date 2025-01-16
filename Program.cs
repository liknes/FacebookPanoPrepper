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
            ApplicationConfiguration.Initialize(); // Add this line
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var form = services.GetRequiredService<MainForm>();
                Application.Run(form);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}\n\n{ex.StackTrace}",
                    "Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        else
        {
            var processor = services.GetRequiredService<ImageProcessingService>();
            RunConsoleMode(args, processor);
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Ensure Windows Forms context is initialized
        if (Application.OpenForms.Count == 0)
        {
            Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }

        // Load settings if they exist
        Settings settings;
        if (File.Exists("settings.json"))
        {
            try
            {
                string json = File.ReadAllText("settings.json");
                settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
            catch (Exception)
            {
                settings = new Settings();
            }
        }
        else
        {
            settings = new Settings();
        }

        // Create ProcessingOptions from Settings
        var options = new ProcessingOptions(settings);

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        services.AddSingleton(settings);  // Add Settings to DI
        services.AddSingleton(options);   // Add ProcessingOptions to DI
        services.AddSingleton<ImageProcessingService>();
        services.AddTransient<MainForm>();

        return services.BuildServiceProvider();
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
}