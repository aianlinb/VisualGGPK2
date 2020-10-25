using System.Collections.Generic;

namespace LibBundle.Records
{
    public class DirectoryRecord
    {
        public long indexOffset;
        public List<FileRecord> children = new List<FileRecord>(); // Files only

        public ulong Hash;
        public int Offset;
        public int Size;
        public int RecursiveSize;

        public DirectoryRecord(System.IO.BinaryReader br)
        {
            indexOffset = br.BaseStream.Position;
            Hash = br.ReadUInt64();
            Offset = br.ReadInt32();
            Size = br.ReadInt32();
            RecursiveSize = br.ReadInt32();
        }
    }
}