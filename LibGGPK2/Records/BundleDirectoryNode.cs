using System;
using System.Collections.Generic;
using System.IO;

namespace LibGGPK2.Records
{
    public class BundleDirectoryNode : RecordTreeNode
    {
        public new ulong Hash;

        public override SortedSet<RecordTreeNode> Children { get; }

        public BundleDirectoryNode(string name, ulong hash, int offset, int size, GGPKContainer ggpkContainer)
        {
            Name = name;
            Hash = hash;
            Offset = offset;
            Length = size;
            this.ggpkContainer = ggpkContainer;
            Children = new SortedSet<RecordTreeNode>(DirectoryRecord.Comparer);
        }

        protected override void Read()
        {
            throw new NotSupportedException("A virtual node of bundles cannot be read");
        }
        internal override void Write(BinaryWriter bw = null)
        {
            throw new NotSupportedException("A virtual node of bundles cannot be written");
        }
    }
}
