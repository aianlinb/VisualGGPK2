using LibDat2.Types;
using System.Runtime.InteropServices;
using System.Text;

namespace LibDat2 {
	public record PointedValue {
		public long Offset { get; }

		public int Length { get; }

		public long EndOffset { get => Offset + Length; }

		public object Value { get => FieldValue.Value; }

		public readonly FieldType FieldValue;

		public PointedValue(long Offset, FieldType Value, bool UTF32 = false) {
			this.Offset = Offset;
			FieldValue = Value;
			Length = GetLength(Value, UTF32);
		}

		protected static int GetLength(FieldType type, bool UTF32 = false) {
			if (type is ListType lt) {
				if (lt.Values.Count > 0)
					return GetLength(lt.Values[0]) * lt.Values.Count;
				return 0;
			}else if (type is PointerType p)
				return p.x64 ? 8 : 4;
			else if (type.Value is null)
				return 0;
			else if (type is ValueType<string> s)
				return (UTF32 ? Encoding.UTF32.GetByteCount(s.Value) : Encoding.Unicode.GetByteCount(s.Value)) + 4;
			else if (type is KeyType k) {
				if (k.EOF1)
					return 0;
				return 4 * (k.Foreign && !k.EOF2 ? 2 : 1) * (k.x64 ? 2 : 1);
			}
			return Marshal.SizeOf(type.Value);
		}

		public override string ToString() => FieldValue.Value.ToString();
	}
}