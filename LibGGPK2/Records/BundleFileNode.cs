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
        public readonly static LinkedList<KeyValuePair<BundleRecord, MemoryStream>> CachedBundleData = new LinkedList<KeyValuePair<BundleRecord, MemoryStream>>();
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
                BundleFileRecord.bundleRecord.Read(br, ggpkContainer.RecordOfBundle(BundleFileRecord.bundleRecord).DataBegin);
                cached = BundleFileRecord.bundleRecord.Bundle.Read(br);
                CachedBundleData.AddFirst(new KeyValuePair<BundleRecord, MemoryStream>(BundleFileRecord.bundleRecord, cached));
                CachedSize += cached.Length;
                while (CachedSize > 300000000 && CachedBundleData.Count > 1) {
                    var ms = CachedBundleData.Last.Value.Value;
                    CachedSize -= ms.Length;
                    ms.Close();
                    CachedBundleData.RemoveLast();
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
            var BundleToSave = ggpkContainer.Index.GetSmallestBundle();
            BundleFileRecord.Write(NewContent);
            if (BundleFileRecord.bundleRecord != BundleToSave)
                BundleFileRecord.Move(BundleToSave);
            var NewBundleData = BundleToSave.Save(ggpkContainer.Reader, ggpkContainer.RecordOfBundle(BundleToSave).DataBegin);
            var fr = ggpkContainer.RecordOfBundle(BundleToSave);
            fr.ReplaceContent(NewBundleData);
            BundleToSave.Bundle.offset = fr.DataBegin;
            UpdateCache(BundleToSave);
            ggpkContainer.IndexRecord.ReplaceContent(ggpkContainer.Index.Save());
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

        public void UpdateCache(BundleRecord br) {
            Hash = BundleFileRecord.NameHash;
            Offset = BundleFileRecord.Offset;
            Length = BundleFileRecord.Size;
            var node = CachedBundleData.First;
            while (node != null) {
                if (node.Value.Key == br) {
                    CachedBundleData.Remove(node);
                    break;
                } else
                    node = node.Next;
            }
        }

        private DataFormats? _DataFormat = null;
        /// <summary>
        /// Content data format of this file
        /// </summary>
        public virtual DataFormats DataFormat
        {
            get
            {
                if (_DataFormat == null)
                {
                    switch (Path.GetExtension(Name).ToLower())
                    {
                        case ".act":
                        case ".ais":
                        case ".amd": // Animated Meta Data
                        case ".ao": // Animated Object
                        case ".aoc": // Animated Object Controller
                        case ".arl":
                        case ".arm": // Rooms
                        case ".atlas":
                        case ".cht": // ChestData
                        case ".clt":
                        case ".dct": // Decals
                        case ".ddt": // Doodads
                        case ".dgr":
                        case ".dlp":
                        case ".ecf":
                        case ".edp":
                        case ".env": // Environment
                        case ".epk":
                        case ".et":
                        case ".ffx": // FFX Render
                        case ".fmt":
                        case ".fxgraph":
                        case ".gft":
                        case ".gt": // Ground Types
                        case ".idl":
                        case ".idt":
                        case ".mat": // Materials
                        case ".mtd":
                        case ".ot":
                        case ".otc":
                        case ".pet":
                        case ".red":
                        case ".rs": // Room Set
                        case ".sm": // Skin Mesh
                        case ".tgr":
                        case ".tgt":
                        case ".trl": // Trails Effect
                        case ".tsi":
                        case ".tst":
                        case ".txt":
                        case ".ui": // User Interface
                        case ".xml":
                            _DataFormat = DataFormats.Unicode;
                            break;
                        case ".ast":
                        case ".csv":
                        case ".filter": // Item/loot Filter
                        case ".fx": // Shader
                        case ".hlsl": // Shader
                        case ".mel": // Maya Embedded Language
                        case ".mtp":
                        case ".properties":
                        case ".slt":
                        case ".smd": // Skin Mesh Data
                            _DataFormat = DataFormats.Ascii;
                            break;
                        case ".dat":
                        case ".dat64":
                            _DataFormat = DataFormats.Dat;
                            break;
                        case ".dds":
                            _DataFormat = DataFormats.TextureDds;
                            break;
                        case ".jpg":
                        case ".png":
                            _DataFormat = DataFormats.Image;
                            break;
                        case ".ogg":
                            _DataFormat = DataFormats.OGG;
                            break;
                        case ".bk2":
                            _DataFormat = DataFormats.BK2;
                            break;
                        case ".bank":
                            _DataFormat = DataFormats.BANK;
                            break;
                        default:
                            _DataFormat = DataFormats.Unknown;
                            break;
                    }
                }
                return _DataFormat.Value;
            }
        }
    }
}