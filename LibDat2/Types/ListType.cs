using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibDat2.Types {
	public class ListType : FieldType {

		public List<FieldType> Values;

		public override object Value {
			get {
				var s = new StringBuilder("[");
				foreach (var f in Values) {
					s.Append(f is KeyType kt && kt.Foreign ? $"({f.Value})" : f.Value);
					s.Append(", ");
				}
				if (s.Length > 2) s.Remove(s.Length - 2, 2);
				s.Append(']');
				return s.ToString();
			}
		}

		public ListType(BinaryReader Reader, DatContainer dat, string ListDataType, long Count) : base(dat?.x64 ?? false) {
			Values = new((int)Count);
			for (var i = 0L; i < Count; i++) {
				var t = Create(ListDataType, Reader, dat);
				if (t is KeyType k && (k.EOF1 || k.EOF2))
					throw new EndOfStreamException();
				Values.Add(t);
			}
		}
	}
}