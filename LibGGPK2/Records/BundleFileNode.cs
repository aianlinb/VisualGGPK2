using LibBundle.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static LibGGPK2.Records.IFileRecord;

namespace LibGGPK2.Records
{
    public class BundleFileNode : RecordTreeNode, IFileRecord
    {
        public readonly static LinkedList<KeyValuePair<BundleRecord, MemoryStream>> CachedBundleData = new();
        public static long CachedSize = 0;

        /// <summary>
        /// BundleFileNode with a cache that need to be updated
        /// </summary>
        public static BundleFileNode LastFileToUpdate;

        /// <summary>
        /// FNV1a64Hash of the path of file
        /// </summary>
        public new ulong Hash;
        /// <summary>
        /// Record of the file in bundle
        /// </summary>
        public LibBundle.Records.FileRecord BundleFileRecord;

        /// <summary>
        /// Create a node of the file in bundle
        /// </summary>
        public BundleFileNode(string name, LibBundle.Records.FileRecord record, GGPKContainer ggpkContainer)
        {
            Name = name;
            BundleFileRecord = record;
            Hash = record.NameHash;
            Offset = record.Offset;
            Length = record.Size;
            this.ggpkContainer = ggpkContainer;
        }

        /// <summary>
        /// There is no child with a file
        /// </summary>
        public override SortedSet<RecordTreeNode> Children => null;

        /// <summary>
        /// Get the file content of this record.
        /// For batch reading please use <see cref="BatchReadFileContent"/>.
        /// </summary>
        /// <param name="ggpkStream">Stream of GGPK file</param>
        public virtual byte[] ReadFileContent(Stream ggpkStream = null)
        {
            var cached = CachedBundleData.FirstOrDefault((o) => o.Key == BundleFileRecord.bundleRecord).Value;
            if (cached == null) {
                using var br = ggpkStream == null ? null : new BinaryReader(ggpkStream, Encoding.UTF8, true);
                BundleFileRecord.bundleRecord.Read(br, ggpkContainer.RecordOfBundle(BundleFileRecord.bundleRecord)?.DataBegin);
                cached = BundleFileRecord.bundleRecord.Bundle.Read(br);
                CachedBundleData.AddLast(new KeyValuePair<BundleRecord, MemoryStream>(BundleFileRecord.bundleRecord, cached));
                CachedSize += cached.Length;
                while (CachedSize > 300000000 && CachedBundleData.Count > 1) {
                    var ms = CachedBundleData.First.Value.Value;
                    CachedSize -= ms.Length;
                    ms.Close();
                    CachedBundleData.RemoveFirst();
                }
            }
            return BundleFileRecord.Read(cached);
        }

        /// <summary>
        /// Get the file content of this record from bundle stream.
        /// For batch reading
        /// </summary>
        /// <param name="bundleStream">Stream of bundle file</param>
        public virtual byte[] BatchReadFileContent(MemoryStream bundleStream)
        {
            return BundleFileRecord.Read(bundleStream);
        }

        /// <summary>
        /// Replace the file content with a new content,
        /// and write the modified bundle to the GGPK.
        /// </summary>
        /// <returns>Size of imported bytes</returns>
        public virtual void ReplaceContent(byte[] NewContent)
        {
            var BundleToSave = BundleFileRecord.bundleRecord;
            BundleFileRecord.Write(NewContent);
            if (ggpkContainer.Reader == null) {
                BundleToSave.SaveWithRecompression();
                ggpkContainer.Index.Save("_.index.bin");
            } else {
                var NewBundleData = BundleToSave.SaveWithRecompression(ggpkContainer.Reader, ggpkContainer.RecordOfBundle(BundleToSave).DataBegin);
                var fr = ggpkContainer.RecordOfBundle(BundleToSave);
                fr.ReplaceContent(NewBundleData);
                BundleToSave.Bundle.offset = fr.DataBegin;
                ggpkContainer.IndexRecord.ReplaceContent(ggpkContainer.Index.Save());
            }
            RemoveOldCache(BundleToSave);
        }

        /// <summary>
        /// Replace the file content with a new content,
        /// and return the bundle which have to be saved.
        /// </summary>
        /// <returns>Size of imported bytes</returns>
        public virtual int BatchReplaceContent(byte[] NewContent, BundleRecord BundleToSave)
        {
            BundleFileRecord.Write(NewContent);
            LastFileToUpdate = this;
            if (BundleFileRecord.bundleRecord != BundleToSave)
                BundleFileRecord.Move(BundleToSave);
            return NewContent.Length;
        }

        /// <summary>
        /// Throw a <see cref="NotSupportedException"/>
        /// </summary>
        protected override void Read()
        {
            throw new NotSupportedException("A virtual node of bundles cannot be read");
        }
        /// <summary>
        /// Throw a <see cref="NotSupportedException"/>
        /// </summary>
        internal override void Write(BinaryWriter bw = null)
        {
            throw new NotSupportedException("A virtual node of bundles cannot be written");
        }

        public virtual void RemoveOldCache(BundleRecord br) {
            Hash = BundleFileRecord.NameHash;
            Offset = BundleFileRecord.Offset;
            Length = BundleFileRecord.Size;
            var node = CachedBundleData.First;
            while (node != null)
                if (node.Value.Key == br) {
                    CachedBundleData.Remove(node);
                    break;
                } else
                    node = node.Next;
        }

        protected DataFormats? _DataFormat = null;
        /// <summary>
        /// Content data format of this file
        /// </summary>
        public virtual DataFormats DataFormat {
            get {
                _DataFormat ??= FileRecord.GetDataFormat(Name);
                return _DataFormat.Value;
            }
        }
    }
}