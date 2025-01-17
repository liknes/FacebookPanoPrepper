using Microsoft.WindowsAPICodePack.Shell;

namespace FacebookPanoPrepper.Services
{
    public class WindowsPropertyService
    {
        public void SetImageProperties(string imagePath, string title, string description)
        {
            try
            {
                using (var file = ShellFile.FromFilePath(imagePath))
                {
                    file.Properties.System.Title.Value = title;
                    file.Properties.System.Comment.Value = description;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error setting properties for {Path.GetFileName(imagePath)}: {ex.Message}", ex);
            }
        }

        public (string Title, string Description) GetImageProperties(string imagePath)
        {
            try
            {
                using (var file = ShellFile.FromFilePath(imagePath))
                {
                    return (
                        file.Properties.System.Title.Value ?? "",
                        file.Properties.System.Comment.Value ?? ""
                    );
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading properties from {Path.GetFileName(imagePath)}: {ex.Message}", ex);
            }
        }
    }
}
