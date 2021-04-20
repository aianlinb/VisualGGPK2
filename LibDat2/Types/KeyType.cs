using System.IO;

namespace LibDat2.Types {
	class KeyType : FieldType {

		public const ulong Nullx32Key = 0xFEFEFEFE;
		public const ulong Nullx64Key = 0xFEFEFEFEFEFEFEFE;

		public override object Value { 
			get {
				if (Foreign) {
					var p1 = Pointer1Value == (x64 ? Nullx64Key : Nullx32Key) ? "null" : Pointer1Value.ToString();
					if (EOF1)
						p1 = "EOF";
					var p2 = Pointer2Value == (x64 ? Nullx64Key : Nullx32Key) ? "null" : Pointer2Value.ToString();
					if (EOF2)
						p2 = "EOF";
					return p1 + ", " + p2;
				} else if (Pointer1Value == (x64 ? Nullx64Key : Nullx32Key))
					return "null";
				else
					return EOF1 ? "EOF" : Pointer1Value.ToString();
			}
		}

		public ulong Pointer1Value;
		public ulong Pointer2Value;
		internal protected bool EOF1;
		internal protected bool EOF2;

		public bool Foreign;

		public KeyType(bool x64, BinaryReader Reader, bool Foreign) : base(x64) {
			this.Foreign = Foreign;

			if (Reader.BaseStream.Position == Reader.BaseStream.Length)
				EOF1 = true;
			else
				Pointer1Value = x64 ? Reader.ReadUInt64() : Reader.ReadUInt32();
			if (Foreign)
				if (Reader.BaseStream.Position == Reader.BaseStream.Length)
					EOF2 = true;
				else
					Pointer2Value = x64 ? Reader.ReadUInt64() : Reader.ReadUInt32();
		}
	}
}