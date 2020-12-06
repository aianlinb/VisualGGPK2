using System;
using System.IO;

namespace LibDat2.Types {
	public class FieldType {

		public readonly bool x64;

		public virtual object Value { get; set; }

		internal Type Type;

		protected FieldType(bool x64) {
			this.x64 = x64;
		}

		public static FieldType Create(string type, BinaryReader reader, DatContainer dat) {
			if (type.StartsWith("x64|"))
				return dat.x64 ? Create(type[4..], reader, dat) : Null;
			else if (type.StartsWith("x32|"))
				return dat.x64 ? Null : Create(type[4..], reader, dat);
			else if (type == "ref|key")
				return new KeyType(dat.x64, reader, false);
			else if (type == "ref|foreignkey")
				return new KeyType(dat.x64, reader, true);
			else if (type.StartsWith("x32ref|"))
				return new PointerType(reader, false, dat, type[7..]);
			else if (type.StartsWith("ref|"))
				return new PointerType(reader, dat.x64, dat, type[4..]);
			return type switch {
				"bool" => new ValueType<bool>(dat.x64, reader),
				"byte" => new ValueType<byte>(dat.x64, reader),
				"short" => new ValueType<short>(dat.x64, reader),
				"int" => new ValueType<int>(dat.x64, reader),
				"uint" => new ValueType<uint>(dat.x64, reader),
				"float" => new ValueType<float>(dat.x64, reader),
				"long" => new ValueType<long>(dat.x64, reader),
				"ulong" => new ValueType<ulong>(dat.x64, reader),
				"string" => new ValueType<string>(dat.x64, reader),
				_ => throw new InvalidCastException($"Unknown Type: {type}"),
			};
		}

		public static long? TypeLength(string type, bool x64) {
			if (type.StartsWith("x64|"))
				return x64 ? TypeLength(type[4..], x64) : 0;
			else if (type.StartsWith("x32|"))
				return x64 ? 0 : TypeLength(type[4..], x64);
			else if (type.StartsWith("ref|list|"))
				return x64 ? 16 : 8;
			else if (type == "ref|foreignkey")
				return x64 ? 16 : 8;
			else if (type.StartsWith("x32ref|"))
				return 4;
			else if (type.StartsWith("ref|"))
				return x64 ? 8 : 4;
			else return type switch {
				"bool" => 1,
				"byte" => 1,
				"short" => 2,
				"int" => 4,
				"uint" => 4,
				"float" => 4,
				"long" => 8,
				"ulong" => 8,
				"string" => null,
				"key" => x64 ? 8 : 4,
				"foreignkey" => x64 ? 16 : 8,
				_ => throw new InvalidCastException($"Unknown Type: {type}")
			};
		}

		private FieldType() { }
		public static FieldType Null = new();
	}
}