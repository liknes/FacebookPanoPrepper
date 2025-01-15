using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using FacebookPanoPrepper.Services;

namespace FacebookPanoPrepper.Forms
{
    public class MainForm : Form
    {
        private readonly ImageProcessingService _processingService;
        private readonly ILogger<MainForm> _logger;

        // Form controls
        private TableLayoutPanel tableLayoutPanel;
        private Panel dropPanel;
        private Label dropLabel;
        private ProgressBar progressBar;
        private RichTextBox logTextBox;

        public MainForm(ImageProcessingService processingService, ILogger<MainForm> logger)
        {
            _processingService = processingService;
            _logger = logger;

            // Initialize the form
            InitializeFormControls();
            SetupDragDrop();
        }

        private void InitializeFormControls()
        {
            // Set form properties
            this.Text = "Facebook Pano Prepper";
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

            dropPanel = new DashedBorderPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.WhiteSmoke
            };

            dropLabel = new Label
            {
                Text = "Drag and drop panorama images here",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, 14)
            };

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Style = ProgressBarStyle.Continuous
            };

            logTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Consolas", 9)
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
            progressBar.Maximum = files.Length;
            progressBar.Value = 0;
            dropLabel.Text = "Processing...";
            logTextBox.Clear();

            try
            {
                foreach (var file in files)
                {
                    var outputPath = Path.Combine(
                        Path.GetDirectoryName(file) ?? string.Empty,
                        "360_" + Path.GetFileName(file)
                    );

                    var progress = new Progress<int>(value =>
                    {
                        progressBar.Value = value;
                    });

                    var report = await _processingService.ProcessImageAsync(file, outputPath, progress);
                    logTextBox.AppendText(report.GetSummary() + Environment.NewLine);
                    progressBar.Value++;
                }

                MessageBox.Show("Processing complete!", "Success", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _logger.LogError(ex, "Error processing files");
            }
            finally
            {
                dropLabel.Text = "Drag and drop panorama images here";
                progressBar.Value = 0;
            }
        }
    }
}
