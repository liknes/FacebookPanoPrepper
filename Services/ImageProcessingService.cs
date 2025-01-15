using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using FacebookPanoPrepper.Models;
using Microsoft.Extensions.Logging;

namespace FacebookPanoPrepper.Services;

public class ImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;
    private readonly ProcessingOptions _options;

    public ImageProcessingService(ILogger<ImageProcessingService> logger, ProcessingOptions options)
    {
        _logger = logger;
        _options = options;
    }

    public async Task<ProcessingReport> ProcessImageAsync(string inputPath, string outputPath,
    IProgress<int>? progress = null)
    {
        var report = new ProcessingReport
        {
            FileName = Path.GetFileName(inputPath),
            OutputPath = outputPath,
            ProcessedDate = DateTime.UtcNow
        };

        try
        {
            // Ensure output directory exists
            string outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Load the original image
            using var originalImage = Image.FromFile(inputPath);
            report.OriginalSpecs = GetImageSpecs(originalImage, new FileInfo(inputPath).Length);

            // Create a new bitmap from the original
            using var processedImage = new Bitmap(originalImage);

            // Save with metadata
            await SaveProcessedImageAsync(processedImage, outputPath, _options.JpegQuality);

            report.Success = true;
            report.ProcessedSpecs = GetImageSpecs(processedImage, new FileInfo(outputPath).Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {FileName}: {Message}", report.FileName, ex.Message);
            report.Success = false;
            report.Warnings.Add(ex.Message);
            if (ex.InnerException != null)
            {
                report.Warnings.Add($"Details: {ex.InnerException.Message}");
            }
        }

        return report;
    }

    private async Task<Image> ProcessImageWithOptionsAsync(Image originalImage)
    {
        return await Task.Run(() =>
        {
            Image processedImage = new Bitmap(originalImage);

            try
            {
                // Check if resize is needed
                if (_options.AutoResize)
                {
                    if (originalImage.Width > _options.MaxWidth || originalImage.Height > _options.MaxHeight)
                    {
                        var ratio = Math.Min(
                            (double)_options.MaxWidth / originalImage.Width,
                            (double)_options.MaxHeight / originalImage.Height
                        );

                        var newWidth = (int)(originalImage.Width * ratio);
                        var newHeight = (int)(originalImage.Height * ratio);

                        var resized = new Bitmap(newWidth, newHeight);
                        using var graphics = Graphics.FromImage(resized);

                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);

                        processedImage.Dispose();
                        processedImage = resized;
                    }
                }

                // Check if aspect ratio correction is needed
                if (_options.AutoCorrectAspectRatio)
                {
                    const double targetRatio = 2.0;
                    var currentRatio = (double)processedImage.Width / processedImage.Height;

                    if (Math.Abs(currentRatio - targetRatio) > 0.1)
                    {
                        int newWidth, newHeight;
                        if (currentRatio > targetRatio)
                        {
                            newWidth = processedImage.Width;
                            newHeight = (int)(processedImage.Width / targetRatio);
                        }
                        else
                        {
                            newHeight = processedImage.Height;
                            newWidth = (int)(processedImage.Height * targetRatio);
                        }

                        var corrected = new Bitmap(newWidth, newHeight);
                        using var graphics = Graphics.FromImage(corrected);

                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(processedImage, 0, 0, newWidth, newHeight);

                        processedImage.Dispose();
                        processedImage = corrected;
                    }
                }

                return processedImage;
            }
            catch (Exception)
            {
                processedImage.Dispose();
                throw;
            }
        });
    }

    private async Task SaveProcessedImageAsync(Image image, string outputPath, int quality)
    {
        await Task.Run(() =>
        {
            try
            {
                // Create temp directory if it doesn't exist
                string tempDir = Path.GetDirectoryName(outputPath) ?? string.Empty;
                if (!string.IsNullOrEmpty(tempDir) && !Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                // First save the image with quality settings
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                var codec = ImageCodecInfo.GetImageDecoders()
                    .First(c => c.FormatID == ImageFormat.Jpeg.Guid);

                // Save to a temporary file first
                string tempPath = Path.Combine(
                    Path.GetDirectoryName(outputPath) ?? string.Empty,
                    "temp_" + Path.GetFileName(outputPath)
                );

                image.Save(tempPath, codec, encoderParameters);

                // Now read the file and add XMP metadata
                byte[] imageBytes = File.ReadAllBytes(tempPath);
                string xmpMetadata = CreateXmpMetadata(image.Width, image.Height);
                byte[] xmpBytes = System.Text.Encoding.UTF8.GetBytes(xmpMetadata);

                using (var ms = new MemoryStream())
                {
                    // Write SOI marker
                    ms.Write(imageBytes, 0, 2);

                    // Write APP1 marker for XMP
                    ms.WriteByte(0xFF);
                    ms.WriteByte(0xE1);

                    // Write length of XMP section (including length bytes)
                    int length = xmpBytes.Length + 2 + 29; // 29 is for "http://ns.adobe.com/xap/1.0/" + null terminator
                    ms.WriteByte((byte)((length >> 8) & 0xFF));
                    ms.WriteByte((byte)(length & 0xFF));

                    // Write XMP identifier
                    byte[] xmpIdentifier = System.Text.Encoding.ASCII.GetBytes("http://ns.adobe.com/xap/1.0/");
                    ms.Write(xmpIdentifier, 0, xmpIdentifier.Length);
                    ms.WriteByte(0x00); // null terminator

                    // Write XMP data
                    ms.Write(xmpBytes, 0, xmpBytes.Length);

                    // Write the rest of the original image
                    ms.Write(imageBytes, 2, imageBytes.Length - 2);

                    // Save the final file
                    File.WriteAllBytes(outputPath, ms.ToArray());
                }

                // Clean up temporary file
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveProcessedImageAsync: {Message}", ex.Message);
                throw new Exception($"Failed to save image: {ex.Message}", ex);
            }
        });
    }

    private ImageSpecs GetImageSpecs(Image image, long fileSize)
    {
        return new ImageSpecs(
            image.Width,
            image.Height,
            fileSize,
            (double)image.Width / image.Height,
            image.RawFormat.ToString()
        );
    }

    private string CreateXmpMetadata(int width, int height) =>
        $@"<?xpacket begin=""﻿"" id=""W5M0MpCehiHzreSzNTczkc9d""?>
        <x:xmpmeta xmlns:x=""adobe:ns:meta/"" x:xmptk=""Adobe XMP Core 5.6-c140 79.160451, 2017/05/06-01:08:21        "">
         <rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#"">
          <rdf:Description rdf:about=""""
            xmlns:GPano=""http://ns.google.com/photos/1.0/panorama/""
            xmlns:xmp=""http://ns.adobe.com/xap/1.0/""
            xmlns:dc=""http://purl.org/dc/elements/1.1/""
            xmlns:xmpMM=""http://ns.adobe.com/xap/1.0/mm/""
            xmlns:stEvt=""http://ns.adobe.com/xap/1.0/sType/ResourceEvent#"">
           <GPano:ProjectionType>equirectangular</GPano:ProjectionType>
           <GPano:FullPanoWidthPixels>{width}</GPano:FullPanoWidthPixels>
           <GPano:FullPanoHeightPixels>{height}</GPano:FullPanoHeightPixels>
           <GPano:CroppedAreaImageWidthPixels>{width}</GPano:CroppedAreaImageWidthPixels>
           <GPano:CroppedAreaImageHeightPixels>{height}</GPano:CroppedAreaImageHeightPixels>
           <GPano:CroppedAreaLeftPixels>0</GPano:CroppedAreaLeftPixels>
           <GPano:CroppedAreaTopPixels>0</GPano:CroppedAreaTopPixels>
           <GPano:InitialViewHeadingDegrees>180</GPano:InitialViewHeadingDegrees>
           <GPano:InitialHorizontalFOVDegrees>90</GPano:InitialHorizontalFOVDegrees>
           <GPano:StitchingSoftware>FacebookPanoPrepper</GPano:StitchingSoftware>
           <xmp:CreateDate>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</xmp:CreateDate>
           <xmp:ModifyDate>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</xmp:ModifyDate>
           <xmp:CreatorTool>FacebookPanoPrepper</xmp:CreatorTool>
          </rdf:Description>
         </rdf:RDF>
        </x:xmpmeta>
        <?xpacket end=""w""?>";
}