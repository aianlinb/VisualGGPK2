using System.IO;

namespace LibBundle.Records
{
    public class FileRecord
    {
        public long indexOffset;
        public BundleRecord bundleRecord;
        public DirectoryRecord parent;
        public string path;

        public ulong NameHash;
        public int BundleIndex;
        public int Offset;
        public int Size;

        public FileRecord(BinaryReader br)
        {
            indexOffset = br.BaseStream.Position;
            NameHash = br.ReadUInt64();
            BundleIndex = br.ReadInt32();
            Offset = br.ReadInt32();
            Size = br.ReadInt32();
        }

        public byte[] Read(Stream stream = null)
        {
            if (bundleRecord.FileToAdd.TryGetValue(this, out var b)) return b;
            b = new byte[Size];
            var data = stream ?? bundleRecord.Bundle.Read();
            data.Seek(Offset, SeekOrigin.Begin);
            data.Read(b, 0, Size);
            return b;
        }

        public void Move(BundleRecord target)
        {
            if (bundleRecord.FileToAdd.TryGetValue(this, out var data))
                bundleRecord.FileToAdd.Remove(this);
            else data = Read();
            bundleRecord.Files.Remove(this);
            target.Files.Add(this);
            target.FileToAdd[this] = data;
            bundleRecord = target;
            BundleIndex = target.bundleIndex;
        }

        public void Write(byte[] data)
        {
            Size = data.Length;
            bundleRecord.FileToAdd[this] = data;
        }
    }
}