using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibGGPK3.Records {
    public class DirectoryRecord : TreeNode {
        public struct DirectoryEntry {
            /// <summary>
            /// Murmur2 hash of lowercase entry name
            /// </summary>
            public uint EntryNameHash;
            /// <summary>
            /// Offset in pack file where the record begins
            /// </summary>
            public long Offset;

            public DirectoryEntry(uint entryNameHash, long offset) {
                EntryNameHash = entryNameHash;
                Offset = offset;
            }
        }

        public static readonly byte[] Tag = Encoding.ASCII.GetBytes("PDIR");
        /// <summary>
        /// Use for sort the children of directory.
        /// </summary>
        private static readonly SortComparer Comparer = new();

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
        public DirectoryRecord(int length, GGPKContainer ggpk) {
            GGPK = ggpk;
            Offset = ggpk.FileStream.Position - 8;
            Length = length;
            Read();
        }

        private SortedSet<TreeNode> _Children;
        public virtual SortedSet<TreeNode> Children {
            get {
                if (_Children == null) {
                    _Children = new SortedSet<TreeNode>(Comparer);
                    foreach (var e in Entries) {
                        var b = GGPK.GetRecord(e.Offset) as TreeNode;
                        b.Parent = this;
                        _Children.Add(b);
                    }
                }
                return _Children;
            }
        }

        /// <param name="name">Name of record</param>
        /// <returns>A record with <see cref="Name"/> == <paramref name="name"/></returns>
        public virtual TreeNode GetChildItem(string name) {
            return Children.FirstOrDefault(rtn => rtn.Name == name);
        }

        protected override void Read() {
            var br = GGPK.Reader;
            var nameLength = br.ReadInt32();
            var totalEntries = br.ReadInt32();

            Hash = br.ReadBytes(32);
            Name = Encoding.Unicode.GetString(br.ReadBytes(2 * (nameLength - 1)));
            br.BaseStream.Seek(2, SeekOrigin.Current); // Null terminator

            EntriesBegin = br.BaseStream.Position;
            Entries = new DirectoryEntry[totalEntries];
            for (var i = 0; i < totalEntries; i++)
                Entries[i] = new DirectoryEntry(br.ReadUInt32(), br.ReadInt64());
        }

        protected internal override void Write(BinaryWriter bw = null) {
            bw ??= GGPK.Writer;
            Offset = bw.BaseStream.Position;
            bw.Write(Length);
bw.Write(Tag);
            bw.Write(Name.Length + 1);
            bw.Write(Entries.Length);
            bw.Write(Hash);
            bw.Write(Encoding.Unicode.GetBytes(Name));
            bw.Write((short)0); // Null terminator
            foreach (var entry in Entries) {
                bw.Write(entry.EntryNameHash);
                bw.Write(entry.Offset);
            }
        }
    }

    /// <summary>
    /// Use to sort the children of directory.
    /// </summary>
    public class SortComparer : IComparer<TreeNode> {
        //[System.Runtime.InteropServices.DllImport("shlwapi.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        //public static extern int StrCmpLogicalW(string x, string y);
        public virtual int Compare(TreeNode x, TreeNode y) {
            if (x is DirectoryRecord)
                if (y is DirectoryRecord)
                    return string.Compare(x.Name, y.Name);
                else
                    return -1;
            else
                if (y is DirectoryRecord)
                    return 1;
                else
                    return string.Compare(x.Name, y.Name);
        }
    }
}