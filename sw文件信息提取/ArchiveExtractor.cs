using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sw文件信息提取
{
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
}
