# Facebook Pano Prepper

A Windows desktop application that helps prepare panoramic images for optimal display on Facebook by adding the required metadata and adjusting image properties to ensure proper 360-degree viewing. Today many cameras, DJI Drones for example, create images that Facebook will identify as panoramic images. If you resize the images or do any kind of work on them, you will have to use an application like Facebook Pano Prepper to fix the meta tags so that Facebook correctly display them as 360 panoramic images.

## Features

- **Drag & Drop Interface**: Simple drag-and-drop functionality for processing single or multiple panoramic images
- **Batch Processing**: Process multiple panoramic images simultaneously
- **Dark/Light Theme**: Built-in theme switching for comfortable viewing in any environment
- **Real-time Progress**: Visual feedback with progress bars and detailed logging
- **Detailed Reports**: Comprehensive processing reports showing:
  - Original image specifications
  - Processing status
  - File size information
  - Aspect ratio details
  - Warning messages (if any)
- **Settings Management**: Configurable output options and processing parameters

## Requirements

- Windows OS
- .NET 8.0 or later
- Sufficient disk space for image processing

## Installation

1. Download the latest release from the [Releases](link-to-releases) page
2. Extract the ZIP file to your preferred location
3. Run `FacebookPanoPrepper.exe`

## Usage

1. **Launch the Application**
   - Start FacebookPanoPrepper.exe
   - The main window will display a drop zone for your images

2. **Process Images**
   - Drag and drop one or more panoramic images onto the application window
   - The application will automatically begin processing
   - Progress is shown in real-time
   - Results are displayed in the log window

3. **Configure Settings**
   - Access settings through File ? Settings
   - Customize output folder location
   - Adjust processing parameters as needed

4. **View Results**
   - Processed images are saved with "360_" prefix
   - Check the log window for detailed processing information
   - Status bar shows overall processing status

## Features in Detail

### Image Processing
- Automatically adds required 360-degree metadata
- Preserves original image quality
- Handles multiple image formats (JPG/JPEG)
- Maintains aspect ratio requirements for Facebook

### User Interface
- Clean, modern Windows Forms interface
- Intuitive drag-and-drop functionality
- Real-time processing feedback
- Dark/Light theme support
- Detailed logging with color-coded status messages

### Error Handling
- Comprehensive error reporting
- Warning system for potential issues
- Clear success/failure indicators
- Detailed processing logs

## Technical Details

Built using:
- C# (.NET 8.0)
- Windows Forms
- Microsoft.Extensions.Logging
- Custom image processing services

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Support

For issues, questions, or suggestions, please:
1. Check the [Issues](link-to-issues) page
2. Create a new issue if needed
3. Provide detailed information about your problem
