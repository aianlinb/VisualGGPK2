using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibGGPK2.Records
{
	public class DirectoryRecord : RecordTreeNode
    {
        public struct DirectoryEntry
        {
            /// <summary>
            /// Murmur2 hash of lowercase entry name
            /// </summary>
            public uint EntryNameHash;
            /// <summary>
            /// Offset in pack file where the record begins
            /// </summary>
            public long Offset;

            public DirectoryEntry(uint entryNameHash, long offset)
            {
                EntryNameHash = entryNameHash;
                Offset = offset;
            }
        }

        public static readonly byte[] Tag = Encoding.ASCII.GetBytes("PDIR");
        /// <summary>
        /// Use for sort the children of directory.
        /// </summary>
        public static readonly SortComp Comparer = new();

        /// <summary>
        /// Records (File/Directory) this directory contains.
        /// </summary>
        public DirectoryEntry[] Entries;
        /// <summary>
        /// Offset in pack file where entries list begins. This is only here because it makes rewriting the entries list easier.
        /// </summary>
        public long EntriesBegin;

        /// <summary>
        /// Read a DirectoryRecord from GGPK
        /// </summary>
        public DirectoryRecord(int length, GGPKContainer ggpk)
        {
            ggpkContainer = ggpk;
            Offset = ggpk.fileStream.Position - 8;
            Length = length;
            Read();
        }

        private SortedSet<RecordTreeNode> _Children;
        public override SortedSet<RecordTreeNode> Children
        {
            get
            {
                if (_Children == null)
                {
                    _Children = new SortedSet<RecordTreeNode>(Comparer);
                    foreach (var e in Entries)
                    {
                        var b = ggpkContainer.GetRecord(e.Offset) as RecordTreeNode;
                        b.Parent = this;
                        _Children.Add(b);
                    }
                }
                return _Children;
            }
        }

        protected override void Read()
        {
            var br = ggpkContainer.Reader;
            var nameLength = br.ReadInt32();
            var totalEntries = br.ReadInt32();

            Hash = br.ReadBytes(32);
            if (ggpkContainer.ggpkRecord.GGPKVersion == 4) {
                Name = Encoding.UTF32.GetString(br.ReadBytes(4 * (nameLength - 1)));
                br.BaseStream.Seek(4, SeekOrigin.Current); // Null terminator
            } else {
                Name = Encoding.Unicode.GetString(br.ReadBytes(2 * (nameLength - 1)));
                br.BaseStream.Seek(2, SeekOrigin.Current); // Null terminator
            }

            EntriesBegin = br.BaseStream.Position;
            Entries = new DirectoryEntry[totalEntries];
            for (var i = 0; i < totalEntries; i++)
                Entries[i] = new DirectoryEntry (br.ReadUInt32(), br.ReadInt64());
        }

        internal override void Write(BinaryWriter bw = null)
        {
            bw ??= ggpkContainer.Writer;
            Offset = bw.BaseStream.Position;
            bw.Write(Length);
            bw.Write(Tag);
            bw.Write(Name.Length + 1);
            bw.Write(Entries.Length);
            bw.Write(Hash);
            if (ggpkContainer.ggpkRecord.GGPKVersion == 4) {
                bw.Write(Encoding.UTF32.GetBytes(Name));
                bw.Write(0); // Null terminator
            } else {
                bw.Write(Encoding.Unicode.GetBytes(Name));
                bw.Write((short)0); // Null terminator
            }
            foreach (var entry in Entries)
            {
                bw.Write(entry.EntryNameHash);
                bw.Write(entry.Offset);
            }
        }
    }

    /// <summary>
    /// Use to sort the children of directory.
    /// </summary>
    public class SortComp : IComparer<RecordTreeNode>
    {
        [System.Runtime.InteropServices.DllImport("shlwapi.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string x, string y);
        public virtual int Compare(RecordTreeNode x, RecordTreeNode y)
        {
            if (x is DirectoryRecord || x is BundleDirectoryNode)
                if (y is DirectoryRecord || y is BundleDirectoryNode)
                    return StrCmpLogicalW(x.Name, y.Name);
                else
                    return -1;
            else
                if (y is DirectoryRecord || y is BundleDirectoryNode)
                    return 1;
                else
                    return StrCmpLogicalW(x.Name, y.Name);
        }
    }
}