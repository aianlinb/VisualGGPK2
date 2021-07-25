using System.Text;
using System;

namespace LibDat2 {
	public record PointedValue {
		/// <summary>
		/// Position of data in DataSection
		/// </summary>
		public long Offset { get; }

		/// <summary>
		/// Length of data in DataSection
		/// </summary>
		public int Length { get; }

		/// <summary>
		/// Position of end of data in DataSection
		/// </summary>
		public long EndOffset { get => Offset + Length; }

		/// <summary>
		/// The actual data in DataSection
		/// </summary>
		public object Value { get; }

		/// <summary>
		/// Create a PointedValue
		/// This won't calculate the actual length of the value
		/// </summary>
		public PointedValue(long Offset, int Length, object Value) {
			this.Offset = Offset;
			this.Length = Length;
			this.Value = Value;
		}

		/// <summary>
		/// Create a PointedValue and calculate the length of the value
		/// </summary>
		public PointedValue(long Offset, object Value, string type, bool x64, bool UTF32 = false) {
			this.Offset = Offset;
			if (Offset == 0) {
				this.Value = Array.Empty<object>();
				Length = 0;
				return;
			}
			this.Value = Value;
			Length = GetLength(Value, type, x64, UTF32);
		}

		/// <summary>
		/// Calculate the length of the value
		/// </summary>
		protected static int GetLength(object value, string type, bool x64, bool UTF32 = false) {
			if (value is null)
				return 0;
			else if (type.StartsWith("array|")) {
				var a = value as object[];
				if (a.Length > 0)
					return DatContainer.FieldTypeLength(type[6..], x64) * a.Length;
				return 0;
			} else if (type == "string")
				return (UTF32 ? Encoding.UTF32.GetByteCount((string)value) : Encoding.Unicode.GetByteCount((string)value)) + 4;
			else
				return DatContainer.FieldTypeLength(type, x64);
		}

		/// <summary>
		/// <see cref="Value"/>.ToString()
		/// </summary>
		public override string ToString() => Value.ToString();

		/// <summary>
		/// <see cref="Array.Empty"/>
		/// </summary>
		public static readonly object[] NullArray = Array.Empty<object>();
	}
}