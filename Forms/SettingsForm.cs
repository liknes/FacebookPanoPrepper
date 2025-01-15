using FacebookPanoPrepper.Models;

namespace FacebookPanoPrepper.Forms
{
    public class SettingsForm : Form
    {
        private readonly ProcessingOptions _options;
        private TableLayoutPanel _mainLayout;
        private NumericUpDown _qualityInput;
        private CheckBox _autoResizeCheck;
        private CheckBox _aspectRatioCheck;
        private TextBox _outputFolderPath;
        private Button _browseButton;
        private Button _saveButton;
        private Button _cancelButton;

        public SettingsForm(ProcessingOptions options)
        {
            _options = options;
            InitializeComponents();
            LoadSettings();
        }

        private void InitializeComponents()
        {
            this.Text = "Settings";
            this.Size = new Size(500, 300);
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
                Value = _options.JpegQuality
            };
            _mainLayout.Controls.Add(_qualityInput, 1, 0);
            _mainLayout.Controls.Add(new Label { Text = "%" }, 2, 0);

            // Auto-resize checkbox
            _autoResizeCheck = new CheckBox
            {
                Text = "Auto-resize large images",
                Checked = _options.AutoResize
            };
            _mainLayout.Controls.Add(_autoResizeCheck, 0, 1);

            // Aspect ratio checkbox
            _aspectRatioCheck = new CheckBox
            {
                Text = "Auto-correct aspect ratio",
                Checked = _options.AutoCorrectAspectRatio
            };
            _mainLayout.Controls.Add(_aspectRatioCheck, 0, 2);

            // Output folder
            _mainLayout.Controls.Add(new Label { Text = "Output Folder:" }, 0, 3);
            _outputFolderPath = new TextBox
            {
                Text = _options.OutputFolder,
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

            this.Controls.Add(_mainLayout);
            this.Controls.Add(buttonPanel);
        }

        private void LoadSettings()
        {
            _qualityInput.Value = _options.JpegQuality;
            _autoResizeCheck.Checked = _options.AutoResize;
            _aspectRatioCheck.Checked = _options.AutoCorrectAspectRatio;
            _outputFolderPath.Text = _options.OutputFolder;
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _outputFolderPath.Text = dialog.SelectedPath;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            _options.JpegQuality = (int)_qualityInput.Value;
            _options.AutoResize = _autoResizeCheck.Checked;
            _options.AutoCorrectAspectRatio = _aspectRatioCheck.Checked;
            _options.OutputFolder = _outputFolderPath.Text;

            SaveSettingsToFile();
            this.DialogResult = DialogResult.OK;
        }

        private void SaveSettingsToFile()
        {
            var settings = new
            {
                JpegQuality = _options.JpegQuality,
                AutoResize = _options.AutoResize,
                AutoCorrectAspectRatio = _options.AutoCorrectAspectRatio,
                OutputFolder = _options.OutputFolder
            };

            string json = System.Text.Json.JsonSerializer.Serialize(settings,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("settings.json", json);
        }
    }
}
