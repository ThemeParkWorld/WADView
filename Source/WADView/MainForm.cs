using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WADView
{
    public partial class MainForm : Form
    {
        private Dictionary<string, int> duplicateFiles = new Dictionary<string, int>();
        private DWFBArchive currentArchive;
        public MainForm(string[] args)
        {
            InitializeComponent();
            if (args.Length > 0)
            {
                OpenArchive(args[0]);
            }
        }

        private void SetStatus(string status = "Ready")
        {
            statusLabel.Text = status;
            statusStrip1.Refresh();
        }

        private void SetProgress(int progress = -1)
        {
            if (progress < 0)
            {
                progressBar.Visible = false;
            }
            else
            {
                progressBar.Visible = true;
                progressBar.Value = progress;
            }
        }

        private void ExportFile(int index, string path)
        {
            var file = currentArchive.files[index];

            var filePath = path + @"\" + file.Name.Replace("\0", "");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            while (File.Exists(filePath))
            {
                if (!duplicateFiles.ContainsKey(filePath))
                    duplicateFiles.Add(filePath, 1);

                filePath = $"{path}/{Path.GetFileNameWithoutExtension(filePath)} ({duplicateFiles[filePath]++}){Path.GetExtension(filePath)}";
            }

            var streamWriter = new StreamWriter(filePath);
            using (var binaryWriter = new BinaryWriter(streamWriter.BaseStream))
            {
                binaryWriter.Write(file.Data);
                streamWriter.Close();
            }
        }

        private void OpenArchive(string path)
        {
            currentArchive = new DWFBArchive(path);

            SetStatus("Opening archive");

            mainListView.Items.Clear();
            foreach (ArchiveFile file in currentArchive.files)
            {
                mainListView.Items.Add(new ListViewItem(new[] { file.Name, file.DecompressedSize.ToString(), file.IsCompressed.ToString(), file.ArchiveOffset.ToString(), file.Unknown.ToString() }));
            }

            SetProgress();
            SetStatus();
        }

        private void SaveArchive(string path)
        {
            SetStatus("Saving archive");

            if (currentArchive != null)
                currentArchive.SaveArchive(path);

            SetProgress();
            SetStatus();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                OpenArchive(openFileDialog.FileName);
            }
        }

        private void ExportAllItem_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog folderBrowserDialog = new CommonOpenFileDialog();
            folderBrowserDialog.IsFolderPicker = true;
            if (folderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SetStatus("Exporting all files");

                for (int i = 0; i < currentArchive.files.Count; ++i)
                {
                    SetStatus($"Exporting all files ({i} / {currentArchive.files.Count})");
                    SetProgress((int)((i / (float)currentArchive.files.Count) * 100));
                    ExportFile(i, folderBrowserDialog.FileName);
                }

                SetProgress();
                SetStatus();
            }
        }

        private void ExitItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ExportFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                SetStatus("Exporting selected file(s)");

                if (mainListView.SelectedIndices.Count > 0)
                {
                    for (int i = 0; i < mainListView.SelectedIndices.Count; ++i)
                    {
                        SetStatus($"Exporting selected file(s) ({i} / {mainListView.SelectedIndices.Count})");
                        SetProgress((int)((i / (float)mainListView.SelectedIndices.Count) * 100));
                        int index = mainListView.SelectedIndices[i];
                        ExportFile(index, folderBrowserDialog.SelectedPath);
                    }
                }

                SetProgress();
                SetStatus();
            }
        }

        private void MainListView_DoubleClick(object sender, EventArgs e)
        {
            if (mainListView.SelectedIndices.Count > 0)
            {
                for (int i = 0; i < mainListView.SelectedIndices.Count; ++i)
                {
                    PreviewForm tmp = new PreviewForm(currentArchive.files[mainListView.SelectedIndices[i]]);
                    tmp.Show();
                }
            }
        }

        private static void SetAssociation(string extension, string keyName, string openWith, string fileDesc)
        {
            RegistryKey subKey;

            subKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\" + extension, true);
            subKey.DeleteSubKey("UserChoice", false);
            subKey.Close();

            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private void AssociateWithWADToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Fix
            SetAssociation(".wad", "Bullfrog_WAD", Application.ExecutablePath, "Bullfrog WAD file used in Theme Engine games & Dungeon Keeper 2");
            MessageBox.Show("Associated program with .wad");
        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveArchive(saveFileDialog.FileName);
            }
        }
    }
}
