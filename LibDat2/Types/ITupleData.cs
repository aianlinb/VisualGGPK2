using System;
using System.IO;

namespace LibDat2.Types {
	public interface ITupleData : IFieldData {
		/// <summary>
		/// FieldType of value in array
		/// </summary>
		public string TypeOfValue { get; }

		/// <summary>
		/// Read a <see cref="IArrayData"/> from a dat file
		/// </summary>
		public static new ITupleData Read(BinaryReader reader, string typeOfValue, DatContainer dat) {
			if (typeOfValue.StartsWith("array|"))
				return new TupleData<IArrayData>(dat, typeOfValue).Read(reader);
			if (typeOfValue.StartsWith("tuple|"))
				return new TupleData<ITupleData>(dat, typeOfValue).Read(reader);
			return typeOfValue switch {
				"bool"			=> new TupleData<bool>(dat, typeOfValue).Read(reader),
				"i8"			=> new TupleData<sbyte>(dat, typeOfValue).Read(reader),
				"i16"			=> new TupleData<short>(dat, typeOfValue).Read(reader),
				"i32"			=> new TupleData<int>(dat, typeOfValue).Read(reader),
				"i64"			=> new TupleData<long>(dat, typeOfValue).Read(reader),
				"u8"			=> new TupleData<byte>(dat, typeOfValue).Read(reader),
				"u16"			=> new TupleData<ushort>(dat, typeOfValue).Read(reader),
				"u32"			=> new TupleData<uint>(dat, typeOfValue).Read(reader),
				"u64"			=> new TupleData<ulong>(dat, typeOfValue).Read(reader),
				"f32"			=> new TupleData<float>(dat, typeOfValue).Read(reader),
				"f64"			=> new TupleData<double>(dat, typeOfValue).Read(reader),
				"row"			=> new TupleData<RowData>(dat, typeOfValue).Read(reader),
				"foreignrow"	=> new TupleData<ForeignRowData>(dat, typeOfValue).Read(reader),
				"string"		=> new TupleData<StringData>(dat, typeOfValue).Read(reader),
				"valuestring"	=> new TupleData<ValueStringData>(dat, typeOfValue).Read(reader),
				_ => throw new InvalidCastException("Unknown Type: " + typeOfValue)
			};
		}

		/// <summary>
		/// Create an instance of <see cref="IFieldData"/> from its value in string representation
		/// </summary>
		public static new ITupleData FromString(string value, string typeOfValue, DatContainer dat) {
			if (typeOfValue.StartsWith("array|"))
				return new TupleData<IArrayData>(dat, typeOfValue).FromString(value);
			if (typeOfValue.StartsWith("tuple|"))
				return new TupleData<ITupleData>(dat, typeOfValue).FromString(value);
			return typeOfValue switch {
				"bool"			=> new TupleData<bool>(dat, typeOfValue).FromString(value),
				"i8"			=> new TupleData<sbyte>(dat, typeOfValue).FromString(value),
				"i16"			=> new TupleData<short>(dat, typeOfValue).FromString(value),
				"i32"			=> new TupleData<int>(dat, typeOfValue).FromString(value),
				"i64"			=> new TupleData<long>(dat, typeOfValue).FromString(value),
				"u8"			=> new TupleData<byte>(dat, typeOfValue).FromString(value),
				"u16"			=> new TupleData<ushort>(dat, typeOfValue).FromString(value),
				"u32"			=> new TupleData<uint>(dat, typeOfValue).FromString(value),
				"u64"			=> new TupleData<ulong>(dat, typeOfValue).FromString(value),
				"f32"			=> new TupleData<float>(dat, typeOfValue).FromString(value),
				"f64"			=> new TupleData<double>(dat, typeOfValue).FromString(value),
				"row"			=> new TupleData<RowData>(dat, typeOfValue).FromString(value),
				"foreignrow"	=> new TupleData<ForeignRowData>(dat, typeOfValue).FromString(value),
				"string"		=> new TupleData<StringData>(dat, typeOfValue).FromString(value),
				"valuestring"	=> new TupleData<ValueStringData>(dat, typeOfValue).FromString(value),
				_ => throw new InvalidCastException("Unknown Type: " + typeOfValue)
			};
		}
	}
}