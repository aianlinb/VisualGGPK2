using System.Collections.Generic;

namespace LibGGPK2.Records
{
    public abstract class RecordTreeNode : BaseRecord
    {
        public virtual DirectoryRecord Parent { get; internal set; }
        public virtual SortedSet<RecordTreeNode> Children { get; }

        /// <summary>
        /// SHA256 hash of this file's data
        /// </summary>
        public byte[] Hash;
        /// <summary>
        /// File name
        /// </summary>
        public string Name;

        public virtual string GetPath()
        {
            return Parent?.GetPath() + Name + "/";
        }

        public virtual uint GetNameHash()
        {
            return LibGGPK.Murmur.Hash2(Name);
        }
    }
}