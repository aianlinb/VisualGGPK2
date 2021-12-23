using System.Collections.Generic;
using System.Linq;

namespace LibGGPK2.Records
{
    /// <summary>
    /// FileRecord or DirectoryRecord or BundleFileNode or BundleDirectoryNode
    /// </summary>
    public abstract class RecordTreeNode : BaseRecord
    {
        public virtual RecordTreeNode Parent { get; internal set; }
        public virtual SortedSet<RecordTreeNode> Children { get; }

        /// <summary>
        /// SHA256 hash of the file content
        /// </summary>
        public byte[] Hash;
        /// <summary>
        /// File/Directory name
        /// </summary>
        public string Name;

        /// <summary>
        /// Get the full path in GGPK of this File/Directory
        /// </summary>
        public virtual string GetPath()
        {
            return this is IFileRecord ? Parent?.GetPath() ?? "" + Name : Parent?.GetPath() ?? "" + Name + "/";
        }

        /// <param name="name">Name of record</param>
        /// <returns>A record with <see cref="Name"/> == <paramref name="name"/></returns>
        public virtual RecordTreeNode GetChildItem(string name)
        {
            return Children?.FirstOrDefault(rtn => rtn.Name == name);
        }

        public virtual uint GetNameHash()
        {
            return MurmurHash2Unsafe.Hash(Name.ToLower(), 0);
        }
    }
}