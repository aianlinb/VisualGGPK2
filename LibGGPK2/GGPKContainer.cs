using LibGGPK2.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibGGPK2
{
    /// <summary>
    /// Container for handling GGPK file
    /// </summary>
    public class GGPKContainer : IDisposable
    {
        public readonly FileStream fileStream;
        public readonly BinaryReader Reader;
        public readonly BinaryWriter Writer;
        public readonly GGPKRecord ggpkRecord;
        public readonly DirectoryRecord rootDirectory;
        public readonly LinkedList<FreeRecord> LinkedFreeRecords = new LinkedList<FreeRecord>();

        /// <summary>
        /// Load GGPK
        /// </summary>
        /// <param name="path">Path to GGPK file</param>
        public GGPKContainer(string path)
        {
            fileStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Reader = new BinaryReader(fileStream);
            Writer = new BinaryWriter(fileStream);

            BaseRecord ggpk;
            while (!((ggpk = GetRecord()) is GGPKRecord));
            ggpkRecord = ggpk as GGPKRecord;
            rootDirectory = GetRecord(ggpkRecord.RootDirectoryOffset) as DirectoryRecord;
            rootDirectory.Name = "ROOT";

            long NextFreeOffset = ggpkRecord.FirstFreeRecordOffset;
            while (NextFreeOffset > 0)
            {
                FreeRecord current = GetRecord(NextFreeOffset) as FreeRecord;
                LinkedFreeRecords.AddLast(current);
                NextFreeOffset = current.NextFreeOffset;
            }
        }

        /// <summary>
        /// Read a record from GGPK at <paramref name="offset"/>
        /// </summary>
        public BaseRecord GetRecord(long? offset = null)
        {
            if (offset.HasValue)
                fileStream.Seek(offset.Value, SeekOrigin.Begin);
            var length = Reader.ReadInt32();
            var tag = Reader.ReadBytes(4);
            if (tag.SequenceEqual(FileRecord.Tag))
                return new FileRecord(length, this);
            else if (tag.SequenceEqual(FreeRecord.Tag))
                return new FreeRecord(length, this);
            else if (tag.SequenceEqual(DirectoryRecord.Tag))
                return new DirectoryRecord(length, this);
            else if (tag.SequenceEqual(GGPKRecord.Tag))
                return new GGPKRecord(length, this);
            else
                throw new Exception("Invalid Record Tag: " + Encoding.ASCII.GetString(tag) + " at offset: " + (fileStream.Position - 8).ToString());
        }

        /// <summary>
        /// Find a FileRecord with a <paramref name="path"/>
        /// </summary>
        public FileRecord FindFileRecord(string path)
        {
            var SplittedPath = path.Split(new char[] { '/', '\\' });
            RecordTreeNode parent = rootDirectory;
            for (int i = 0; i < SplittedPath.Length; i++)
            {
                var name = SplittedPath[i];
                var next = parent.GetChildItem(name);
                if (next is null)
                    return null;
                parent = next;
            }
            return (FileRecord)parent;
        }

        /// <summary>
        /// Defragment the GGPK asynchronously.
        /// Currently isn't implemented.
        /// Throw a <see cref="NotImplementedException"/>.
        /// </summary>
        public async Task DefragmentAsync()
        {
            await Task.Delay(1).ConfigureAwait(false);
            Defragment();
        }

        /// <summary>
        /// Defragment the GGPK synchronously.
        /// Currently isn't implemented.
        /// Throw a <see cref="NotImplementedException"/>.
        /// </summary>
        public void Defragment()
        {
            throw new NotImplementedException();
            //TODO
        }

        public void Dispose()
        {
            Writer.Flush();
            Writer.Close();
            Reader.Close();
        }

        /// <summary>
        /// Export file/directory asynchronously
        /// </summary>
        /// <param name="record">File/Directory Record to export</param>
        /// <param name="path">Path to save</param>
        /// <param name="ProgressStep">It will be executed every time a file is exported</param>
        /// <returns>Number of files exported</returns>
        public static async Task<int> ExportAsync(RecordTreeNode record, string path, Action ProgressStep = null)
        {
            await Task.Delay(1).ConfigureAwait(false);
            return Export(record, path, ProgressStep);
        }

        /// <summary>
        /// Export file/directory synchronously
        /// </summary>
        /// <param name="record">File/Directory Record to export</param>
        /// <param name="path">Path to save</param>
        /// <param name="ProgressStep">It will be executed every time a file is exported</param>
        /// <returns>Number of files exported</returns>
        public static int Export(RecordTreeNode record, string path, Action ProgressStep = null)
        {
            if (record is FileRecord fr)
            {
                File.WriteAllBytes(path, fr.ReadFileContent());
                ProgressStep();
                return 1;
            }
            else
            {
                int count = 0;
                Directory.CreateDirectory(path);
                foreach (var f in record.Children)
                    count += Export(f, path + "\\" + f.Name, ProgressStep);
                return count;
            }
        }

        /// <summary>
        /// Replace file/directory asynchronously
        /// </summary>
        /// <param name="record">File/Directory Record to replace</param>
        /// <param name="path">Path to file to import</param>
        /// <param name="ProgressStep">It will be executed every time a file is replaced</param>
        /// <returns>Number of files replaced</returns>
        public static async Task<int> ReplaceAsync(RecordTreeNode record, string path, Action ProgressStep = null)
        {
            await Task.Delay(1).ConfigureAwait(false);
            return Replace(record, path, ProgressStep);
        }

        /// <summary>
        /// Replace file/directory synchronously
        /// </summary>
        /// <param name="record">File/Directory Record to replace</param>
        /// <param name="path">Path to file to import</param>
        /// <param name="ProgressStep">It will be executed every time a file is replaced</param>
        /// <returns>Number of files replaced</returns>
        public static int Replace(RecordTreeNode record, string path, Action ProgressStep = null)
        {
            if (record is FileRecord fr)
            {
                if (File.Exists(path))
                {
                    fr.ReplaceContent(File.ReadAllBytes(path));
                    ProgressStep();
                    return 1;
                }
                return 0;
            }
            else
            {
                int count = 0;
                foreach (var f in record.Children)
                    count += Replace(f, path + "\\" + f.Name, ProgressStep);
                return count;
            }
        }
    }
}