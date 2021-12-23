using System.IO;
using System.Text;

namespace LibGGPK3.Records {
	/// <summary>
	/// GGPK record is the very first record and exists at the very beginning of the GGPK file.
	/// It must have excatly 2 entries - One goes to the root directory and the other to a FREE record.
	/// </summary>
	public class GGPKRecord : BaseRecord {
		public static readonly byte[] Tag = Encoding.ASCII.GetBytes("GGPK");

		public uint GGPKVersion = 3;

		public long RootDirectoryOffset;
		public long FirstFreeRecordOffset;

		public GGPKRecord(int length, GGPK ggpk) : base(length, ggpk) {
			Offset = ggpk.FileStream.Position - 8;
			var br = Ggpk.Reader;
			GGPKVersion = br.ReadUInt32(); // 3
			RootDirectoryOffset = br.ReadInt64();
			FirstFreeRecordOffset = br.ReadInt64();
		}

		protected internal override void Write(BinaryWriter? bw = null) {
			bw ??= Ggpk.Writer;
			Offset = bw.BaseStream.Position;
			bw.Write(Length); // 28
			bw.Write(Tag);
			bw.Write(GGPKVersion); // 3
			bw.Write(RootDirectoryOffset);
			bw.Write(FirstFreeRecordOffset);
		}
	}
}