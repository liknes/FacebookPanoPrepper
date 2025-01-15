using FacebookPanoPrepper.Services;

namespace FacebookPanoPrepper;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("FacebookPanoPrepper - 360° Panorama Preparation Tool");
        Console.WriteLine("================================================");
        Console.WriteLine("\nFacebook 360 Photo Requirements:");
        Console.WriteLine("- Maximum dimension: 30,000 pixels");
        Console.WriteLine("- Maximum total pixels: 135,000,000");
        Console.WriteLine("- Recommended aspect ratio: 2:1");
        Console.WriteLine("- Recommended maximum file size: 30MB");
        Console.WriteLine("- Absolute maximum file size: 45MB");
        Console.WriteLine("- Format: JPEG recommended");

        Console.WriteLine("\nEnter the folder path containing panorama images:");
        string folderPath = Console.ReadLine() ?? string.Empty;

        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine("Folder not found!");
            return;
        }

        // Create output folder
        string outputFolder = Path.Combine(folderPath, "360_processed");
        Directory.CreateDirectory(outputFolder);

        // Get all jpg/jpeg files
        var imageFiles = Directory.GetFiles(folderPath, "*.*")
            .Where(file => new[] { ".jpg", ".jpeg" }
            .Contains(Path.GetExtension(file).ToLower()))
            .ToList();

        if (!imageFiles.Any())
        {
            Console.WriteLine("No JPEG images found in the folder!");
            return;
        }

        Console.WriteLine($"\nFound {imageFiles.Count} JPEG files to process.");

        var processor = new ImageProcessor();
        int successCount = 0;
        foreach (string imagePath in imageFiles)
        {
            processor.ProcessImage(imagePath, outputFolder);
            successCount++;
        }

        Console.WriteLine($"\nProcessing complete! Successfully processed {successCount} of {imageFiles.Count} images.");
        Console.WriteLine($"Processed images can be found in: {outputFolder}");
        Console.WriteLine("\nPress any key to exit.");
        Console.ReadKey();
    }
}