using LibBundle;
using LibGGPK2.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibGGPK2
{
    /// <summary>
    /// Container for handling GGPK file
    /// </summary>
    public class GGPKContainer : IDisposable
    {
        public FileStream fileStream;
        public BinaryReader Reader;
        public BinaryWriter Writer;
        public readonly GGPKRecord ggpkRecord;
        public readonly RecordTreeNode rootDirectory;
        public readonly DirectoryRecord OriginalBundles2;
        public readonly BundleDirectoryNode FakeBundles2;
        public readonly IndexContainer Index;
        public readonly FileRecord IndexRecord;
        public readonly LinkedList<FreeRecord> LinkedFreeRecords;
        protected readonly Dictionary<LibBundle.Records.BundleRecord, FileRecord> _RecordOfBundle;

        /// <summary>
        /// Get the FileRecord of a bundle
        /// </summary>
        public FileRecord RecordOfBundle(LibBundle.Records.BundleRecord bundleRecord) {
            if (_RecordOfBundle == null) return null;
            if (!_RecordOfBundle.TryGetValue(bundleRecord, out var fr))
                _RecordOfBundle.Add(bundleRecord, fr = (FileRecord)FindRecord(bundleRecord.Name, OriginalBundles2));
            //if (fr == null)
            //    throw new FileNotFoundException("Missing file: " + bundleRecord.Name + " in GGPK");
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
                Environment.CurrentDirectory = Directory.GetParent(path).FullName;
                Index = new IndexContainer(path);
                if (BuildTree) {
                    rootDirectory = FakeBundles2 = new BundleDirectoryNode("Bundles2", "", MurmurHash2Unsafe.Hash("bundles2", 0), 0, 0, this);
                    foreach (var f in Index.Files)
                        BuildBundleTree(f, rootDirectory);
                }
                return;
            }

            // Open File
            fileStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            Reader = new BinaryReader(fileStream);
            Writer = new BinaryWriter(fileStream);

            // Read ROOT Directory Record
            BaseRecord ggpk;
            while ((ggpk = GetRecord()) is not GGPKRecord);
            ggpkRecord = ggpk as GGPKRecord;
            rootDirectory = GetRecord(ggpkRecord.RootDirectoryOffset) as DirectoryRecord;
            rootDirectory.Name = "ROOT";

            // Build Linked FreeRecord List
            LinkedFreeRecords = new LinkedList<FreeRecord>();
            var NextFreeOffset = ggpkRecord.FirstFreeRecordOffset;
            while (NextFreeOffset > 0)
            {
                FreeRecord current = GetRecord(NextFreeOffset) as FreeRecord;
                LinkedFreeRecords.AddLast(current);
                NextFreeOffset = current.NextFreeOffset;
            }

            // Read Bundles
            OriginalBundles2 = rootDirectory.Children.FirstOrDefault(d => d.GetNameHash() == MurmurHash2Unsafe.Hash("bundles2", 0)) as DirectoryRecord;
            if (OriginalBundles2?.Children.FirstOrDefault(r => r.Name == "_.index.bin") is FileRecord _index)
            {
                IndexRecord = _index;
                if (BundleMode)
                    return;
                fileStream.Seek(_index.DataBegin, SeekOrigin.Begin);
                Index = new IndexContainer(Reader);
                if (BuildTree) {
                    FakeBundles2 = new BundleDirectoryNode("Bundles2", "", MurmurHash2Unsafe.Hash("bundles2", 0), (int)OriginalBundles2.Offset, OriginalBundles2.Length, this);
                    rootDirectory.Children.Remove(OriginalBundles2);
                    rootDirectory.Children.Add(FakeBundles2);
                    foreach (var f in Index.Files)
                        BuildBundleTree(f, FakeBundles2);
                }
                _RecordOfBundle = new Dictionary<LibBundle.Records.BundleRecord, FileRecord>(Index.Bundles.Length);
            } // else BundleMode = true;
        }

        public virtual void BuildBundleTree(LibBundle.Records.FileRecord fr, RecordTreeNode parent)
        {
            var SplittedPath = fr.path.Split('/');
            var path = "";
            for (var i = 0; i < SplittedPath.Length; i++)
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
        public virtual BaseRecord GetRecord(long? offset = null)
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
        /// <param name="path">Path in GGPK under <paramref name="parent"/></param>
        /// <param name="parent">Where to start searching, null for ROOT directory in GGPK</param>
        /// <returns>null if not found</returns>
        public virtual RecordTreeNode FindRecord(string path, RecordTreeNode parent = null) {
            parent ??= rootDirectory;
            var SplittedPath = path.Split('/', '\\');
            foreach (var name in SplittedPath)
            {
                var next = parent.GetChildItem(name);
                if (next == null)
                    return null;
                parent = next;
            }
            return parent;
        }

        /// <summary>
        /// Defragment the GGPK synchronously.
        /// Currently isn't implemented.
        /// Throw a <see cref="NotImplementedException"/>.
        /// </summary>
        public virtual void Defragment()
        {
            throw new NotImplementedException();
            //TODO
        }

#pragma warning disable CA1816 // Dispose should call SuppressFinalize
        public virtual void Dispose()
		{
            Writer.Flush();
            Writer.Close();
            Reader.Close();
        }
#pragma warning restore CA1816 // Dispose should call SuppressFinalize

        /// <summary>
        /// Export files
        /// </summary>
        /// <param name="list">File list to export. (generate by <see cref="RecursiveFileList"/>)
        /// The list must be sort by thier bundle to speed up exportation.</param>
        /// <param name="ProgressStep">It will be executed every time a file is exported</param>
        public static void Export(IEnumerable<KeyValuePair<IFileRecord, string>> list, Action ProgressStep = null)
        {
            LibBundle.Records.BundleRecord br = null;
            MemoryStream ms = null;
            var failBundles = 0;
            var failFiles = 0;
            foreach (var (record, path) in list) {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (record is BundleFileNode bfn) {
                    if (br != bfn.BundleFileRecord.bundleRecord) {
                        ms?.Close();
                        br = bfn.BundleFileRecord.bundleRecord;
                        br.Read(bfn.ggpkContainer.Reader, bfn.ggpkContainer.RecordOfBundle(br)?.DataBegin);
                        ms = br.Bundle?.Read(bfn.ggpkContainer.Reader);
                        if (ms == null)
                            ++failBundles;
                    }
                    if (ms == null)
                        ++failFiles;
                    else
                        File.WriteAllBytes(path, bfn.BatchReadFileContent(ms));
                } else
                    File.WriteAllBytes(path, record.ReadFileContent());
                ProgressStep?.Invoke();
            }
            if (failBundles != 0 || failFiles != 0)
                throw new BundleMissingException(failBundles, failFiles);
        }

        public class BundleMissingException : Exception {
            public int failBundles, failFiles;
            public BundleMissingException(int failBundles, int failFiles) : base($"{failBundles} bundles are missing\n{failFiles} files were not exported!") {
                this.failBundles = failBundles;
                this.failFiles = failFiles;
            }
        }

        /// <summary>
        /// Replace files
        /// </summary>
        /// <param name="list">File list to replace (generate by <see cref="RecursiveFileList"/>)</param>
        /// <param name="ProgressStep">It will be executed every time a file is replaced</param>
        public virtual void Replace(IEnumerable<KeyValuePair<IFileRecord, string>> list, Action ProgressStep = null) {
            var bundles = Index == null ? new() : new List<LibBundle.Records.BundleRecord>(Index.Bundles);
            var changed = false;
            var BundleToSave = Index.GetSmallestBundle(bundles);
            var fr = RecordOfBundle(BundleToSave);

            if (Index != null) // else BundleMode
                if (fileStream == null) // SteamMode
                    while (!File.Exists(BundleToSave.Name)) {
                        bundles.Remove(BundleToSave);
                        if (bundles.Count == 0)
                            throw new("Couldn't find a bundle to save");
                        BundleToSave = Index.GetSmallestBundle(bundles);
                    }
                else
                    while (fr == null) {
                        bundles.Remove(BundleToSave);
                        if (bundles.Count == 0)
                            throw new("Couldn't find a bundle to save");
                        BundleToSave = Index.GetSmallestBundle(bundles);
                        fr = RecordOfBundle(BundleToSave);
                    }

            var SavedSize = 0;
            foreach (var (record, path) in list) {
                if (SavedSize > 200000000 && bundles.Count > 1) { // 200MB per bundle
                    changed = true;
                    if (fileStream == null) // SteamMode
                        BundleToSave.Save();
                    else {
                        fr.ReplaceContent(BundleToSave.Save(Reader, fr.DataBegin));
                        BundleToSave.Bundle.offset = fr.DataBegin;
                        BundleFileNode.LastFileToUpdate.RemoveOldCache(BundleToSave);
                    }
                    BundleToSave = Index.GetSmallestBundle();

                    if (Index != null) { // else BundleMode
                        fr = RecordOfBundle(BundleToSave);
                        if (fileStream == null) // SteamMode
                            while (!File.Exists(BundleToSave.Name)) {
                                bundles.Remove(BundleToSave);
                                BundleToSave = Index.GetSmallestBundle(bundles);
                            }
                        else
                            while (fr == null) {
                                bundles.Remove(BundleToSave);
                                BundleToSave = Index.GetSmallestBundle(bundles);
                                fr = RecordOfBundle(BundleToSave);
                            }
                    }
                    SavedSize = 0;
                }

                if (record is BundleFileNode bfn) // In Bundle
                    SavedSize += bfn.BatchReplaceContent(File.ReadAllBytes(path), BundleToSave);
                else // In GGPK
                    record.ReplaceContent(File.ReadAllBytes(path));
                ProgressStep();
            }
            if (BundleToSave != null && SavedSize > 0) {
                changed = true;
                if (fileStream == null) // SteamMode
                    BundleToSave.Save();
                else {
                    fr.ReplaceContent(BundleToSave.Save(Reader, fr.DataBegin));
                    BundleToSave.Bundle.offset = fr.DataBegin;
                    BundleFileNode.LastFileToUpdate.RemoveOldCache(BundleToSave);
                }
            }

            // Save Index
            if (changed)
                if (fr == null)
                    Index.Save("_.index.bin");
                else
                    IndexRecord.ReplaceContent(Index.Save());
        }

        /// <summary>
        /// Replace files with .zip
        /// </summary>
        /// <param name="list">File list to replace (generate by <see cref="RecursiveFileList"/>)</param>
        /// <param name="ProgressStep">It will be executed every time a file is replaced</param>
        public virtual void Replace(IEnumerable<KeyValuePair<IFileRecord, ZipArchiveEntry>> list, Action ProgressStep = null) {
            var bundles = Index == null ? new() : new List<LibBundle.Records.BundleRecord>(Index.Bundles);
            var changed = false;
            var BundleToSave = Index.GetSmallestBundle(bundles);
            var fr = RecordOfBundle(BundleToSave);

            if (Index != null) // else BundleMode
                if (fileStream == null) // SteamMode
                    while (!File.Exists(BundleToSave.Name)) {
                        bundles.Remove(BundleToSave);
                        if (bundles.Count == 0)
                            throw new("Couldn't find a bundle to save");
                        BundleToSave = Index.GetSmallestBundle(bundles);
                    }
                else
                    while (fr == null) {
                        bundles.Remove(BundleToSave);
                        if (bundles.Count == 0)
                            throw new("Couldn't find a bundle to save");
                        BundleToSave = Index.GetSmallestBundle(bundles);
                        fr = RecordOfBundle(BundleToSave);
                    }

            var SavedSize = 0;
            foreach (var (record, zipped) in list) {
                if (SavedSize > 200000000 && bundles.Count > 1) { // 200MB per bundle
                    changed = true;
                    if (fileStream == null) // SteamMode
                        BundleToSave.Save();
                    else {
                        fr.ReplaceContent(BundleToSave.Save(Reader, fr.DataBegin));
                        BundleToSave.Bundle.offset = fr.DataBegin;
                        BundleFileNode.LastFileToUpdate.RemoveOldCache(BundleToSave);
                    }
                    BundleToSave = Index.GetSmallestBundle();

                    if (Index != null) { // else BundleMode
                        fr = RecordOfBundle(BundleToSave);
                        if (fileStream == null) // SteamMode
                            while (!File.Exists(BundleToSave.Name)) {
                                bundles.Remove(BundleToSave);
                                BundleToSave = Index.GetSmallestBundle(bundles);
                            }
                        else
                            while (fr == null) {
                                bundles.Remove(BundleToSave);
                                BundleToSave = Index.GetSmallestBundle(bundles);
                                fr = RecordOfBundle(BundleToSave);
                            }
                    }
                    SavedSize = 0;
                }

                var s = zipped.Open();
                var b = new byte[zipped.Length];
                var l = 0;
                while (l < b.Length)
                    l += s.Read(b, l, b.Length - l);
                s.Close();
                if (record is BundleFileNode bfn) // In Bundle
                    SavedSize += bfn.BatchReplaceContent(b, BundleToSave);
                else // In GGPK
                    record.ReplaceContent(b);
                ProgressStep();
            }
            if (BundleToSave != null && SavedSize > 0) {
                changed = true;
                if (fileStream == null)
                    BundleToSave.Save();
                else {
                    fr.ReplaceContent(BundleToSave.Save(Reader, fr.DataBegin));
                    BundleToSave.Bundle.offset = fr.DataBegin;
                    BundleFileNode.LastFileToUpdate.RemoveOldCache(BundleToSave);
                }
            }

            // Save Index
            if (changed)
                if (fr == null)
                    Index.Save("_.index.bin");
                else
                    IndexRecord.ReplaceContent(Index.Save());
        }

        /// <summary>
        /// Get the file list under a node to export/replace
        /// </summary>
        /// <param name="record">File/Directory Record to export</param>
        /// <param name="path">Path to save</param>
        /// <param name="list">File list (use <see cref="BundleSortComparer"/> when reading)</param>
        /// <param name="export">True for export False for replace</param>
        /// <param name="regex">Regular Expression for filtering files by their path</param>
        public static void RecursiveFileList(RecordTreeNode record, string path, ICollection<KeyValuePair<IFileRecord, string>> list, bool export, string regex = null)
        {
            if (record is IFileRecord fr)
            {
                if ((export || File.Exists(path)) && (regex == null || Regex.IsMatch(record.GetPath(), regex)))
                    list.Add(new(fr, path));
            }
            else
                foreach (var f in record.Children)
                    RecursiveFileList(f, path + "\\" + f.Name, list, export, regex);
        }

        /// <summary>
        /// Get the file list under a node
        /// </summary>
        /// <param name="record">File/Directory Record</param>
        /// <param name="list">File list (use <see cref="BundleSortComparer"/> when reading)</param>
        /// <param name="regex">Regular Expression for filtering files by their path</param>
        public static void RecursiveFileList(RecordTreeNode record, ICollection<IFileRecord> list, string regex = null) {
            if (record is IFileRecord fr) {
                if (regex == null || Regex.IsMatch(record.GetPath(), regex))
                    list.Add(fr);
            } else
                foreach (var f in record.Children)
                    RecursiveFileList(f, list, regex);
        }

        /// <summary>
        /// Get the file list to replace from a folder on disk
        /// </summary>
        /// <param name="ROOTPath">"ROOT" folder on disk</param>
        /// <param name="list">File list (use <see cref="BundleSortComparer"/> when reading)</param>
        /// <param name="searchPattern">Use to filter files in "ROOT" folder on disk</param>
        /// <param name="regex">Regular Expression for filtering files in GGPK by their path</param>
        public virtual void GetFileList(string ROOTPath, ICollection<KeyValuePair<IFileRecord, string>> list, string searchPattern = "*", string regex = null) {
            var files = Directory.GetFiles(ROOTPath, searchPattern, SearchOption.AllDirectories);
            foreach (var f in files) {
                var path = f[(ROOTPath.Length + 1)..];
                if ((regex == null || Regex.IsMatch(path, regex)) && FindRecord(path) is IFileRecord ifr)
                    list.Add(new(ifr, f));
			}
        }

        /// <summary>
        /// Get the file list to replace from a .zip on disk
        /// </summary>
        /// <param name="ZipEntries">ZipArchiveEntries to read/></param>
        /// <param name="list">File list (use <see cref="BundleSortComparer"/> when reading)</param>
        /// <param name="regex">Regular Expression for filtering files in GGPK by their path</param>
        public virtual void GetFileListFromZip(IEnumerable<ZipArchiveEntry> ZipEntries, ICollection<KeyValuePair<IFileRecord, ZipArchiveEntry>> list, string regex = null) {
            var first = ZipEntries.FirstOrDefault();
            if (first == null)
                return;

            RecordTreeNode parent;
            if (first.FullName.StartsWith("ROOT/"))
                parent = rootDirectory;
            else if (first.FullName.StartsWith("Bundles2/"))
                parent = (RecordTreeNode)FakeBundles2 ?? OriginalBundles2;
            else
                throw new("The root directory in zip must be ROOT or Bundles2");

            var rootNameOffset = parent.Name.Length + 1;
            foreach (var zae in ZipEntries) {
                if (zae.FullName.EndsWith('/'))
                    continue;
                var path = zae.FullName[rootNameOffset..];
                if (regex == null || Regex.IsMatch(path, regex)) {
                    if (FindRecord(path, parent) is not IFileRecord ifr)
                        throw new Exception("Not found in GGPK: " + zae.FullName);
                    list.Add(new(ifr, zae));
                }
			}
        }
    }

    /// <summary>
    /// Use to sort the files by their bundle.
    /// </summary>
    public class BundleSortComparer : IComparer<IFileRecord>
    {
        public static readonly BundleSortComparer Instance = new();
        public virtual int Compare(IFileRecord x, IFileRecord y)
        {
            // In GGPK
            if (x is FileRecord frx)
                if (y is FileRecord fry)
                    return frx.DataBegin > fry.DataBegin ? 1 : -1;
                else
                    return -1;
            else if (y is FileRecord)
                return 1;

            // In Bundle
            var bfx = (BundleFileNode)x;
            var bfy = (BundleFileNode)y;
            var ofx = bfx.ggpkContainer.RecordOfBundle(bfx.BundleFileRecord.bundleRecord)?.DataBegin ?? 0;
            var ofy = bfy.ggpkContainer.RecordOfBundle(bfy.BundleFileRecord.bundleRecord)?.DataBegin ?? 0;
            if (ofx > ofy)
                return 1;
            else if (ofx < ofy)
                return -1;
            else
                return bfx.Offset > bfy.Offset ? 1 : -1;
        }
    }
}