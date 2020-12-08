using System.Collections.Generic;
using System.IO;

namespace LibBundle.Records
{
    public class DirectoryRecord
    {
        public long indexOffset;
        public List<FileRecord> children = new List<FileRecord>(); // Files only

        public ulong NameHash;
        public int Offset;
        public int Size;
        public int RecursiveSize;

        public DirectoryRecord(BinaryReader br)
        {
            indexOffset = br.BaseStream.Position;
            NameHash = br.ReadUInt64();
            Offset = br.ReadInt32();
            Size = br.ReadInt32();
            RecursiveSize = br.ReadInt32();
        }
    }
}