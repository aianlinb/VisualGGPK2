using System.IO;

namespace LibDat2 {
	public class KeyType { // RowType

		public const uint Nullx32Key = 0xFEFEFEFE;
		public const ulong Nullx64Key = 0xFEFEFEFEFEFEFEFE;

		public bool Foreign;
		public ulong? Key1;
		public ulong? Key2;

		public KeyType(bool Foreign, ulong? Key1, ulong? Key2) {
			this.Foreign = Foreign;
			this.Key1 = Key1;
			this.Key2 = Key2;
		}

		public override string ToString() {
			return Foreign ? $"<{Key1?.ToString() ?? "null"}, { Key2?.ToString() ?? "null"}>" : $"<{Key1?.ToString() ?? "null"}>";
		}

		public void Write(BinaryWriter writer, bool x64) {
			if (x64) {
				if (Foreign) {
					writer.Write(Key1 ?? Nullx64Key);
					writer.Write(Key2 ?? Nullx64Key);
				} else
					writer.Write(Key1 ?? Nullx64Key);
			} else {
				if (Foreign) {
					writer.Write((uint?)Key1 ?? Nullx32Key);
					writer.Write((uint?)Key2 ?? Nullx32Key);
				} else
					writer.Write((uint?)Key1 ?? Nullx32Key);
			}
		}
	}
}