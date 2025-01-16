using FacebookPanoPrepper.Models;
using System.Runtime;

namespace FacebookPanoPrepper.Forms
{
    public class SettingsForm : Form
    {
        private readonly ProcessingOptions _options;
        private readonly Settings _settings;
        private TableLayoutPanel _mainLayout;
        private NumericUpDown _qualityInput;
        private CheckBox _autoResizeCheck;
        private CheckBox _aspectRatioCheck;
        private TextBox _outputFolderPath;
        private Button _browseButton;
        private Button _saveButton;
        private Button _cancelButton;
        private CheckBox _multiResCheckbox;
        private CheckBox _webServerCheckbox;
        private NumericUpDown _portInput;

        public SettingsForm(ProcessingOptions options, Settings settings)
        {
            _options = options;
            _settings = settings;
            InitializeComponents();
            LoadSettings();
        }

        private void InitializeComponents()
        {
            this.Text = "Settings";
            this.Size = new Size(500, 400); // Increased height to accommodate advanced features
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            _mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ColumnCount = 3,
                RowCount = 6
            };

            // Quality settings
            _mainLayout.Controls.Add(new Label { Text = "JPEG Quality:" }, 0, 0);
            _qualityInput = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 100,
                Value = _settings.JpegQuality
            };
            _mainLayout.Controls.Add(_qualityInput, 1, 0);
            _mainLayout.Controls.Add(new Label { Text = "%" }, 2, 0);

            // Auto-resize checkbox
            _autoResizeCheck = new CheckBox
            {
                Text = "Auto-resize large images",
                Checked = _settings.AutoResize
            };
            _mainLayout.Controls.Add(_autoResizeCheck, 0, 1);

            // Aspect ratio checkbox
            _aspectRatioCheck = new CheckBox
            {
                Text = "Auto-correct aspect ratio",
                Checked = _settings.AutoCorrectAspectRatio
            };
            _mainLayout.Controls.Add(_aspectRatioCheck, 0, 2);

            // Output folder
            _mainLayout.Controls.Add(new Label { Text = "Output Folder:" }, 0, 3);
            _outputFolderPath = new TextBox
            {
                Text = _settings.OutputFolder,
                Width = 250
            };
            _mainLayout.Controls.Add(_outputFolderPath, 1, 3);

            _browseButton = new Button
            {
                Text = "Browse...",
                Width = 80
            };
            _browseButton.Click += BrowseButton_Click;
            _mainLayout.Controls.Add(_browseButton, 2, 3);

            // Add a GroupBox for advanced features
            var advancedGroup = new GroupBox
            {
                Text = "Advanced Features",
                Dock = DockStyle.Bottom,
                Padding = new Padding(10),
                Height = 140
            };

            _multiResCheckbox = new CheckBox
            {
                Text = "Enable Multi-Resolution Support (Better performance for large panoramas)",
                Checked = _settings.EnableMultiResolution,
                AutoSize = true,
                Location = new Point(15, 25)
            };

            _webServerCheckbox = new CheckBox
            {
                Text = "Use Local Web Server (Requires app to stay running)",
                Checked = _settings.UseLocalWebServer,
                AutoSize = true,
                Location = new Point(15, 50),
                Enabled = _settings.EnableMultiResolution
            };

            var portLabel = new Label
            {
                Text = "Web Server Port:",
                AutoSize = true,
                Location = new Point(35, 75)
            };

            _portInput = new NumericUpDown
            {
                Minimum = 1024,
                Maximum = 65535,
                Value = _settings.WebServerPort,
                Location = new Point(140, 73),
                Width = 80,
                Enabled = _settings.EnableMultiResolution && _settings.UseLocalWebServer
            };

            var warningLabel = new Label
            {
                Text = "Note: These features are experimental and may require additional permissions.",
                ForeColor = Color.DarkRed,
                AutoSize = true,
                Location = new Point(15, 100)
            };

            // Wire up events
            _multiResCheckbox.CheckedChanged += (s, e) =>
            {
                _settings.EnableMultiResolution = _multiResCheckbox.Checked;
                _webServerCheckbox.Enabled = _multiResCheckbox.Checked;
                _portInput.Enabled = _multiResCheckbox.Checked && _webServerCheckbox.Checked;
            };

            _webServerCheckbox.CheckedChanged += (s, e) =>
            {
                _settings.UseLocalWebServer = _webServerCheckbox.Checked;
                _portInput.Enabled = _webServerCheckbox.Checked;
            };

            _portInput.ValueChanged += (s, e) =>
            {
                _settings.WebServerPort = (int)_portInput.Value;
            };

            // Add controls to advanced group
            advancedGroup.Controls.AddRange(new Control[]
            {
                _multiResCheckbox,
                _webServerCheckbox,
                portLabel,
                _portInput,
                warningLabel
            });

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Bottom,
                Height = 40
            };

            _saveButton = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK
            };
            _saveButton.Click += SaveButton_Click;

            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel
            };

            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_saveButton);

            // Add all components to form
            this.Controls.Add(_mainLayout);
            this.Controls.Add(advancedGroup);
            this.Controls.Add(buttonPanel);
        }

        private void LoadSettings()
        {
            _qualityInput.Value = _settings.JpegQuality;
            _autoResizeCheck.Checked = _settings.AutoResize;
            _aspectRatioCheck.Checked = _settings.AutoCorrectAspectRatio;
            _outputFolderPath.Text = _settings.OutputFolder;
            _multiResCheckbox.Checked = _settings.EnableMultiResolution;
            _webServerCheckbox.Checked = _settings.UseLocalWebServer;
            _portInput.Value = _settings.WebServerPort;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Save to settings object
            _settings.JpegQuality = (int)_qualityInput.Value;
            _settings.AutoResize = _autoResizeCheck.Checked;
            _settings.AutoCorrectAspectRatio = _aspectRatioCheck.Checked;
            _settings.OutputFolder = _outputFolderPath.Text;
            _settings.EnableMultiResolution = _multiResCheckbox.Checked;
            _settings.UseLocalWebServer = _webServerCheckbox.Checked;
            _settings.WebServerPort = (int)_portInput.Value;

            // Update options properties
            _options.JpegQuality = _settings.JpegQuality;
            _options.AutoResize = _settings.AutoResize;
            _options.AutoCorrectAspectRatio = _settings.AutoCorrectAspectRatio;
            _options.OutputFolder = _settings.OutputFolder;
            _options.EnableMultiResolution = _settings.EnableMultiResolution;
            _options.UseLocalWebServer = _settings.UseLocalWebServer;
            _options.WebServerPort = _settings.WebServerPort;

            this.DialogResult = DialogResult.OK;
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _outputFolderPath.Text = dialog.SelectedPath;
            }
        }
    }
}