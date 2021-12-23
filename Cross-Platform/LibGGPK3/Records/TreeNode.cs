namespace LibGGPK3.Records {
	public abstract class TreeNode : BaseRecord {
		/// <summary>
		/// File/Directory name
		/// </summary>
		public string Name;
		/// <summary>
		/// SHA256 hash of the file content
		/// </summary>
		public byte[] Hash;
		/// <summary>
		/// Parent node
		/// </summary>
		public TreeNode? Parent;

#pragma warning disable CS8618
		protected TreeNode(int length, GGPK ggpk) : base(length, ggpk) {
		}

		/// <summary>
		/// Get the full path in GGPK of this File/Directory
		/// </summary>
		public virtual string GetPath() {
			return this is FileRecord ? Parent?.GetPath() ?? "" + Name : Parent?.GetPath() ?? "" + Name + "/";
		}

		/// <summary>
		/// Get the murmur hash of name of this File/Directory
		/// </summary>
		public virtual uint GetNameHash() {
			return MurmurHash2Unsafe.Hash(Name.ToLower(), 0);
		}
	}
}