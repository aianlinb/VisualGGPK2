using LibBundle;
using LibGGPK2.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LibGGPK2
{
    /// <summary>
    /// Container for handling GGPK file
    /// </summary>
    public class GGPKContainer : IDisposable
    {
        public static readonly BundleSortComp BundleComparer = new BundleSortComp();

        public readonly FileStream fileStream;
        public readonly BinaryReader Reader;
        public readonly BinaryWriter Writer;
        public readonly GGPKRecord ggpkRecord;
        public readonly RecordTreeNode rootDirectory;
        public readonly DirectoryRecord OriginalBundles2;
        public readonly BundleDirectoryNode FakeBundles2;
        public readonly IndexContainer Index;
        public readonly FileRecord IndexRecord;
        public readonly LinkedList<FreeRecord> LinkedFreeRecords;
        private readonly Dictionary<LibBundle.Records.BundleRecord, FileRecord> _RecordOfBundle;

        public FileRecord RecordOfBundle(LibBundle.Records.BundleRecord bundleRecord) {
            if (_RecordOfBundle == null) return null;
            if (!_RecordOfBundle.TryGetValue(bundleRecord, out var fr))
                fr = (FileRecord)FindRecord(bundleRecord.Name, OriginalBundles2);
            return fr;
        }

        /// <summary>
        /// Load GGPK
        /// </summary>
        /// <param name="path">Path to GGPK file</param>
        public GGPKContainer(string path, bool BundleMode = false, bool SteamMode = false, bool BuildTree = true)
        {
            // Steam Mode (No GGPK)
            if (SteamMode) {
                if (BundleMode)
                    throw new NotSupportedException("BundleMode and SteamMode cannot be both true");
                Index = new IndexContainer(path);
                rootDirectory = FakeBundles2 = new BundleDirectoryNode("Bundles2", "", MurmurHash2Unsafe.Hash("bundles2", 0), 0, 0, this);
                foreach (var f in Index.Files)
                    BuildBundleTree(f, rootDirectory);
                return;
            }

            // Open File
            fileStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            Reader = new BinaryReader(fileStream);
            Writer = new BinaryWriter(fileStream);

            // Read ROOT Directory Record
            BaseRecord ggpk;
            while (!((ggpk = GetRecord()) is GGPKRecord));
            ggpkRecord = ggpk as GGPKRecord;
            rootDirectory = GetRecord(ggpkRecord.RootDirectoryOffset) as DirectoryRecord;
            rootDirectory.Name = "ROOT";

            // Build Linked FreeRecord List
            LinkedFreeRecords = new LinkedList<FreeRecord>();
            long NextFreeOffset = ggpkRecord.FirstFreeRecordOffset;
            while (NextFreeOffset > 0)
            {
                FreeRecord current = GetRecord(NextFreeOffset) as FreeRecord;
                LinkedFreeRecords.AddLast(current);
                NextFreeOffset = current.NextFreeOffset;
            }

            if (BundleMode) return;
            // Read Bundles
            var Bundles2DirectoryNameHash = MurmurHash2Unsafe.Hash("bundles2", 0);
            OriginalBundles2 = rootDirectory.Children.First(d => d.GetNameHash() == Bundles2DirectoryNameHash) as DirectoryRecord;
            if (OriginalBundles2.Children.FirstOrDefault(r => r.Name == "_.index.bin") is FileRecord _index)
            {
                IndexRecord = _index;
                fileStream.Seek(_index.DataBegin, SeekOrigin.Begin);
                Index = new IndexContainer(Reader);
                if (BuildTree) {
                    FakeBundles2 = new BundleDirectoryNode("Bundles2", "", MurmurHash2Unsafe.Hash("bundles2", 0), (int)OriginalBundles2.Offset, OriginalBundles2.Length, this);
                    rootDirectory.Children.Remove(OriginalBundles2);
                    rootDirectory.Children.Add(FakeBundles2);
                    foreach (var f in Index.Files)
                        BuildBundleTree(f, FakeBundles2);
                }
            }
            _RecordOfBundle = new Dictionary<LibBundle.Records.BundleRecord, FileRecord>(Index.Bundles.Length);
        }

        public void BuildBundleTree(LibBundle.Records.FileRecord fr, RecordTreeNode parent)
        {
            var SplittedPath = fr.path.Split('/');
            var path = "";
            for (int i = 0; i < SplittedPath.Length; i++)
            {
                var name = SplittedPath[i];
                var isFile = (i + 1 == SplittedPath.Length);
                var parentOfFile = (i + 2 == SplittedPath.Length);
                var next = parent.GetChildItem(name);
                path += name;
                if (!isFile) path += "/";
                if (next == null)
                { // No exist node, Build a new node
                    if (isFile)
                        next = new BundleFileNode(name, fr, this);
                    else if (parentOfFile)
                        next = new BundleDirectoryNode(name, path, fr.parent.NameHash, fr.parent.Offset, fr.parent.Size, this);
                    else
                        next = new BundleDirectoryNode(name, path, 0, 0, 0, this);
                    parent.Children.Add(next);
                    next.Parent = parent;
                }
                else if (parentOfFile && next.Offset == 0)
                {
                    ((BundleDirectoryNode)next).Hash = fr.parent.NameHash;
                    next.Offset = fr.parent.Offset;
                    next.Length = fr.parent.Size;
                }
                parent = next;
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
        /// Find the record with a <paramref name="path"/>
        /// </summary>
        public RecordTreeNode FindRecord(string path, RecordTreeNode parent = null)
        {
            var SplittedPath = path.Split(new char[] { '/', '\\' });
            if (parent == null)
                parent = rootDirectory;
            for (int i = 0; i < SplittedPath.Length; i++)
            {
                var name = SplittedPath[i];
                var next = parent.GetChildItem(name);
                if (next == null)
                    return null;
                parent = next;
            }
            return parent;
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
        /// Export files asynchronously
        /// </summary>
        /// <param name="record">File/Directory Record to export</param>
        /// <param name="path">Path to save</param>
        /// <param name="ProgressStep">It will be executed every time a file is exported</param>
        public static async Task<Exception> ExportAsync(ICollection<KeyValuePair<IFileRecord, string>> list, Action ProgressStep = null)
        {
            await Task.Delay(1).ConfigureAwait(false);
            return Export(list, ProgressStep); ;
        }

        /// <summary>
        /// Export files synchronously
        /// </summary>
        /// <param name="list">File list to export. (generate by <see cref="RecursiveFileList"/>)
        /// The list must be sort by thier bundle to speed up exportation.</param>
        /// <param name="ProgressStep">It will be executed every time a file is exported</param>
        public static Exception Export(ICollection<KeyValuePair<IFileRecord, string>> list, Action ProgressStep = null)
        {
            try {
                LibBundle.Records.BundleRecord br = null;
                MemoryStream ms = null;
                foreach (var record in list) {
                    Directory.CreateDirectory(Directory.GetParent(record.Value).FullName);
                    if (record.Key is BundleFileNode bfn) {
                        if (br != bfn.BundleFileRecord.bundleRecord) {
                            ms?.Close();
                            br = bfn.BundleFileRecord.bundleRecord;
                            br.Read(bfn.ggpkContainer.Reader, bfn.ggpkContainer.RecordOfBundle(br)?.DataBegin);
                            ms = br.Bundle.Read(bfn.ggpkContainer.Reader);
                        }
                        File.WriteAllBytes(record.Value, bfn.BatchReadFileContent(ms));
                    } else
                        File.WriteAllBytes(record.Value, record.Key.ReadFileContent());
                    ProgressStep?.Invoke();
                }
            } catch (Exception ex) {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Replace files asynchronously
        /// </summary>
        /// <param name="list">File list to replace (generate by <see cref="RecursiveFileList"/>)</param>
        /// <param name="ProgressStep">It will be executed every time a file is replaced</param>
        public async Task<Exception> ReplaceAsync(ICollection<KeyValuePair<IFileRecord, string>> list, Action ProgressStep = null)
        {
            await Task.Delay(1).ConfigureAwait(false);
            return Replace(list, ProgressStep);
        }

        /// <summary>
        /// Replace files synchronously
        /// </summary>
        /// <param name="list">File list to replace (generate by <see cref="RecursiveFileList"/>)</param>
        /// <param name="ProgressStep">It will be executed every time a file is replaced</param>
        public Exception Replace(ICollection<KeyValuePair<IFileRecord, string>> list, Action ProgressStep = null)
        {
            try {
                var changed = false;
                var BundleToSave = Index?.GetSmallestBundle();
                var fr = RecordOfBundle(BundleToSave);
                var SavedSize = 0;
                foreach (var record in list) {
                    if (SavedSize > 50000000) // 50MB per bundle
                    {
                        changed = true;
                        if (fr == null) {
                            BundleToSave.Save();
                        } else {
                            fr.ReplaceContent(BundleToSave.Save(Reader, fr.DataBegin));
                            BundleToSave.Bundle.offset = fr.DataBegin;
                            BundleFileNode.LastFileToUpdate.UpdateCache(BundleToSave);
                        }
                        BundleToSave = Index.GetSmallestBundle();
                        fr = RecordOfBundle(BundleToSave);
                        SavedSize = 0;
                    }
                    if (record.Key is BundleFileNode bfn)
                        SavedSize += bfn.BatchReplaceContent(File.ReadAllBytes(record.Value), BundleToSave);
                    else
                        record.Key.ReplaceContent(File.ReadAllBytes(record.Value));
                    ProgressStep();
                }
                if (BundleToSave != null && SavedSize > 0) {
                    changed = true;
                    if (fr == null) {
                        BundleToSave.Save();
                    } else {
                        fr.ReplaceContent(BundleToSave.Save(Reader, fr.DataBegin));
                        BundleToSave.Bundle.offset = fr.DataBegin;
                        BundleFileNode.LastFileToUpdate.UpdateCache(BundleToSave);
                    }
                }

                // Save Index
                if (changed)
                    if (fr == null)
                        Index.Save("_.index.bin");
                    else
                        IndexRecord.ReplaceContent(Index.Save());
            } catch (Exception ex) {
                return ex;
			}
            return null;
        }

        /// <summary>
        /// Get the file list to export
        /// </summary>
        /// <param name="record">File/Directory Record to export</param>
        /// <param name="path">Path to save</param>
        /// <param name="list">File list</param>
        /// <param name="export">True for export False for replace</param>
        public static void RecursiveFileList(RecordTreeNode record, string path, ICollection<KeyValuePair<IFileRecord, string>> list, bool export, string regex = null)
        {
            if (record is IFileRecord fr)
            {
                if ((export || File.Exists(path)) && (regex == null || Regex.IsMatch(record.GetPath(), regex)))
                    list.Add(new KeyValuePair<IFileRecord, string>(fr, path));
            }
            else
                foreach (var f in record.Children)
                    RecursiveFileList(f, path + "\\" + f.Name, list, export, regex);
        }
    }

    /// <summary>
    /// Use to sort the files by their bundle.
    /// </summary>
    public class BundleSortComp : IComparer<IFileRecord>
    {
        public virtual int Compare(IFileRecord x, IFileRecord y)
        {
            if (x is FileRecord frx)
                if (y is FileRecord fry)
                    return frx.DataBegin > fry.DataBegin ? 1 : -1;
                else
                    return -1;
            else
                if (y is FileRecord)
                return 1;
            else
            {
                var bfx = (BundleFileNode)x;
                var bfy = (BundleFileNode)y;
                var ofx = bfx.ggpkContainer.RecordOfBundle(bfx.BundleFileRecord.bundleRecord)?.DataBegin ?? 0;
                var ofy = bfy.ggpkContainer.RecordOfBundle(bfx.BundleFileRecord.bundleRecord)?.DataBegin ?? 0;
                if (ofx > ofy)
                    return 1;
                else if (ofx < ofy)
                    return -1;
                else
                    return bfx.Offset > bfy.Offset ? 1 : -1;
            }
        }
    }
}