using FacebookPanoPrepper.Models;
using System.Drawing;

namespace FacebookPanoPrepper.Services
{
    public class ImageProcessor
    {
        // Constants for Facebook 360 photo requirements
        private const int MAX_DIMENSION = 30000; // Maximum pixels in any dimension
        private const int MAX_TOTAL_PIXELS = 135000000; // Maximum total pixels
        private const double IDEAL_ASPECT_RATIO = 2.0;
        private const double ASPECT_RATIO_TOLERANCE = 0.1;
        private const long MAX_JPEG_SIZE_BYTES = 30 * 1024 * 1024; // Recommended 30MB for 360 photos
        private const long ABSOLUTE_MAX_JPEG_SIZE = 45 * 1024 * 1024; // Absolute maximum 45MB for JPEGs

        public void ProcessImage(string imagePath, string outputFolder)
        {
            try
            {
                Console.WriteLine($"\nProcessing: {Path.GetFileName(imagePath)}");

                // Check file size
                var fileInfo = new FileInfo(imagePath);
                if (fileInfo.Length > ABSOLUTE_MAX_JPEG_SIZE)
                {
                    Console.WriteLine($"Error: File size ({fileInfo.Length / 1024 / 1024}MB) exceeds Facebook's absolute maximum of 45MB");
                    return;
                }
                if (fileInfo.Length > MAX_JPEG_SIZE_BYTES)
                {
                    Console.WriteLine($"Warning: File size ({fileInfo.Length / 1024 / 1024}MB) exceeds Facebook's recommended maximum of 30MB");
                }

                using var image = Image.FromFile(imagePath);
                var validationResult = ValidateImage(image);

                if (!validationResult.IsValid)
                {
                    Console.WriteLine($"Image validation failed: {validationResult.Message}");
                    return;
                }

                string outputPath = CreateOutputPath(imagePath, outputFolder);
                AddMetadataAndSave(imagePath, outputPath, image.Width, image.Height);

                Console.WriteLine($"Successfully processed: {Path.GetFileName(outputPath)}");
                PrintImageSpecs(image, fileInfo.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {Path.GetFileName(imagePath)}: {ex.Message}");
            }
        }

        private ImageValidationResult ValidateImage(Image image)
        {
            // Check maximum dimension
            if (image.Width > MAX_DIMENSION || image.Height > MAX_DIMENSION)
            {
                return new ImageValidationResult(false,
                    $"Image too large. Maximum allowed dimension is {MAX_DIMENSION}px. Current: {image.Width}x{image.Height}");
            }

            // Check total pixels
            long totalPixels = (long)image.Width * image.Height;
            if (totalPixels > MAX_TOTAL_PIXELS)
            {
                return new ImageValidationResult(false,
                    $"Total pixel count ({totalPixels:N0}) exceeds maximum of {MAX_TOTAL_PIXELS:N0}");
            }

            // Check aspect ratio
            double aspectRatio = (double)image.Width / image.Height;
            if (Math.Abs(aspectRatio - IDEAL_ASPECT_RATIO) > ASPECT_RATIO_TOLERANCE)
            {
                return new ImageValidationResult(false,
                    $"Aspect ratio should be 2:1. Current ratio: {aspectRatio:F2}:1");
            }

            return new ImageValidationResult(true, "Valid");
        }

        private void PrintImageSpecs(Image image, long fileSize)
        {
            Console.WriteLine("\nImage Specifications:");
            Console.WriteLine($"Resolution: {image.Width}x{image.Height}");
            Console.WriteLine($"Total Pixels: {((long)image.Width * image.Height):N0}");
            Console.WriteLine($"Aspect Ratio: {((double)image.Width / image.Height):F2}:1");
            Console.WriteLine($"File Size: {fileSize / 1024 / 1024}MB");
        }

        private void AddMetadataAndSave(string inputPath, string outputPath, int width, int height)
        {
            byte[] imageBytes = File.ReadAllBytes(inputPath);
            string xmpMetadata = CreateXmpMetadata(width, height);
            byte[] xmpBytes = System.Text.Encoding.UTF8.GetBytes(xmpMetadata);

            using var ms = new MemoryStream();

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

            // Save the file
            File.WriteAllBytes(outputPath, ms.ToArray());

            // Verify the metadata was written
            try
            {
                using var image = Image.FromFile(outputPath);
                var properties = image.PropertyItems;
                Console.WriteLine("Metadata successfully embedded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not verify metadata: {ex.Message}");
            }
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

        private string CreateOutputPath(string inputPath, string outputFolder)
        {
            string filename = Path.GetFileNameWithoutExtension(inputPath);
            return Path.Combine(outputFolder, filename + "_360.jpg");
        }
    }
}
