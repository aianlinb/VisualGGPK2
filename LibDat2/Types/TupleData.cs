using System;
using System.IO;
using System.Text.RegularExpressions;

namespace LibDat2.Types {
#pragma warning disable CS8612
	public class TupleData<TypeOfValueInTuple> : FieldDataBase<(TypeOfValueInTuple, TypeOfValueInTuple)>, ITupleData where TypeOfValueInTuple : notnull {
		/// <summary>
		/// FieldType of value in tuple
		/// </summary>
		public string TypeOfValue { get; }

		public TupleData(DatContainer dat, string typeOfValue) : base(dat) {
			TypeOfValue = typeOfValue;
		}

		/// <inheritdoc/>
		public override TupleData<TypeOfValueInTuple> Read(BinaryReader reader) {
			object value;
			if (TypeOfValue.StartsWith("array|"))
				value = (IArrayData.Read(reader, TypeOfValue[6..], Dat), IArrayData.Read(reader, TypeOfValue[6..], Dat));
			else if (TypeOfValue.StartsWith("tuple|"))
				value = (ITupleData.Read(reader, TypeOfValue[6..], Dat), ITupleData.Read(reader, TypeOfValue[6..], Dat));
			else
				value = TypeOfValue switch {
				"bool"			=> (reader.ReadBoolean(), reader.ReadBoolean()),
				"i8"			=> (reader.ReadSByte(), reader.ReadSByte()),
				"i16"			=> (reader.ReadInt16(), reader.ReadInt16()),
				"i32"			=> (reader.ReadInt32(), reader.ReadInt32()),
				"i64"			=> (reader.ReadInt64(), reader.ReadInt64()),
				"u8"			=> (reader.ReadByte(), reader.ReadByte()),
				"u16"			=> (reader.ReadUInt16(), reader.ReadUInt16()),
				"u32"			=> (reader.ReadUInt32(), reader.ReadUInt32()),
				"u64"			=> (reader.ReadUInt64(), reader.ReadUInt64()),
				"f32"			=> (reader.ReadSingle(), reader.ReadSingle()),
				"f64"			=> (reader.ReadDouble(), reader.ReadDouble()),
				"row"			=> (new RowData(Dat).Read(reader), new RowData(Dat).Read(reader)),
				"foreignrow"	=> (new ForeignRowData(Dat).Read(reader), new ForeignRowData(Dat).Read(reader)),
				"string"		=> (StringData.Read(reader, Dat).Value, StringData.Read(reader, Dat).Value),
				"valuestring"	=> (new ValueStringData(Dat).Read(reader), new ValueStringData(Dat).Read(reader)),
				_ => throw new InvalidCastException("Unknown Type: " + Value)
			};
			Value = ((TypeOfValueInTuple, TypeOfValueInTuple))value;
			return this;
		}

		/// <inheritdoc/>
		public override void Write(BinaryWriter writer) {
			switch (TypeOfValue) {
				case "bool":
					writer.Write((bool)(object)Value.Item1);
					writer.Write((bool)(object)Value.Item2);
					break;
				case "i8":
					writer.Write((sbyte)(object)Value.Item1);
					writer.Write((sbyte)(object)Value.Item2);
					break;
				case "i16":
					writer.Write((short)(object)Value.Item1);
					writer.Write((short)(object)Value.Item2);
					break;
				case "i32":
					writer.Write((int)(object)Value.Item1);
					writer.Write((int)(object)Value.Item2);
					break;
				case "i64":
					writer.Write((long)(object)Value.Item1);
					writer.Write((long)(object)Value.Item2);
					break;
				case "u8":
					writer.Write((byte)(object)Value.Item1);
					writer.Write((byte)(object)Value.Item2);
					break;
				case "u16":
					writer.Write((ushort)(object)Value.Item1);
					writer.Write((ushort)(object)Value.Item2);
					break;
				case "u32":
					writer.Write((bool)(object)Value.Item1);
					writer.Write((bool)(object)Value.Item2);
					break;
				case "u64":
					writer.Write((uint)(object)Value.Item1);
					writer.Write((uint)(object)Value.Item2);
					break;
				case "f32":
					writer.Write((float)(object)Value.Item1);
					writer.Write((float)(object)Value.Item2);
					break;
				case "f64":
					writer.Write((double)(object)Value.Item1);
					writer.Write((double)(object)Value.Item2);
					break;
				default:
					((IFieldData)Value.Item1).Write(writer);
					((IFieldData)Value.Item2).Write(writer);
					break;
			}
		}

		/// <inheritdoc/>
		public override TupleData<TypeOfValueInTuple> FromString(string value) {
			if (!TypeOfValue.EndsWith("string"))
				value = Regex.Replace(value.Trim(), @"\s", "");
			if (!value.StartsWith('(') || !value.EndsWith(')'))
				throw new InvalidCastException("\"" + value + "\" cannot be converted to an tuple");

			if (TypeOfValue == "foreignrow") {
				value = value[1..^1]; // Trim '(' ')'
				if (!value.StartsWith('<') || !value.EndsWith('>'))
					throw new InvalidCastException("\"(" + value + "\") cannot be converted to an tuple of ForeignRowData");
				var sarray = value[1..^1].Split(">,<"); // Trim '<' '>'
				if (sarray.Length != 2)
					throw new InvalidCastException("A tuple must have exactly 2 elements: \"(" + value + ")\"");
				var a = new ForeignRowData(Dat).FromString("<" + sarray[0] + ">");
				var b = new ForeignRowData(Dat).FromString("<" + sarray[1] + ">");
				Value = ((TypeOfValueInTuple)(object)a, (TypeOfValueInTuple)(object)b);
			} else if (TypeOfValue.StartsWith("tuple")) {
				value = value[1..^1]; // Trim '(' ')'
				if (!value.StartsWith('(') || !value.EndsWith(')'))
					throw new InvalidCastException("\"(" + value + "\") cannot be converted to an tuple of tuple");
				var sarray = value[1..^1].Split("),("); // Trim '<' '>'
				if (sarray.Length != 2)
					throw new InvalidCastException("A tuple must have exactly 2 elements: \"(" + value + ")\"");
				var a = ITupleData.FromString("(" + sarray[0] + ")", TypeOfValue[6..], Dat);
				var b = ITupleData.FromString("(" + sarray[1] + ")", TypeOfValue[6..], Dat);
				Value = ((TypeOfValueInTuple)(object)a, (TypeOfValueInTuple)(object)b);
			} else if (TypeOfValue.StartsWith("array|")) {
				throw new InvalidOperationException("Parsing tuple of array is not implemented");
			} else {
				var sarray = value[1..^1].Split(','); // Trim '(' ')'
				if (sarray.Length != 2)
					throw new InvalidCastException("A tuple must have exactly 2 elements: \"" + value + "\"");
				if (TypeOfValue.EndsWith("string"))
					Value = ((TypeOfValueInTuple)IFieldData.FromString(sarray[0], TypeOfValue, Dat), (TypeOfValueInTuple)IFieldData.FromString(sarray[1], TypeOfValue, Dat));
				else
					Value = ((TypeOfValueInTuple)IFieldData.FromString(sarray[0], TypeOfValue, Dat).Value, (TypeOfValueInTuple)IFieldData.FromString(sarray[1], TypeOfValue, Dat).Value);
			}
			return this;
		}
	}
}