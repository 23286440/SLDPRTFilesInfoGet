using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SldWorks;
using SwConst;
using System.Threading;
using System.Diagnostics;

namespace sw文件信息提取
{
    public partial class Form1 : Form
    {
        #region 折叠代码
        public class ArchiveExtractor : IDisposable
        {
            public string archiveFilePath { get; }
            public bool isSupported { get; }
            public bool readerFactorySupported { get; }
            public bool isArchiveProtected { get; }
            public string archivePassWord { set; get; }
            public ArchiveType archiveType { get; }
            public IEnumerable<IArchiveEntry> archiveEntries { get; }
            private FileStream archiveFileStream;

            public ArchiveExtractor(string archiveFilePath)
            {
                this.archiveFilePath = archiveFilePath;
                this.archivePassWord = null;
                archiveFileStream = File.OpenRead(archiveFilePath);
                readerFactorySupported = IsReaderFactorySupported();
                try
                {
                    archiveType = DetectArchiveType();
                    isSupported = true;
                    archiveEntries = GetArchiveEntries();
                    isArchiveProtected = IsArchiveProtected();
                }
                catch (System.InvalidOperationException)
                {
                    isSupported = false;
                }
                finally { ResetStream(archiveFileStream); }
            }

            private bool IsReaderFactorySupported()
            {
                try
                {
                    var reader = SharpCompress.Readers.ReaderFactory.Open(archiveFileStream);
                    return reader != null;
                }
                catch (System.InvalidOperationException)
                {
                    return false;
                }
                finally { ResetStream(archiveFileStream); }
            }

            private ArchiveType DetectArchiveType()
            {
                try
                {
                    using (var archive = ArchiveFactory.Open(archiveFilePath))
                    {
                        return archive.Type;
                    }
                }
                finally { ResetStream(archiveFileStream); }
            }

            private IEnumerable<IArchiveEntry> GetArchiveEntries()
            {
                try
                {
                    using (var archive = ArchiveFactory.Open(archiveFileStream))
                    {
                        return archive.Entries.ToList();
                    }
                }
                finally { ResetStream(archiveFileStream); }
            }

            private bool IsArchiveProtected()
            {
                try
                {
                    _ = archiveEntries.FirstOrDefault();
                }
                catch (SharpCompress.Common.CryptographicException)
                {
                    return true;
                }
                finally { ResetStream(archiveFileStream); }
                return false;
            }

            public bool IsEntriesProtected()
            {
                try
                {
                    var options = new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true,
                    };

                    using (IArchive archive = ArchiveFactory.Open(archiveFileStream, new SharpCompress.Readers.ReaderOptions { Password = this.archivePassWord }))
                    {
                        return archive.Entries.Any(entry => entry.IsEncrypted);
                    }
                }
                finally { ResetStream(archiveFileStream); }
            }

            public string ExtractArchive(string outputDirectory = null, string password = null, List<string> filesToExtract = null)
            {
                try
                {
                    if (String.IsNullOrEmpty(outputDirectory))
                    {
                        outputDirectory = Path.Combine(Path.GetDirectoryName(archiveFilePath), Path.GetFileNameWithoutExtension(archiveFilePath));
                    }

                    if (File.Exists(outputDirectory))
                    {
                        outputDirectory = Path.Combine(Path.GetDirectoryName(archiveFilePath), Path.GetFileNameWithoutExtension($"{archiveFilePath}_unzip"));
                    }

                    Directory.CreateDirectory(outputDirectory);

                    var options = new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true,
                    };

                    using (IArchive archive = ArchiveFactory.Open(archiveFileStream, new SharpCompress.Readers.ReaderOptions { Password = password }))
                    {
                        if (filesToExtract == null)
                        {
                            archive.WriteToDirectory(outputDirectory, options);
                        }
                        else
                        {
                            foreach (var entry in archive.Entries)
                            {
                                if (!entry.IsDirectory && filesToExtract.Contains(entry.Key))
                                {
                                    entry.WriteToDirectory(outputDirectory);
                                }
                            }
                        }
                    }
                }
                finally { ResetStream(archiveFileStream); }
                return outputDirectory;
            }

            private void ResetStream(FileStream fileStream)
            {
                if (fileStream != null)
                {
                    fileStream.Position = 0;
                }
            }

