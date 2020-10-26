using System;
using System.Collections.Generic;
using System.IO;

namespace LibGGPK2.Records
{
    public class BundleDirectoryNode : RecordTreeNode
    {
        /// <summary>
        /// FNV1a64Hash of the path of directory
        /// </summary>
        public new ulong Hash;
        /// <summary>
        /// Path in bundle without root directory (Bundles2)
        /// </summary>
        public string path;

        /// <summary>
        /// Files and directories this directory contains
        /// </summary>
        public override SortedSet<RecordTreeNode> Children { get; }

        /// <summary>
        /// Create a node of the directory in bundle
        /// </summary>
        public BundleDirectoryNode(string name, string path, ulong hash, int offset, int size, GGPKContainer ggpkContainer)
        {
            Name = name;
            this.path = path;
            Hash = hash;
            Offset = offset;
            Length = size;
            this.ggpkContainer = ggpkContainer;
            Children = new SortedSet<RecordTreeNode>(DirectoryRecord.Comparer);
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
    }
}
