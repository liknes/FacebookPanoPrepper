using Microsoft.Extensions.Logging;
using FacebookPanoPrepper.Services;
using FacebookPanoPrepper.Models;
using FacebookPanoPrepper.Helpers;
using FacebookPanoPrepper.Controls;
using System.Diagnostics;
using System.Text.Json;

namespace FacebookPanoPrepper.Forms
{
    public class MainForm : Form
    {
        private readonly ImageProcessingService _processingService;
        private readonly Settings _settings;
        private readonly ILogger<MainForm> _logger;
        private ProcessingOptions _options;
        private LocalWebServer _webServer;

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

        public MainForm(ImageProcessingService processingService, ILogger<MainForm> logger, ProcessingOptions options, Settings settings)
        {
            try
            {
                _processingService = processingService;
                _logger = logger;
                _options = options;
                _settings = settings;

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
            _settings = settings;
        }

        private void InitializeFormControls()
        {
            try
            {
                // form properties
                this.Text = $"Facebook Pano Prepper {AppVersion.FullVersion}";
                this.Size = new Size(800, 600);
                this.MinimumSize = new Size(600, 400);

                // Initialize Menu Strip first
                InitializeMenuStrip();

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
                    BackColor = ThemeManager.GetCurrentScheme().Background,
                    Font = new Font("Consolas", 9, FontStyle.Regular, GraphicsUnit.Point),
                    TabStop = false  // Prevent tab focus
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

                // Initialize Status Strip
                InitializeStatusStrip();

                // Now apply the theme colors
                if (ThemeManager.IsDarkMode)
                {
                    var scheme = ThemeManager.GetCurrentScheme();
                    logTextBox.BackColor = scheme.Background;
                    _statusStrip.BackColor = scheme.StatusStripBackground;
                    _statusProgress.BackColor = scheme.ScrollBarBackground;
                    _statusProgress.ForeColor = scheme.StatusProgressBar;
                }
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

        private void SettingsMenuItem_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm(_options, _settings))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    SaveSettings();
                }
            }
        }

        //private void SaveSettings()
        //{
        //    var json = JsonSerializer.Serialize(_settings);
        //    File.WriteAllText("settings.json", json);
        //}

        //private void InitializeMenuStrip()
        //{
        //    _menuStrip = new MenuStrip();

        //    // File Menu
        //    var fileMenu = new ToolStripMenuItem("File");
        //    fileMenu.DropDownOpening += MenuStrip_ItemClicked;
        //    var settingsItem = new ToolStripMenuItem("Settings...");
        //    settingsItem.Click += SettingsItem_Click;
        //    var exitItem = new ToolStripMenuItem("Exit");
        //    exitItem.Click += (s, e) => this.Close();

        //    fileMenu.DropDownItems.Add(settingsItem);
        //    fileMenu.DropDownItems.Add(new ToolStripSeparator());
        //    fileMenu.DropDownItems.Add(exitItem);

        //    // View Menu
        //    _viewMenu = new ToolStripMenuItem("View");
        //    _viewMenu.DropDownOpening += MenuStrip_ItemClicked;
        //    _darkModeItem = new ToolStripMenuItem("Dark Mode")
        //    {
        //        CheckOnClick = true,
        //        BackColor = ThemeManager.GetCurrentScheme().Background,
        //        ForeColor = ThemeManager.GetCurrentScheme().Text
        //    };
        //    _darkModeItem.Click += DarkModeItem_Click;
        //    _viewMenu.DropDownItems.Add(_darkModeItem);

        //    // Help Menu
        //    var helpMenu = new ToolStripMenuItem("Help");
        //    helpMenu.DropDownOpening += MenuStrip_ItemClicked;
        //    var aboutItem = new ToolStripMenuItem("About");
        //    aboutItem.Click += (s, e) => ShowAboutDialog();
        //    helpMenu.DropDownItems.Add(aboutItem);

        //    _menuStrip.Items.Add(fileMenu);
        //    _menuStrip.Items.Add(_viewMenu);
        //    _menuStrip.Items.Add(helpMenu);

        //    _menuStrip.BackColor = ThemeManager.GetCurrentScheme().Background;
        //    _menuStrip.ForeColor = ThemeManager.GetCurrentScheme().Text;

        //    this.Controls.Add(_menuStrip);
        //}

        private void InitializeStatusStrip()
        {
            try
            {
                _statusStrip = new StatusStrip
                {
                    SizingGrip = false,
                    BackColor = ThemeManager.GetCurrentScheme().StatusStripBackground
                };

                _statusLabel = new ToolStripStatusLabel
                {
                    Spring = true,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Text = "Ready"
                };

                _statusProgress = new ToolStripProgressBar  // Changed from DarkProgressBar
                {
                    Width = 100,
                    Visible = false
                };

                _statusStrip.Items.AddRange(new ToolStripItem[]
                {
            _statusLabel,
            _statusProgress
                });

                this.Controls.Add(_statusStrip);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in InitializeStatusStrip: {ex.Message}\nStack trace: {ex.StackTrace}",
                    "Status Strip Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                throw;
            }
        }