            public void Dispose()
            {
                if (archiveFileStream != null)
                {
                    archiveFileStream.Close();
                    archiveFileStream.Dispose();
                    archiveFileStream = null;
                }
            }
        }
        class SLDFileSummaryInfo
        {
            public string FileName { get; set; }
            public string Title { get; set; }
            public string Subject { get; set; }
            public string Author { get; set; }
            public string Keywords { get; set; }
            public string Comment { get; set; }
            public string SavedBy { get; set; }
            public string DateCreated { get; set; }
            public string DateSaved { get; set; }
            public string DateCreated2 { get; set; }
            public string DateSaved2 { get; set; }
        }

        #endregion

        string sldFilesPath = null;
        public Form1()
        {
            sldFilesPath = Path.Combine(System.Environment.CurrentDirectory, "sldFiles");

            if (Directory.Exists(sldFilesPath))
            {
                Directory.GetDirectories(sldFilesPath).ToList().ForEach(d => new DirectoryInfo(d).Attributes = FileAttributes.Normal);
                Directory.GetFiles(sldFilesPath, "*.*", SearchOption.AllDirectories).ToList().ForEach(f => new FileInfo(f).Attributes = FileAttributes.Normal);

                try
                {
                    Directory.Delete(sldFilesPath, true);
                }
                catch (DirectoryNotFoundException) { }
                catch (IOException ex)
                {
                    MessageBox.Show(ex.Message, "清空临时文件时出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            Directory.CreateDirectory(sldFilesPath);

            InitializeComponent();
            dataGridView1.DataSource = new List<SLDFileSummaryInfo>();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            this.Cursor = Cursors.WaitCursor;
            backgroundWorker_AddFiles.RunWorkerAsync(openFileDialog1.FileNames);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            this.Cursor = Cursors.WaitCursor;
            backgroundWorker_AddFiles.RunWorkerAsync(Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.*", SearchOption.AllDirectories));
        }

        private void backgroundWorker_AddFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            if (e.Argument is string[] files)
            {
                CheckedListBox tempListBox = new CheckedListBox();
                AddFiles(files, tempListBox);
                e.Result = tempListBox;
            }
            else
            {
                throw new ArgumentException($"传入了非预期数据类型{e.Argument.GetType()}");
            }
        }

        private void backgroundWorker_AddFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result is CheckedListBox tempListBox)
            {
                foreach (var item in tempListBox.Items)
                {
                    checkedListBox1.Items.Add(item, true);
                }
            }
            else
            {
                throw new ArgumentException($"数据类型非预期数据类型：{e.Result.GetType()}");
            }
            label_Count.Text = $"共计:{checkedListBox1.Items.Count}项";
            this.Cursor = Cursors.Default;
        }

        private void AddFiles(string[] files, CheckedListBox tempListBox)
        {
            foreach (string file in files)
            {
                if (Path.GetExtension(file).Equals(".SLDPRT", StringComparison.OrdinalIgnoreCase))
                {
                    int itemIndex = tempListBox.Items.Add(file);
                    tempListBox.SetItemChecked(itemIndex, true);
                }
                else
                {
                    try
                    {
                        ExtractAndAddFiles(file, tempListBox);
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show(ex.Message, "解压文件时出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExtractAndAddFiles(string file, CheckedListBox tempListBox)
        {
            using (ArchiveExtractor archiveExtractor = new ArchiveExtractor(file))
            {
                if (!archiveExtractor.isSupported)
                {
                    return;
                }
                string archiveExtractorTempPath = Path.Combine(sldFilesPath, Path.GetFileNameWithoutExtension(file));
                archiveExtractorTempPath = archiveExtractor.ExtractArchive(archiveExtractorTempPath);
                AddFiles(Directory.GetFiles(archiveExtractorTempPath, "*.*", SearchOption.AllDirectories), tempListBox);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.CheckedItems.Count == 0)
            {
                MessageBox.Show("请至少选择一个文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            this.Cursor = Cursors.WaitCursor;
            label_ProgressBarInfo.Text = "正在打开SolidWorks";

            ListBox tempListBox = new CheckedListBox();
            foreach (var item in checkedListBox1.Items)
            {
                tempListBox.Items.Add(item);
            }
            backgroundWorker_Analyse.RunWorkerAsync(tempListBox);
            panel1.Visible = true;
            button10.Enabled = true;
        }
        private void button10_Click(object sender, EventArgs e)
        {
            backgroundWorker_Analyse.CancelAsync();
            button10.Enabled = false;
        }
        private void backgroundWorker_Analyse_DoWork(object sender, DoWorkEventArgs e)
        {
            List<SLDFileSummaryInfo> sldFileSummaryInfos = new List<SLDFileSummaryInfo>();
            if (e.Argument is ListBox items)
            {
                for (int i = 0; i < items.Items.Count; i++)
                {
                    if (backgroundWorker_Analyse.CancellationPending)
                    {
                        backgroundWorker_Analyse.ReportProgress(progressBar1.Maximum, "已取消");
                        e.Result = sldFileSummaryInfos;
                        return;
                    }
                    string file = items.Items[i].ToString();
                    SLDFileSummaryInfo sldFileSummaryInfo = GetFileSummaryInfo(file);
                    sldFileSummaryInfos.Add(sldFileSummaryInfo);
                    backgroundWorker_Analyse.ReportProgress((i + 1) * 100 / items.Items.Count, $"已完成：{file}");
                }
                backgroundWorker_Analyse.ReportProgress(progressBar1.Maximum, "已完成所有文件");
                e.Result = sldFileSummaryInfos;
            }
            else
            {
                throw new ArgumentException($"传入了非预期数据类型{e.Argument.GetType()}");
            }
        }

        private void backgroundWorker_Analyse_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            if (e.UserState is string file)
            {
                label_ProgressBarInfo.Text = file;
            }
            else
            {
                throw new ArgumentException($"数据类型非预期数据类型：{e.UserState.GetType()}");
            }
        }
        private void backgroundWorker_Analyse_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button10.Enabled = false;
            this.Cursor = Cursors.Default;

            if (e.Result is List<SLDFileSummaryInfo> sldFileSummaryInfos)
            {
                dataGridView1.DataSource = sldFileSummaryInfos;
            }
            else
            {
                throw new ArgumentException($"数据类型非预期数据类型：{e.Result.GetType()}");
            }

            tabControl1.SelectedTab = tabPage2;
            button4_Click(sender, e);
            panel1.Visible = false;
        }
        private SLDFileSummaryInfo GetFileSummaryInfo(string file)
        {
            SLDFileSummaryInfo sldFileSummaryInfo = new SLDFileSummaryInfo();
            sldFileSummaryInfo.FileName = file;
            SldWorks.SldWorks swApp = new SldWorks.SldWorks();
            swApp.Visible = true;
            ModelDoc2 swModel = (ModelDoc2)swApp.OpenDoc(file, (int)swDocumentTypes_e.swDocPART);
            if (swModel == null)
            {
                return sldFileSummaryInfo;
            }
            sldFileSummaryInfo.Title = swModel.SummaryInfo[(int)swSummInfoField_e.swSumInfoTitle];
            sldFileSummaryInfo.Subject = swModel.SummaryInfo[(int)swSummInfoField_e.swSumInfoSubject];
            sldFileSummaryInfo.Author = swModel.SummaryInfo[(int)swSummInfoField_e.swSumInfoAuthor];
            sldFileSummaryInfo.Keywords = swModel.SummaryInfo[(int)swSummInfoField_e.swSumInfoKeywords];
            sldFileSummaryInfo.Comment = swModel.SummaryInfo[(int)swSummInfoField_e.swSumInfoComment];
            sldFileSummaryInfo.SavedBy = swModel.SummaryInfo[(int)swSummInfoField_e.swSumInfoSavedBy];
            sldFileSummaryInfo.DateCreated = swModel.SummaryInfo[(int)swSummInfoField_e.swSumInfoCreateDate];
            sldFileSummaryInfo.DateSaved = swModel.SummaryInfo[(int)swSummInfoField_e.swSumInfoSaveDate];
            sldFileSummaryInfo.DateCreated2 = swModel.SummaryInfo[(int)swSummInfoField_e.swSumInfoCreateDate2];
            sldFileSummaryInfo.DateSaved2 = swModel.SummaryInfo[(int)swSummInfoField_e.swSumInfoSaveDate2];

            swApp.CloseDoc(file);
            return sldFileSummaryInfo;
        }
        private void button4_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("文件名,标题,主题,作者,关键字,备注,保存者,创建日期,保存日期,创建日期2,保存日期2\n");
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                stringBuilder.Append($"{row.Cells[0].Value},{row.Cells[1].Value},{row.Cells[2].Value},{row.Cells[3].Value},{row.Cells[4].Value},{row.Cells[5].Value},{row.Cells[6].Value},{row.Cells[7].Value},{row.Cells[8].Value},{row.Cells[9].Value},{row.Cells[10].Value}\n");
            }

            try
            {
                File.WriteAllText(saveFileDialog1.FileName, stringBuilder.ToString());
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, "保存文件时出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void button5_Click(object sender, EventArgs e)
        {
            checkedListBox1.Items.Clear();
            label_Count.Text = $"共计:{checkedListBox1.Items.Count}项";
        }
        private void button6_Click(object sender, EventArgs e)
        {
            for (int i = checkedListBox1.CheckedItems.Count - 1; i >= 0; i--)
            {
                checkedListBox1.Items.Remove(checkedListBox1.CheckedItems[i]);
            }
            label_Count.Text = $"共计:{checkedListBox1.Items.Count}项";
        }
        private void button7_Click(object sender, EventArgs e)
        {
            checkedListBox1.ClearSelected();
            label_Count.Text = $"共计:{checkedListBox1.Items.Count}项";
        }

        private void button8_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, !checkedListBox1.GetItemChecked(i));
            }
            label_Count.Text = $"共计:{checkedListBox1.Items.Count}项";
        }

        private void button9_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
            }
            label_Count.Text = $"共计:{checkedListBox1.Items.Count}项";
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
        }

        private void checkedListBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            label_Count.Text = $"共计:{checkedListBox1.Items.Count}项";
        }
    }
}