using Microsoft.Extensions.Logging;
using FacebookPanoPrepper.Services;
using FacebookPanoPrepper.Models;
using FacebookPanoPrepper.Helpers;
using FacebookPanoPrepper.Controls;

namespace FacebookPanoPrepper.Forms
{
    public class MainForm : Form
    {
        private readonly ImageProcessingService _processingService;
        private readonly ILogger<MainForm> _logger;
        private readonly ProcessingOptions _options;

        // Form controls
        private TableLayoutPanel tableLayoutPanel;
        private FancyDropPanel dropPanel;
        private Label dropLabel;
        private DarkProgressBar progressBar;
        private RichTextBox logTextBox;
        private MenuStrip _menuStrip;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripProgressBar _statusProgress;
        private CancellationTokenSource? _cancellationTokenSource;
        private ToolStripMenuItem _viewMenu;
        private ToolStripMenuItem _darkModeItem;

        public MainForm(ImageProcessingService processingService, ILogger<MainForm> logger, ProcessingOptions options)
        {
            try
            {
                _processingService = processingService;
                _logger = logger;
                _options = options;

                // Initialize the form
                InitializeFormControls();
                SetupDragDrop();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in MainForm constructor: {ex.Message}\nStack trace: {ex.StackTrace}",
                    "Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                throw;
            }
        }

        private void InitializeFormControls()
        {
            try
            {
                // form properties
                this.Text = $"Facebook Pano Prepper {AppVersion.FullVersion}";
                this.Size = new Size(800, 600);
                this.MinimumSize = new Size(600, 400);

                // Create and position controls
                tableLayoutPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    Padding = new Padding(10)
                };

                dropPanel = new FancyDropPanel
                {
                    Dock = DockStyle.Fill
                };

                // Just modify this part for the label
                dropLabel = new Label
                {
                    Text = "Drag and drop panorama images here",
                    TextAlign = ContentAlignment.MiddleCenter,  
                    Dock = DockStyle.Fill,                    
                    Font = new Font("Segoe UI", 14),          
                    BackColor = Color.Transparent
                };

                progressBar = new DarkProgressBar
                {
                    Dock = DockStyle.Fill,
                    Style = ProgressBarStyle.Continuous
                };

                logTextBox = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    BackColor = Color.White,
                    Font = new Font("Consolas", 9, FontStyle.Regular, GraphicsUnit.Point)
                };

                // Add controls to form
                dropPanel.Controls.Add(dropLabel);

                tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
                tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
                tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));

                tableLayoutPanel.Controls.Add(dropPanel, 0, 0);
                tableLayoutPanel.Controls.Add(progressBar, 0, 1);
                tableLayoutPanel.Controls.Add(logTextBox, 0, 2);

                this.Controls.Add(tableLayoutPanel);

                // Initialize Menu Strip
                InitializeMenuStrip();

                // Initialize Status Strip
                InitializeStatusStrip();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in InitializeFormControls: {ex.Message}\nStack trace: {ex.StackTrace}",
                    "Control Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                throw;
            }
        }

        private void InitializeMenuStrip()
        {
            _menuStrip = new MenuStrip();

            // File Menu
            var fileMenu = new ToolStripMenuItem("File");
            var settingsItem = new ToolStripMenuItem("Settings...");
            settingsItem.Click += SettingsItem_Click;
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => this.Close();

            fileMenu.DropDownItems.Add(settingsItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitItem);

            // View Menu
            _viewMenu = new ToolStripMenuItem("View");
            _darkModeItem = new ToolStripMenuItem("Dark Mode");
            _darkModeItem.CheckOnClick = true;
            _darkModeItem.Click += DarkModeItem_Click;
            _viewMenu.DropDownItems.Add(_darkModeItem);

            // Help Menu
            var helpMenu = new ToolStripMenuItem("Help");
            var aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (s, e) => ShowAboutDialog();
            helpMenu.DropDownItems.Add(aboutItem);

            _menuStrip.Items.Add(fileMenu);
            _menuStrip.Items.Add(_viewMenu);
            _menuStrip.Items.Add(helpMenu);

            this.Controls.Add(_menuStrip);
        }

        private void InitializeStatusStrip()
        {
            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("Ready");
            _statusProgress = new ToolStripProgressBar();
            _statusStrip.Items.Add(_statusLabel);
            _statusStrip.Items.Add(_statusProgress);
            this.Controls.Add(_statusStrip);
        }

        private void ShowAboutDialog()
        {
            MessageBox.Show(
                $"Facebook Pano Prepper {AppVersion.FullVersion}\n" +
                $"Built: {AppVersion.BuildDate:d}\n\n" +
                "Created by Ingve Moss Liknes\n" +
                "MIT License",
                "About Facebook Pano Prepper",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void DarkModeItem_Click(object sender, EventArgs e)
        {
            ThemeManager.ApplyTheme(this, _darkModeItem.Checked);
            this.Refresh();
        }

        private void SettingsItem_Click(object sender, EventArgs e)
        {
            using var settingsForm = new SettingsForm(_options);
            if (settingsForm.ShowDialog(this) == DialogResult.OK)
            {
                _statusLabel.Text = "Settings updated";
            }
        }

        private void SetupDragDrop()
        {
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;
        }

        private async void MainForm_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
            {
                await ProcessFilesAsync(files);
            }
        }

        private void MainForm_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files &&
                files.All(f => Path.GetExtension(f).ToLower() is ".jpg" or ".jpeg"))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private async Task ProcessFilesAsync(string[] files)
        {
            _statusProgress.Maximum = files.Length;
            _statusProgress.Value = 0;
            dropLabel.Text = "Processing...";
            logTextBox.Clear();

            var batchReport = new BatchProcessingReport
            {
                TotalFiles = files.Length,
                StartTime = DateTime.Now
            };

            try
            {
                string baseOutputDir = Path.GetFullPath(_options.OutputFolder);
                if (!Directory.Exists(baseOutputDir))
                {
                    Directory.CreateDirectory(baseOutputDir);
                }

                _cancellationTokenSource = new CancellationTokenSource();

                for (int i = 0; i < files.Length; i++)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    var file = files[i];
                    _statusLabel.Text = $"Processing {i + 1} of {files.Length}: {Path.GetFileName(file)}";

                    var outputPath = Path.Combine(
                        baseOutputDir,
                        "360_" + Path.GetFileName(file)
                    );

                    var progress = new Progress<int>(value =>
                    {
                        _statusProgress.Value = value;
                    });

                    var report = await _processingService.ProcessImageAsync(file, outputPath, progress);
                    batchReport.Reports.Add(report);
                    if (report.Success) batchReport.SuccessfulFiles++;

                    AppendColoredText(report.GetRichTextSummary());
                    _statusProgress.Value = i + 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _logger.LogError(ex, "Error processing files");
            }
            finally
            {
                batchReport.EndTime = DateTime.Now;
                batchReport.ProcessingTime = batchReport.EndTime - batchReport.StartTime;

                _statusLabel.Text = $"Completed: {batchReport.SuccessfulFiles} of {batchReport.TotalFiles} files processed successfully";
                dropLabel.Text = "Drag and drop panorama images here";
                _statusProgress.Value = 0;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                logTextBox.AppendText(Environment.NewLine + batchReport.GetSummary());
            }
        }

        private void AppendColoredText(string text)
        {
            int startIndex = logTextBox.TextLength;
            logTextBox.AppendText(text);

            string[] parts = text.Split(new[] { "|c", "|" }, StringSplitOptions.None);
            int currentIndex = startIndex;

            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 1 && int.TryParse(parts[i], out int argb))
                {
                    logTextBox.SelectionStart = currentIndex;
                    logTextBox.SelectionLength = parts[i + 1].Length;
                    logTextBox.SelectionColor = Color.FromArgb(argb);
                    i++; // Skip the next part as we've already processed it
                }
                currentIndex += parts[i].Length;
            }
        }
    }
}