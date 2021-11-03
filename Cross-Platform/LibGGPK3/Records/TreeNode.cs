namespace LibGGPK3.Records {
	public abstract class TreeNode : BaseRecord {
        /// <summary>
        /// SHA256 hash of the file content
        /// </summary>
        public byte[] Hash;
        /// <summary>
        /// File/Directory name
        /// </summary>
        public string Name;
        /// <summary>
        /// Parent node
        /// </summary>
        public TreeNode Parent;

        /// <summary>
        /// Get the full path in GGPK of this File/Directory
        /// </summary>
        public virtual string GetPath() {
            return this is FileRecord ? Parent?.GetPath() + Name : Parent?.GetPath() + Name + "/";
        }

        public virtual uint GetNameHash() {
            return MurmurHash2Unsafe.Hash(Name.ToLower(), 0);
        }
    }
}