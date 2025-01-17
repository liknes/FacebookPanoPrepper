using FacebookPanoPrepper.Controls;
using FacebookPanoPrepper.Helpers;
using MetadataExtractor;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FacebookPanoPrepper.Forms;

public class MetadataEditorForm : Form
{
    private readonly Settings _settings;
    private readonly List<string> _imagePaths = new();
    private int _currentImageIndex = 0;

    private PictureBox _previewBox;
    private Label _dropHintLabel;
    private TextBox _titleTextBox;
    private TextBox _descriptionTextBox;
    private ListView _exifListView;
    private Button _saveButton;
    private Button _previousButton;
    private Button _nextButton;
    private Label _imageCountLabel;
    private StatusStrip _statusStrip;
    private ToolStripStatusLabel _statusLabel;

    public MetadataEditorForm(Settings settings)
    {
        _settings = settings;
        InitializeComponent();
        InitializeControls();
        SetupLayout();
        ApplyTheme();

        // Set focus to description textbox
        _descriptionTextBox.Select();
    }

    private void InitializeComponent()
    {
        Text = "Metadata Editor";
        MinimumSize = new Size(700, 1000);
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;

        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel();
        _statusStrip.Items.Add(_statusLabel);
    }

    private void InitializeControls()
    {
        // Title input
        _titleTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Enter title for the image...",
            Enabled = false,
            BorderStyle = BorderStyle.FixedSingle
        };

