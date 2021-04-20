using System;
using System.IO;
using System.Text;

namespace LibDat2.Types {
	public class ValueType<T> : FieldType {

		public virtual new T Value { get => (T)base.Value; set => base.Value = value; }

		public ValueType(bool x64, BinaryReader Reader, bool UTF32 = false) : base(x64) {
			switch (default(T)) {
				case bool:
					base.Value = Reader.ReadBoolean();
					break;
				case byte:
					base.Value = Reader.ReadByte();
					break;
				case short:
					base.Value = Reader.ReadInt16();
					break;
				case int:
					base.Value = Reader.ReadInt32();
					break;
				case uint:
					base.Value = Reader.ReadUInt32();
					break;
				case float:
					base.Value = Reader.ReadSingle();
					break;
				case long:
					base.Value = Reader.ReadInt64();
					break;
				case ulong:
					base.Value = Reader.ReadUInt64();
					break;
				case null:
					base.Value = ReadString(Reader, UTF32);
					break;
			}
		}

		public static string ReadString(BinaryReader reader, bool UTF32) {
			if (reader.BaseStream.Position == reader.BaseStream.Length)
				return null;
			var sb = new StringBuilder();
			char ch;
			while ((ch = reader.ReadChar()) != 0) { sb.Append(ch); }
			if (!(UTF32 || reader.ReadChar() == 0))  // string should end with 4 bytes of zero
				throw new Exception("Not found \\0 at the end of the string");
			return sb.ToString();
		}

		protected ValueType(bool x64, object value) : base(x64) {
			base.Value = value;
		}

		public static FieldType FromValue(bool x64, object value) {
			return value switch {
				bool => new ValueType<bool>(x64, value),
				byte => new ValueType<byte>(x64, value),
				short => new ValueType<short>(x64, value),
				int => new ValueType<int>(x64, value),
				uint => new ValueType<uint>(x64, value),
				float => new ValueType<float>(x64, value),
				long => new ValueType<long>(x64, value),
				ulong => new ValueType<ulong>(x64, value),
				string => new ValueType<string>(x64, value),
				_ => throw new InvalidCastException($"Unknown ValueType: {value}"),
			};
		}
	}
}