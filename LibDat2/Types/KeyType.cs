using System.IO;

namespace LibDat2.Types {
	class KeyType : FieldType {

		public const ulong Nullx32Key = 0xFEFEFEFE;
		public const ulong Nullx64Key = 0xFEFEFEFEFEFEFEFE;

		public override object Value { 
			get {
				if (Foreign) {
					var p1 = Pointer1Value == (x64 ? Nullx64Key : Nullx32Key) ? "null" : Pointer1Value.ToString();
					var p2 = Pointer2Value == (x64 ? Nullx64Key : Nullx32Key) ? "null" : Pointer2Value.ToString();
					return p1 + ", " + p2;
				} else if (Pointer1Value == (x64 ? Nullx64Key : Nullx32Key))
					return "null";
				else
					return Pointer1Value;
			}
		}

		public ulong Pointer1Value;
		public ulong Pointer2Value;

		public bool Foreign;

		public KeyType(bool x64, BinaryReader Reader, bool Foreign) : base(x64) {
			this.Foreign = Foreign;
			Pointer1Value = x64 ? Reader.ReadUInt64() : Reader.ReadUInt32();
			if (Foreign) Pointer2Value = x64 ? Reader.ReadUInt64() : Reader.ReadUInt32();
			Type = typeof(ulong);
		}
	}
}