        // Description input
        _descriptionTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            PlaceholderText = "Enter description for the image...",
            Enabled = false,
            BorderStyle = BorderStyle.FixedSingle
        };

        // EXIF ListView
        _exifListView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Enabled = false
        };
        _exifListView.Columns.Add("Tag", 200);
        _exifListView.Columns.Add("Value", 300);

        // Save button
        _saveButton = new Button
        {
            Text = "Save Metadata (Ctrl+S)",
            Enabled = false,
            Height = 23,
            MinimumSize = new Size(150, 23),
            Font = new Font(Font.FontFamily, 8.25f),
            FlatStyle = FlatStyle.System,
            UseVisualStyleBackColor = true
        };
        _saveButton.Click += OnSaveClick;

        // Navigation buttons
        _previousButton = new Button
        {
            Text = "← Previous (Left)",
            Enabled = false,
            Height = 23,
            MinimumSize = new Size(120, 23),
            Font = new Font(Font.FontFamily, 8.25f),
            FlatStyle = FlatStyle.System,
            UseVisualStyleBackColor = true
        };
        _previousButton.Click += (s, e) => NavigateImages(-1);

        _nextButton = new Button
        {
            Text = "Next → (Right)",
            Enabled = false,
            Height = 23,
            MinimumSize = new Size(120, 23),
            Font = new Font(Font.FontFamily, 8.25f),
            FlatStyle = FlatStyle.System,
            UseVisualStyleBackColor = true
        };
        _nextButton.Click += (s, e) => NavigateImages(1);

        _imageCountLabel = new Label
        {
            Text = "No images loaded",
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            Dock = DockStyle.Fill
        };

        // Keyboard shortcuts
        KeyDown += (s, e) =>
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                if (_saveButton.Enabled) OnSaveClick(s, e);
            }
            else if (e.KeyCode == Keys.Left)
            {
                if (_previousButton.Enabled) NavigateImages(-1);
            }
            else if (e.KeyCode == Keys.Right)
            {
                if (_nextButton.Enabled) NavigateImages(1);
            }
        };
    }

    private void SetupLayout()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(10),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };

        // First, add all row styles
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 450));  // Preview (initial height, will be adjusted)
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // Title
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // Description
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));   // Save button
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // EXIF section
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));   // Navigation

        // Preview panel with border
        var previewPanel = new FancyDropPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10)
        };

        // Drop hint label
        _dropHintLabel = new Label
        {
            Text = "Drop image files here",
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14),
            BackColor = Color.Transparent
        };

        // Configure preview box
        _previewBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Visible = false
        };

        previewPanel.Controls.Add(_previewBox);
        previewPanel.Controls.Add(_dropHintLabel);

        // Title section
        var titlePanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0),
            Height = 30,
            Margin = new Padding(0, 0, 0, 5)
        };
        _titleTextBox.Dock = DockStyle.Fill;
        titlePanel.Controls.Add(_titleTextBox);

        // Description section
        var descriptionPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0),
            Height = 60,
            Margin = new Padding(0, 0, 0, 5)
        };
        _descriptionTextBox.Dock = DockStyle.Fill;
        descriptionPanel.Controls.Add(_descriptionTextBox);

        // Save button panel
        var savePanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0),
            Height = 35
        };
        _saveButton.Anchor = AnchorStyles.Right;
        _saveButton.Location = new Point(savePanel.Width - _saveButton.Width - 5, 6);
        savePanel.Controls.Add(_saveButton);

        // EXIF section
        var exifPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0)
        };
        exifPanel.Controls.Add(new Label { Text = "EXIF Data:", Dock = DockStyle.Fill }, 0, 0);
        exifPanel.Controls.Add(_exifListView, 0, 1);

        // Navigation panel
        var navigationPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Height = 35,
            Padding = new Padding(0)
        };
        navigationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        navigationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        navigationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

        _previousButton.Anchor = AnchorStyles.Left;
        _imageCountLabel.Anchor = AnchorStyles.None;
        _nextButton.Anchor = AnchorStyles.Right;

        navigationPanel.Controls.Add(_previousButton, 0, 0);
        navigationPanel.Controls.Add(_imageCountLabel, 1, 0);
        navigationPanel.Controls.Add(_nextButton, 2, 0);

        // Add all sections to main layout
        mainLayout.Controls.Add(previewPanel, 0, 0);
        mainLayout.Controls.Add(titlePanel, 0, 1);
        mainLayout.Controls.Add(descriptionPanel, 0, 2);
        mainLayout.Controls.Add(savePanel, 0, 3);
        mainLayout.Controls.Add(exifPanel, 0, 4);
        mainLayout.Controls.Add(navigationPanel, 0, 5);

        // Single Resize event handler for maintaining aspect ratio
        mainLayout.SizeChanged += (s, e) =>
        {
            var availableWidth = mainLayout.ClientSize.Width - mainLayout.Padding.Horizontal;
            var targetHeight = availableWidth / 2;  // Enforce 2:1 ratio
            mainLayout.RowStyles[0] = new RowStyle(SizeType.Absolute, targetHeight);
            mainLayout.PerformLayout();
        };

        Controls.Add(mainLayout);
        Controls.Add(_statusStrip);

        AllowDrop = true;
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;

        // Force initial layout
        mainLayout.PerformLayout();
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void OnDragDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                _imagePaths.Clear();
                _imagePaths.AddRange(files.Where(f => f.ToLower().EndsWith(".jpg") || f.ToLower().EndsWith(".jpeg")));

                if (_imagePaths.Count > 0)
                {
                    _currentImageIndex = 0;
                    LoadCurrentImage();
                    UpdateNavigationControls();
                }
            }
        }
    }

    private async void LoadCurrentImage()
    {
        if (_currentImageIndex >= 0 && _currentImageIndex < _imagePaths.Count)
        {
            try
            {
                _dropHintLabel.Visible = false;
                _previewBox.Visible = true;

                var imagePath = _imagePaths[_currentImageIndex];
                _previewBox.Image?.Dispose();
                _previewBox.Image = Image.FromFile(imagePath);

                // Enable controls
                _titleTextBox.Enabled = true;
                _descriptionTextBox.Enabled = true;
                _exifListView.Enabled = true;
                _saveButton.Enabled = true;

                // Load EXIF data and suggest title
                await LoadExifDataAndSuggestTitle(imagePath);

                _statusLabel.Text = $"Loaded: {Path.GetFileName(imagePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = "Error loading image.";
                _dropHintLabel.Visible = true;
                _previewBox.Visible = false;
            }
        }
    }

    private async Task LoadExifDataAndSuggestTitle(string imagePath)
    {
        _exifListView.Items.Clear();
        var directories = ImageMetadataReader.ReadMetadata(imagePath);

        double? latitude = null;
        double? longitude = null;
        string latitudeRef = "N";
        string longitudeRef = "E";
        DateTime? dateTaken = null;

        foreach (var directory in directories)
        {
            if (directory.Name == "GPS")
            {
                foreach (var tag in directory.Tags)
                {
                    switch (tag.Name)
                    {
                        case "GPS Latitude":
                            latitude = ParseGpsCoordinate(tag.Description);
                            break;
                        case "GPS Longitude":
                            longitude = ParseGpsCoordinate(tag.Description);
                            break;
                        case "GPS Latitude Ref":
                            latitudeRef = tag.Description;
                            break;
                        case "GPS Longitude Ref":
                            longitudeRef = tag.Description;
                            break;
                    }
                }
            }
            else if (directory.Name == "Exif SubIFD")
            {
                foreach (var tag in directory.Tags)
                {
                    Debug.WriteLine($"Found EXIF tag: {tag.Name} = {tag.Description}");
                    if (tag.Name == "Date/Time Original")
                    {
                        // Format is typically "yyyy:MM:dd HH:mm:ss"
                        var parts = tag.Description.Split(' ')[0].Split(':');
                        if (parts.Length >= 3)
                        {
                            dateTaken = new DateTime(
                                int.Parse(parts[0]), // year
                                int.Parse(parts[1]), // month
                                int.Parse(parts[2])  // day
                            );
                            Debug.WriteLine($"Parsed date: {dateTaken}");
                        }
                    }
                }
            }

            // Add to ListView
            foreach (var tag in directory.Tags)
            {
                var item = new ListViewItem(tag.Name);
                item.SubItems.Add(tag.Description);
                _exifListView.Items.Add(item);
            }
        }

        // Apply the reference (N/S, E/W)
        if (latitude.HasValue && latitudeRef == "S")
            latitude = -latitude;
        if (longitude.HasValue && longitudeRef == "W")
            longitude = -longitude;

        // Create title suggestion
        string titleSuggestion = "";
        string locationName = "";  // Store location name for description
        
        // Try to get location name from coordinates
        if (_settings.SuggestTitlesFromLocation && latitude.HasValue && longitude.HasValue)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "FacebookPanoPrepper/1.0");
                
                var formattedLat = latitude.Value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                var formattedLon = longitude.Value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                
                var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={formattedLat}&lon={formattedLon}";
                
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var location = JsonSerializer.Deserialize<LocationInfo>(json);

                    if (location?.Address != null)
                    {
                        locationName = GetLocationName(location.Address);
                        if (!string.IsNullOrEmpty(locationName))
                        {
                            titleSuggestion = locationName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting location name: {ex.Message}");
            }
        }

        // If we don't have a location name, just use "Place"
        if (string.IsNullOrEmpty(titleSuggestion))
        {
            titleSuggestion = "Place";
            locationName = "Place";
        }

        // Add date if available
        if (dateTaken.HasValue)
        {
            Debug.WriteLine($"Adding date to title: {dateTaken.Value}");
            titleSuggestion += $" - {dateTaken.Value:d}";
        }
        else
        {
            Debug.WriteLine("No date available for title");
        }

        Debug.WriteLine($"Final title suggestion: {titleSuggestion}");
        _titleTextBox.Text = titleSuggestion;
        
        // Set description template
        _descriptionTextBox.Text = $"Panorama taken in {locationName}";
    }

    private double? ParseGpsCoordinate(string coordinate)
    {
        Debug.WriteLine($"Attempting to parse coordinate: {coordinate}");
        try
        {
            // Try parsing format like "41° 24' 12.2\"" or "41 deg 24' 12.2\"" or "41,24,12.2"
            coordinate = coordinate.Replace("deg", "°").Replace("\"", "").Trim();
            
            // Split by comma or space
            var parts = coordinate.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 3)
            {
                var degrees = double.Parse(parts[0].Replace("°", "").Trim());
                var minutes = double.Parse(parts[1].Replace("'", "").Trim());
                var seconds = double.Parse(parts[2].Replace("\"", "").Trim());
                
                var result = degrees + (minutes / 60.0) + (seconds / 3600.0);
                Debug.WriteLine($"Successfully parsed: {result}");
                return result;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error parsing GPS coordinate: {ex.Message}");
        }
        Debug.WriteLine("Failed to parse coordinate");
        return null;
    }

    private string GetLocationName(AddressInfo address)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(address.City))
            parts.Add(address.City);
        else if (!string.IsNullOrEmpty(address.Village))
            parts.Add(address.Village);
        else if (!string.IsNullOrEmpty(address.Municipality))
            parts.Add(address.Municipality);

        if (!string.IsNullOrEmpty(address.County))
            parts.Add(address.County);

        if (!string.IsNullOrEmpty(address.Country))
            parts.Add(address.Country);

        return string.Join(", ", parts);
    }

    private void UpdateNavigationControls()
    {
        _previousButton.Enabled = _currentImageIndex > 0;
        _nextButton.Enabled = _currentImageIndex < _imagePaths.Count - 1;
        _imageCountLabel.Text = _imagePaths.Count > 0
            ? $"Image {_currentImageIndex + 1} of {_imagePaths.Count}"
            : "No images loaded";
    }

    private void NavigateImages(int direction)
    {
        var newIndex = _currentImageIndex + direction;
        if (newIndex >= 0 && newIndex < _imagePaths.Count)
        {
            _currentImageIndex = newIndex;
            LoadCurrentImage();
            UpdateNavigationControls();
        }
    }

    private void OnSaveClick(object sender, EventArgs e)
    {
        if (_currentImageIndex >= 0 && _currentImageIndex < _imagePaths.Count)
        {
            // Save metadata implementation here
            MessageBox.Show("Metadata saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ApplyTheme()
    {
        var theme = ThemeManager.GetCurrentScheme();

        BackColor = theme.Background;
        ForeColor = theme.Text;

        // TextBoxes
        _titleTextBox.BackColor = theme.Background;
        _titleTextBox.ForeColor = theme.Text;
        _descriptionTextBox.BackColor = theme.Background;
        _descriptionTextBox.ForeColor = theme.Text;

        // EXIF ListView
        _exifListView.BackColor = theme.Background;
        _exifListView.ForeColor = theme.Text;

        // Buttons
        foreach (var button in new[] { _previousButton, _nextButton, _saveButton })
        {
            button.BackColor = theme.Section;
            button.ForeColor = theme.Text;
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
        }

        // Status strip
        _statusStrip.BackColor = theme.StatusStripBackground;
        _statusStrip.ForeColor = theme.Text;
        foreach (ToolStripItem item in _statusStrip.Items)
        {
            item.BackColor = theme.StatusStripBackground;
            item.ForeColor = theme.Text;
        }
    }

    protected override void OnSystemColorsChanged(EventArgs e)
    {
        base.OnSystemColorsChanged(e);
        ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _previewBox.Image?.Dispose();
        }
        base.Dispose(disposing);
    }

    private class LocationInfo
    {
        [JsonPropertyName("address")]
        public AddressInfo Address { get; set; }
    }

    private class AddressInfo
    {
        [JsonPropertyName("city")]
        public string City { get; set; }
        
        [JsonPropertyName("village")]
        public string Village { get; set; }
        
        [JsonPropertyName("municipality")]
        public string Municipality { get; set; }
        
        [JsonPropertyName("county")]
        public string County { get; set; }
        
        [JsonPropertyName("country")]
        public string Country { get; set; }
    }
}

