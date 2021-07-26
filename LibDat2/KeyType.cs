using System.IO;
using System.Text.RegularExpressions;

namespace LibDat2 {
	public class KeyType { // RowType

		public const uint Nullx32Key = 0xFEFEFEFE;
		public const ulong Nullx64Key = 0xFEFEFEFEFEFEFEFE;

		public bool Foreign;
		public ulong? Key1;
		/// <summary>
		/// Ignored if the key is not <see cref="Foreign"/>
		/// </summary>
		public ulong? Key2;

		public KeyType(bool Foreign, ulong? Key1, ulong? Key2) {
			this.Foreign = Foreign;
			this.Key1 = Key1;
			this.Key2 = Key2;
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

		public override string ToString() {
			return Foreign ? $"<{Key1?.ToString() ?? "null"}, { Key2?.ToString() ?? "null"}>" : $"<{Key1?.ToString() ?? "null"}>";
		}

		public static readonly Regex ForeignKeyRegex = new(@"^\s*<\s*(.+?)\s*,\s*(.+?)\s*>\s*$");
		public static readonly Regex KeyRegex = new(@"^\s*<\s*(.+?)\s*>\s*$");
		public static KeyType FromString(string value) {
			var m1 = ForeignKeyRegex.Match(value);
			if (m1.Success) {
				ulong? key1;
				ulong? key2;
				if (m1.Groups[1].Value.ToLower() == "null")
					key1 = null;
				else if (ulong.TryParse(m1.Groups[1].Value, out var l))
					key1 = l;
				else
					return null;
				if (m1.Groups[2].Value.ToLower() == "null")
					key2 = null;
				else if (ulong.TryParse(m1.Groups[2].Value, out var l))
					key2 = l;
				else
					return null;
				return new(true, key1, key2);
			}
			var m2 = KeyRegex.Match(value);
			if (m2.Success) {
				ulong? key;
				if (m2.Groups[1].Value.ToLower() == "null")
					key = null;
				else if (ulong.TryParse(m2.Groups[1].Value, out var l))
					key = l;
				else
					return null;
				return new(false, key, null);
			}
			return null;
		}
	}
}