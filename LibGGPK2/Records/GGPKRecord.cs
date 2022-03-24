using System.IO;
using System.Text;

namespace LibGGPK2.Records
{
	/// <summary>
	/// GGPK record is the very first record and exists at the very beginning of the GGPK file.
	/// It must have excatly 2 entries - One goes to the root directory and the other to a FREE record.
	/// </summary>
	public class GGPKRecord : BaseRecord
    {
        public static readonly byte[] Tag = Encoding.ASCII.GetBytes("GGPK");
        
        public uint GGPKVersion = 3; // 3 for PC, 4 for Mac

        public long RootDirectoryOffset;
        public long FirstFreeRecordOffset;

        public GGPKRecord(int length, GGPKContainer ggpk)
        {
            ggpkContainer = ggpk;
            Offset = ggpk.fileStream.Position - 8;
            Length = length; // 28
            Read();
        }

        protected override void Read()
        {
            var br = ggpkContainer.Reader;
            GGPKVersion = br.ReadUInt32(); // 3 for PC, 4 for Mac
            RootDirectoryOffset = br.ReadInt64();
            FirstFreeRecordOffset = br.ReadInt64();
        }

        internal override void Write(BinaryWriter bw = null)
        {
            bw ??= ggpkContainer.Writer;
            Offset = bw.BaseStream.Position;
            bw.Write(Length); // 28
            bw.Write(Tag);
            bw.Write(GGPKVersion); // 3 for PC, 4 for Mac
            bw.Write(RootDirectoryOffset);
            bw.Write(FirstFreeRecordOffset);
        }
    }
}