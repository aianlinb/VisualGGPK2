using LibDat2.Types;
using System.Runtime.InteropServices;

namespace LibDat2 {
	public record PointedValue {
		public long Offset { get; }

		public int Length { get; }

		public object EndOffset { get => Offset + Length; }

		public object Value { get => FieldValue.Value; }

		public readonly FieldType FieldValue;

		public PointedValue(long Offset, FieldType Value) {
			this.Offset = Offset;
			FieldValue = Value;
			if (Value is ListType lt && lt.Values.Count > 0 && lt.Values[0].Value != null)
				Length = GetLength(lt.Values[0].Value) * lt.Values.Count;
			else
				Length = GetLength(Value.Value);
		}

		private static int GetLength(object value) {
			if (value is string s)
				return (s.Length + 2) * 2;
			else if (value is null)
				return 0;
			return Marshal.SizeOf(value);
		}

		public override string ToString() => FieldValue.Value.ToString();
	}
}