        private void MenuStrip_ItemClicked(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                // Force focus to the menu item
                menuItem.Select();
            }
        }

        private void InitializeMenuStrip()
        {
            try
            {
                var menuStrip = new MenuStrip();

                // File Menu
                var fileMenu = new ToolStripMenuItem("File");
                var settingsItem = new ToolStripMenuItem("Settings...");
                var exitItem = new ToolStripMenuItem("Exit");

                settingsItem.Click += (s, e) =>
                {
                    using (var settingsForm = new SettingsForm(_options, _settings))
                    {
                        if (settingsForm.ShowDialog() == DialogResult.OK)
                        {
                            SaveSettings();
                        }
                    }
                };

                exitItem.Click += (s, e) => Close();

                fileMenu.DropDownItems.AddRange(new ToolStripItem[]
                {
            settingsItem,
            new ToolStripSeparator(),
            exitItem
                });

                // Help Menu
                var helpMenu = new ToolStripMenuItem("Help");
                var aboutItem = new ToolStripMenuItem("About...");
                aboutItem.Click += (s, e) => ShowAboutDialog();

                helpMenu.DropDownItems.Add(aboutItem);

                // Add menus to strip
                menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, helpMenu });

                // Add menu strip to form
                this.Controls.Add(menuStrip);
                this.MainMenuStrip = menuStrip;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in InitializeMenuStrip: {ex.Message}\nStack trace: {ex.StackTrace}",
                    "Menu Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                throw;
            }
        }

        private void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings);
                File.WriteAllText("settings.json", json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
                MessageBox.Show("Error saving settings: " + ex.Message, "Settings Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAboutDialog()
        {
            MessageBox.Show(
                $"Facebook Pano Prepper {AppVersion.FullVersion}\n\n" +
                "Created by Ingve Moss Liknes\n" +
                "© 2024 All rights reserved.",
                "About Facebook Pano Prepper",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void DarkModeItem_Click(object sender, EventArgs e)
        {
            ThemeManager.ApplyTheme(this, _darkModeItem.Checked);

            // Force update the colors
            var scheme = ThemeManager.GetCurrentScheme();
            logTextBox.BackColor = scheme.Background;
            _statusStrip.BackColor = scheme.StatusStripBackground;
            _statusProgress.BackColor = scheme.ScrollBarBackground;
            _statusProgress.ForeColor = scheme.StatusProgressBar;

            UpdateLogTextColors();
            this.Refresh();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm(_options, _settings))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    _options = new ProcessingOptions(_settings);
                    SaveSettings();
                }
            }
        }

        //private void SaveSettings()
        //{
        //    var settings = new Settings
        //    {
        //        OutputFolder = _options.OutputFolder,
        //        EnableMultiResolution = _options.EnableMultiResolution,
        //        UseLocalWebServer = _options.UseLocalWebServer,
        //        WebServerPort = _options.WebServerPort
        //    };

        //    var json = JsonSerializer.Serialize(settings);
        //    File.WriteAllText("settings.json", json);
        //}

        private void LoadSettings()
        {
            if (File.Exists("settings.json"))
            {
                try
                {
                    var json = File.ReadAllText("settings.json");
                    var settings = JsonSerializer.Deserialize<Settings>(json);
                    _options = new ProcessingOptions(settings);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading settings");
                    _options = new ProcessingOptions(new Settings());
                }
            }
            else
            {
                _options = new ProcessingOptions(new Settings());
            }
        }

        //private void SettingsItem_Click(object sender, EventArgs e)
        //{
        //    using var settingsForm = new SettingsForm(_options);
        //    if (settingsForm.ShowDialog(this) == DialogResult.OK)
        //    {
        //        _statusLabel.Text = "Settings updated";
        //    }
        //}

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
            var processedFiles = new List<(string FilePath, PanoramaResolutions Resolutions)>();
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
                // Create a timestamped folder for this batch
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string baseOutputDir = Path.GetFullPath(_options.OutputFolder);
                string batchDir = Path.Combine(baseOutputDir, $"Batch_{timestamp}");
                string imagesDir = Path.Combine(batchDir, "images");
                string resolutionsDir = Path.Combine(batchDir, "resolutions");

                // Create directories
                Directory.CreateDirectory(batchDir);
                Directory.CreateDirectory(imagesDir);
                Directory.CreateDirectory(resolutionsDir);

                // Initialize web server if enabled
                if (_options.EnableMultiResolution && _options.UseLocalWebServer)
                {
                    try
                    {
                        _webServer?.Dispose();
                        _webServer = new LocalWebServer(batchDir, _options.WebServerPort);
                        await _webServer.StartAsync();
                        AppendColoredText("Local web server started successfully.\n");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "Failed to start web server. Falling back to standard mode.\n\n" +
                            "Error: " + ex.Message,
                            "Web Server Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        _options.UseLocalWebServer = false;
                    }
                }

                _cancellationTokenSource = new CancellationTokenSource();

                for (int i = 0; i < files.Length; i++)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    var file = files[i];
                    _statusLabel.Text = $"Processing {i + 1} of {files.Length}: {Path.GetFileName(file)}";

                    var outputPath = Path.Combine(
                        imagesDir,
                        "360_" + Path.GetFileName(file)
                    );

                    var progress = new Progress<int>(value =>
                    {
                        _statusProgress.Value = value;
                    });

                    var report = await _processingService.ProcessImageAsync(file, outputPath, progress);
                    batchReport.Reports.Add(report);

                    if (report.Success)
                    {
                        batchReport.SuccessfulFiles++;

                        if (_options.EnableMultiResolution)
                        {
                            if (_options.UseLocalWebServer)
                            {
                                // Create multi-resolution tiles for web server
                                var multiRes = await _processingService.CreateMultiResolutionTiles(
                                    outputPath,
                                    Path.Combine(resolutionsDir, Path.GetFileNameWithoutExtension(outputPath) + "_tiles"));

                                processedFiles.Add((outputPath, new PanoramaResolutions
                                {
                                    FullResPath = outputPath,
                                    Width = multiRes?.Width ?? 0,
                                    Height = multiRes?.Height ?? 0
                                }));

                                if (multiRes != null)
                                {
                                    AppendColoredText($"\nCreated multi-resolution tiles for {Path.GetFileName(file)} ({multiRes.Width}x{multiRes.Height})\n");
                                }
                            }
                            else
                            {
                                // Create progressive resolutions for base64 encoding
                                var resolutions = await _processingService.CreateProgressiveResolutions(
                                    outputPath,
                                    resolutionsDir);

                                processedFiles.Add((outputPath, resolutions));

                                if (resolutions.MediumResPath != null)
                                {
                                    AppendColoredText($"\nCreated progressive resolutions for {Path.GetFileName(file)} ({resolutions.Width}x{resolutions.Height})\n");
                                }
                            }
                        }
                        else
                        {
                            // Standard mode - just use the original processed file
                            processedFiles.Add((outputPath, new PanoramaResolutions
                            {
                                FullResPath = outputPath,
                                Width = Image.FromFile(outputPath).Width,
                                Height = Image.FromFile(outputPath).Height
                            }));
                        }
                    }

                    AppendColoredText(report.GetRichTextSummary());
                    _statusProgress.Value = i + 1;
                }

                // Create viewer in the batch directory
                if (processedFiles.Any())
                {
                    var viewerPath = Path.Combine(batchDir, "viewer.html");
                    //var htmlService = new HtmlViewerService(_options.UseLocalWebServer ? _webServer?.BaseUrl : null);
                    var htmlService = new HtmlViewerService();
                    htmlService.CreateHtmlViewer(viewerPath, processedFiles);

                    // Save processing report
                    var reportPath = Path.Combine(batchDir, "processing_report.txt");
                    File.WriteAllText(reportPath, batchReport.GetSummary());

                    var message = $"Files processed and saved to:\n{batchDir}\n\nWould you like to open the viewer?";
                    if (_options.UseLocalWebServer)
                    {
                        message += "\n\nNote: The viewer will work as long as this application is running.";
                    }

                    if (MessageBox.Show(
                        message,
                        "Processing Complete",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = viewerPath,
                            UseShellExecute = true
                        });
                    }
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

                AppendColoredText(Environment.NewLine + batchReport.GetSummary());
            }
        }

        private void AppendColoredText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            int startIndex = logTextBox.TextLength;
            logTextBox.AppendText(text);

            // First, set the default color for the entire text
            logTextBox.SelectionStart = startIndex;
            logTextBox.SelectionLength = text.Length;
            logTextBox.SelectionColor = ThemeManager.GetTextColor();

            // Then process any color codes
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

            // Reset selection
            logTextBox.SelectionStart = logTextBox.TextLength;
            logTextBox.SelectionLength = 0;
        }

        private void UpdateLogTextColors()
        {
            if (logTextBox.TextLength == 0) return;

            // Store the current text and clear the box
            string currentText = logTextBox.Text;
            logTextBox.Clear();

            // Set the default text color based on the current theme
            logTextBox.SelectionColor = ThemeManager.GetTextColor();

            // Reapply the text with proper colors
            AppendColoredText(currentText);

            // Ensure the text box is using the correct background color
            logTextBox.BackColor = ThemeManager.GetCurrentScheme().Background;
        }
    }
}