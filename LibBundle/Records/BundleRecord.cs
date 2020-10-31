using System;
using System.Collections.Generic;
using System.IO;

namespace LibBundle.Records
{
    public class BundleRecord
    {
        public long indexOffset;
        public int bundleIndex;
        public int nameLength;
        public string Name;
        public int UncompressedSize;
        public readonly List<FileRecord> Files = new List<FileRecord>();
        internal readonly Dictionary<FileRecord, byte[]> FileToAdd = new Dictionary<FileRecord, byte[]>();
        private BundleContainer _bundle;

        public BundleContainer Bundle
        {
            get
            {
                Read();
                return _bundle;
            }
        }

        public BundleRecord(BinaryReader br)
        {
            indexOffset = br.BaseStream.Position;
            nameLength = br.ReadInt32();
            Name = System.Text.Encoding.UTF8.GetString(br.ReadBytes(nameLength)) + ".bundle.bin";
            UncompressedSize = br.ReadInt32();
        }

        public void Read(BinaryReader br = null, long? Offset = null)
        {   
            if (_bundle == null)
                if (Offset.HasValue)
                {
                    br.BaseStream.Seek(Offset.Value, SeekOrigin.Begin);
                    _bundle = new BundleContainer(br);
                }
                else if (br == null)
                    _bundle = new BundleContainer(Name);
                else
                    _bundle = new BundleContainer(br);
        }

        public void Save(string newPath = null, string originalPath = null)
        {
            if (newPath == null && originalPath == null && Bundle.path == null)
                throw new ArgumentNullException("Could not find path to read and save");
            var data = new MemoryStream();
            foreach (var d in FileToAdd)
            {
                d.Key.Offset = (int)data.Position + Bundle.uncompressed_size;
                data.Write(d.Value, 0, d.Key.Size);
            }
            UncompressedSize = (int)data.Length + Bundle.uncompressed_size;
            FileToAdd.Clear();
            if (newPath != null)
                File.WriteAllBytes(newPath, Bundle.AppendAndSave(data, originalPath));
            else if (originalPath != null)
                File.WriteAllBytes(originalPath, Bundle.AppendAndSave(data, originalPath));
            else
                File.WriteAllBytes(Bundle.path, Bundle.AppendAndSave(data, originalPath));
        }
        public byte[] Save(BinaryReader br, long? Offset = null)
        {
            if (Offset != null)
                Read(br, Offset);
            var data = new MemoryStream();
            foreach (var d in FileToAdd)
            {
                d.Key.Offset = (int)data.Position + Bundle.uncompressed_size;
                data.Write(d.Value, 0, d.Key.Size);
            }
            byte[] result;
            if (data.Length == 0) {
                br.BaseStream.Seek(Bundle.offset, SeekOrigin.Begin);
                result = br.ReadBytes(Bundle.head_size + Bundle.compressed_size + 12);
            } else {
                UncompressedSize = (int)data.Length + Bundle.uncompressed_size;
                result = Bundle.AppendAndSave(data, br.BaseStream);
            }
            FileToAdd.Clear();
            data.Close();
            return result;
        }
    }
}