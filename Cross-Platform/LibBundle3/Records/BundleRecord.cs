using System;
using System.Collections.Generic;

namespace LibBundle3.Records {
	public class BundleRecord {
		public int PathLength;
		public string Path; // without extension
		public int UncompressedSize;

		internal int BundleIndex;
		internal int ValidSize;
		public readonly List<FileRecord> Files = new();
		public Index Index;

		protected Bundle? _Bundle;
		public virtual Bundle Bundle {
			get {
				_Bundle ??= Index.FuncReadBundle(this);
				return _Bundle;
			}
		}

		public BundleRecord(int nameLength, string name, int uncompressedSize, Index index) {
			PathLength = nameLength;
			Path = name;
			UncompressedSize = uncompressedSize;
			Index = index;
		}
	}
}