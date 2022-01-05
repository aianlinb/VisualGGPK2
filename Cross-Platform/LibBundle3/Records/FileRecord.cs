using System;
using System.Runtime.CompilerServices;

namespace LibBundle3.Records {
	public class FileRecord {
		public ulong PathHash;
		public int BundleIndex;
		public int Offset;
		public int Size;

		public BundleRecord BundleRecord;
		public DirectoryRecord DirectoryRecord;
		public string Path;

#pragma warning disable CS8618
		public FileRecord(ulong pathHash, int bundleIndex, int offset, int size) {
			PathHash = pathHash;
			BundleIndex = bundleIndex;
			Offset = offset;
			Size = size;
		}

		public virtual Span<byte> Read() {
			return BundleRecord.Bundle.ReadData(Offset, Size);
		}

		public virtual void Write(ReadOnlySpan<byte> newContent) {
			var b = BundleRecord.Bundle.ReadData();
			Offset = BundleRecord.ValidSize;
			Size = newContent.Length;
			BundleRecord.ValidSize += Size;
			var b2 = new byte[BundleRecord.ValidSize];
			Unsafe.CopyBlockUnaligned(ref b2[0], ref b[0], (uint)Offset);
			newContent.CopyTo(b2.AsSpan()[Offset..Size]);
			BundleRecord.Bundle.SaveData(b2);
			BundleRecord.Index.Save();
		}

		public virtual void Redirect(BundleRecord bundle, int offset, int size) {
			if (Offset + Size >= BundleRecord.ValidSize)
				BundleRecord.ValidSize = Offset;
			BundleRecord = bundle;
			BundleIndex = bundle.BundleIndex;
			Offset = offset;
			Size = size;
		}
	}
}