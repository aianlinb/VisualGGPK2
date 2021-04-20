using System.IO;

namespace LibDat2.Types {
	public class PointerType : FieldType {

		public override object Value { get => PointTo.FieldValue.Value; }

		public long Pointer;

		public PointedValue PointTo;

		public PointerType(BinaryReader Reader, bool x64, DatContainer dat, string PointToType) : base(x64) {
			if (PointToType.StartsWith("list|")) {
				var Count = x64 ? Reader.ReadInt64() : Reader.ReadUInt32();
				Pointer = x64 ? Reader.ReadInt64() : Reader.ReadUInt32();
				if (Count > 0 && !dat.PointedDatas.TryGetValue(Pointer, out PointTo)) {
					var tmp = Reader.BaseStream.Position;
					Reader.BaseStream.Seek(dat.DataSectionOffset + Pointer, SeekOrigin.Begin);
					PointTo = new(Pointer, new ListType(Reader, dat, PointToType[5..], Count), dat.UTF32);
					Reader.BaseStream.Seek(tmp, SeekOrigin.Begin);
					dat.PointedDatas.Add(Pointer, PointTo);
				} else if (Count <= 0)
					PointTo = NullList;
			} else {
				Pointer = x64 ? Reader.ReadInt64() : Reader.ReadUInt32();
				if (!dat.PointedDatas.TryGetValue(Pointer, out PointTo)) {
					var tmp = Reader.BaseStream.Position;
					Reader.BaseStream.Seek(dat.DataSectionOffset + Pointer, SeekOrigin.Begin);
					PointTo = new(Pointer, Create(PointToType, Reader, dat), dat.UTF32);
					Reader.BaseStream.Seek(tmp, SeekOrigin.Begin);
					dat.PointedDatas.Add(Pointer, PointTo);
				}
			}
		}

		public static readonly PointedValue NullList = new(0, new ListType(null, null, null, 0));
	}
}