using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebpToJpg
{
    internal sealed class MainForm : Form
    {
        private readonly ListView files = new ListView();
        private readonly HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly NumericUpDown quality = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 90, Width = 65 };
        private readonly TextBox destination = new TextBox { ReadOnly = true, Dock = DockStyle.Fill };
        private readonly Button start = MakeButton("Convert");
        private readonly Button clear = MakeButton("Clear list");
        private readonly Button add = MakeButton("Add WebP files…");
        private readonly Button browse = MakeButton("Browse…");
        private readonly Button reset = MakeButton("Reset");
        private readonly Label status = new Label { AutoSize = true, Anchor = AnchorStyles.Left };
        private readonly ProgressBar progress = new ProgressBar { Dock = DockStyle.Fill, Height = 8 };
        private string outputFolder;
        private bool busy;

        public MainForm()
        {
            Text = "WebP → JPG";
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(790, 500);
            MinimumSize = new Size(690, 465);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            AllowDrop = true;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 8
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < layout.RowCount; i++)
                layout.RowStyles.Add(new RowStyle(i == 2 ? SizeType.Percent : SizeType.AutoSize, i == 2 ? 100 : 0));
            Controls.Add(layout);
            layout.Controls.Add(new Label { Text = "WebP → JPG", AutoSize = true,
                Font = new Font(Font.FontFamily, 21, FontStyle.Bold) }, 0, 0);
            var toolbar = Flow();
            toolbar.Controls.AddRange(new Control[] { add, clear });
            layout.Controls.Add(toolbar, 0, 1);

            files.View = View.Details;
            files.Dock = DockStyle.Fill;
            files.FullRowSelect = true;
            files.ShowItemToolTips = true;
            files.Columns.Add("Source file", 245);
            files.Columns.Add("Result");
            files.Resize += delegate { files.Columns[1].Width = Math.Max(180, files.ClientSize.Width - files.Columns[0].Width - 22); };
            layout.Controls.Add(files, 0, 2);

            layout.Controls.Add(new Label { Text = "Output folder", AutoSize = true, Margin = new Padding(3, 10, 3, 0) }, 0, 3);
            var output = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3 };
            output.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            output.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            output.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            destination.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            output.Controls.Add(destination, 0, 0);
            output.Controls.Add(browse, 1, 0);
            output.Controls.Add(reset, 2, 0);
            layout.Controls.Add(output, 0, 4);

            var settings = Flow();
            settings.Controls.Add(new Label { Text = "JPEG quality (%)", AutoSize = true, Anchor = AnchorStyles.Left });
            settings.Controls.Add(quality);
            layout.Controls.Add(settings, 0, 5);
            layout.Controls.Add(progress, 0, 6);
            var footer = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Top };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            start.BackColor = Color.FromArgb(29, 87, 151);
            start.ForeColor = Color.White;
            start.FlatStyle = FlatStyle.Flat;
            footer.Controls.Add(status, 0, 0);
            footer.Controls.Add(start, 1, 0);
            layout.Controls.Add(footer, 0, 7);

            add.Click += delegate
            {
                using (var picker = new OpenFileDialog { Title = "Select WebP files", Filter = "WebP images (*.webp)|*.webp", Multiselect = true })
                    if (picker.ShowDialog(this) == DialogResult.OK) AddFiles(picker.FileNames);
            };
            DragEnter += (s, e) => e.Effect = !busy && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            DragDrop += (s, e) => { if (!busy && e.Data.GetDataPresent(DataFormats.FileDrop)) AddFiles((string[])e.Data.GetData(DataFormats.FileDrop)); };
            clear.Click += delegate { paths.Clear(); files.Items.Clear(); progress.Value = 0; RefreshControls(); };
            browse.Click += delegate
            {
                using (var picker = new FolderBrowserDialog { Description = "Select an output folder for JPG files", SelectedPath = outputFolder ?? "" })
                    if (picker.ShowDialog(this) == DialogResult.OK) { outputFolder = picker.SelectedPath; RefreshControls(); }
            };
            reset.Click += delegate { outputFolder = null; RefreshControls(); };
            start.Click += async delegate { await RunBatch(); };
            FormClosing += (s, e) => { if (busy && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; status.Text = "Please wait for the conversion to finish."; } };
            RefreshControls();
        }

        private static FlowLayoutPanel Flow()
        {
            return new FlowLayoutPanel { AutoSize = true, WrapContents = false, Dock = DockStyle.Top };
        }

        private static Button MakeButton(string caption)
        {
            return new Button { Text = caption, AutoSize = true, Padding = new Padding(7, 2, 7, 2), UseVisualStyleBackColor = true };
        }

        private void AddFiles(IEnumerable<string> candidates)
        {
            foreach (var file in candidates.Where(p => string.Equals(Path.GetExtension(p), ".webp", StringComparison.OrdinalIgnoreCase) && File.Exists(p)))
            {
                if (!paths.Add(file)) continue;
                var item = new ListViewItem(Path.GetFileName(file)) { Tag = file, ToolTipText = file };
                item.SubItems.Add("Queued");
                files.Items.Add(item);
            }
            RefreshControls();
        }

        private void RefreshControls()
        {
            bool hasFiles = files.Items.Count > 0;
            add.Enabled = browse.Enabled = quality.Enabled = !busy;
            start.Enabled = clear.Enabled = !busy && hasFiles;
            reset.Enabled = !busy && outputFolder != null;
            destination.Text = outputFolder ?? "Same folder as each source file";
            status.Text = hasFiles ? "Files in list: " + files.Items.Count : "Add WebP files or drag them here.";
        }

        private async Task RunBatch()
        {
            busy = true;
            RefreshControls();
            progress.Value = 0;
            progress.Maximum = files.Items.Count;
            int errors = 0;
            int selectedQuality = (int)quality.Value;
            try
            {
                foreach (ListViewItem item in files.Items)
                {
                    string source = (string)item.Tag;
                    string folder = outputFolder ?? Path.GetDirectoryName(source);
                    status.Text = string.Format("Converting file {0} of {1}", progress.Value + 1, progress.Maximum);
                    item.SubItems[1].Text = "Converting…";
                    item.EnsureVisible();
                    try
                    {
                        string result = await Task.Run(() => JpegConverter.ConvertFile(source, folder, selectedQuality));
                        item.SubItems[1].Text = Path.GetFileName(result);
                        item.ToolTipText = result;
                        item.ForeColor = Color.DarkGreen;
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        string message = DescribeError(ex);
                        item.SubItems[1].Text = "Error: " + message;
                        item.ToolTipText = message;
                        item.ForeColor = Color.Firebrick;
                    }
                    progress.Value++;
                }
            }
            finally
            {
                busy = false;
                RefreshControls();
                status.Text = string.Format("Saved: {0}   Errors: {1}", progress.Value - errors, errors);
            }
        }

        private static string DescribeError(Exception error)
        {
            if (error is InvalidDataException) return error.Message;
            if (error is FileNotFoundException) return "The source file could not be found.";
            if (error is DirectoryNotFoundException) return "The source or output folder could not be found.";
            if (error is UnauthorizedAccessException) return "Check read access to the source and write access to the output folder.";
            if (error is FormatException || error is NotSupportedException) return "Windows could not decode this image.";
            if (error is OutOfMemoryException || error is OverflowException) return "The image is too large to convert.";
            if (error is IOException) return string.Format("Could not read or write the file (0x{0:X8}).", error.HResult);
            return string.Format("Conversion failed (0x{0:X8}).", error.HResult);
        }
    }
}
