using FacebookPanoPrepper.Services;

namespace FacebookPanoPrepper.Forms
{
    public class MetadataEditorForm : Form
    {
        private readonly WindowsPropertyService _propertyService;
        private DataGridView imageGrid;
        private Button saveButton;
        private Button cancelButton;
        private Label statusLabel;

        public MetadataEditorForm()
        {
            _propertyService = new WindowsPropertyService();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Image Metadata Editor";
            this.Size = new Size(800, 600);

            imageGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            imageGrid.Columns.AddRange(new DataGridViewColumn[]
            {
            new DataGridViewTextBoxColumn
            {
                Name = "FilePath",
                HeaderText = "File",
                ReadOnly = true
            },
            new DataGridViewTextBoxColumn
            {
                Name = "Title",
                HeaderText = "Title"
            },
            new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "Description"
            }
            });

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            saveButton = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Right
            };
            saveButton.Click += SaveButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Right
            };

            statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 20
            };

            buttonPanel.Controls.AddRange(new Control[] { cancelButton, saveButton });

            this.Controls.AddRange(new Control[]
            {
            imageGrid,
            buttonPanel,
            statusLabel
            });

            // Add drag-drop support
            this.AllowDrop = true;
            this.DragEnter += MetadataEditorForm_DragEnter;
            this.DragDrop += MetadataEditorForm_DragDrop;
        }

        private void MetadataEditorForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.All(f => Path.GetExtension(f).ToLower() is ".jpg" or ".jpeg"))
                {
                    e.Effect = DragDropEffects.Copy;
                }
            }
        }

        private void MetadataEditorForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddFiles(files);
        }

        private void AddFiles(string[] files)
        {
            foreach (var file in files)
            {
                try
                {
                    var properties = _propertyService.GetImageProperties(file);
                    imageGrid.Rows.Add(
                        Path.GetFileName(file),
                        properties.Title,
                        properties.Description
                    );
                    imageGrid.Rows[imageGrid.Rows.Count - 1].Tag = file; // Store full path
                }
                catch (Exception ex)
                {
                    statusLabel.Text = $"Error adding {Path.GetFileName(file)}: {ex.Message}";
                }
            }
        }

        private async void SaveButton_Click(object sender, EventArgs e)
        {
            saveButton.Enabled = false;
            statusLabel.Text = "Saving...";

            try
            {
                foreach (DataGridViewRow row in imageGrid.Rows)
                {
                    string filePath = (string)row.Tag;
                    string title = row.Cells["Title"].Value?.ToString() ?? "";
                    string description = row.Cells["Description"].Value?.ToString() ?? "";

                    await Task.Run(() => _propertyService.SetImageProperties(
                        filePath,
                        title,
                        description
                    ));
                }

                statusLabel.Text = "Save completed successfully.";
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving metadata: {ex.Message}",
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                statusLabel.Text = "Save failed.";
            }
            finally
            {
                saveButton.Enabled = true;
            }
        }
    }
}
