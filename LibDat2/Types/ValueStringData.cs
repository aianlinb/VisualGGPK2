using System;
using System.IO;
using System.Text;
using static LibDat2.Types.IFieldData;

namespace LibDat2.Types {
	[FieldType(FieldType.ValueString)]
	public class ValueStringData : FieldDataBase<string> {
		public ValueStringData(DatContainer dat) : base(dat) { }

		/// <inheritdoc/>
		public unsafe override void Write(BinaryWriter writer) {
			if (Value != null)
				if (Dat.UTF32)
					writer.Write(Encoding.UTF32.GetBytes(Value));
				else
					fixed (char* c = Value) { // string is stored in UTF-16 in C#, so we can directly get its bytes for writing
						var p = (byte*)c;
						writer.BaseStream.Write(new ReadOnlySpan<byte>(p, Value.Length * 2));
					}
			writer.Write(0); // \0 at the end of string
		}

		/// <inheritdoc/>
		public override void Read(BinaryReader reader) {
			if (reader.BaseStream.Position == reader.BaseStream.Length) {
				Value = null;
				return;
			}
			var sb = new StringBuilder();
			if (Dat.UTF32) {
				int ch;
				while ((ch = reader.ReadInt32()) != 0)
					sb.Append(char.ConvertFromUtf32(ch));
			} else {
				char ch;
				for (;;) {
					try {
						ch = reader.ReadChar();
						if (ch == 0)
							break;
						sb.Append(ch);
					} catch (ArgumentException) {
						sb.Append(reader.ReadChars(2));
					}
				}
				try {
					if (reader.ReadChar() != 0)  // string should end with 4 bytes of zero
						throw new("Not found \\0 at the end of the string");
				} catch (ArgumentException) {
					throw new("Not found \\0 at the end of the string");
				}
			}
			Value = sb.ToString();
		}

		/// <inheritdoc/>
		public override void FromString(string value) {
			Value = value;
		}
	}
}