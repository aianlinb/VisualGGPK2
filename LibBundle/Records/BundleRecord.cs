using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibBundle.Records
{
    public class BundleRecord
    {
        public long IndexOffset;
        public int NameLength;
        public string Name;
        public int UncompressedSize;

        public int bundleIndex;
        public int validSize;
        public readonly List<FileRecord> Files = new List<FileRecord>();
        internal readonly Dictionary<FileRecord, byte[]> FileToAdd = new Dictionary<FileRecord, byte[]>();
        protected BundleContainer _bundle;

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
            IndexOffset = br.BaseStream.Position;
            NameLength = br.ReadInt32();
            Name = Encoding.UTF8.GetString(br.ReadBytes(NameLength)) + ".bundle.bin";
            UncompressedSize = br.ReadInt32();
        }

        public virtual void Read(BinaryReader br = null, long? Offset = null)
        {
            if (_bundle != null) return;
            if (Offset.HasValue)
            {
                br.BaseStream.Seek(Offset.Value, SeekOrigin.Begin);
                _bundle = new BundleContainer(br);
            }
            else if (br == null)
            {
                _bundle = new BundleContainer(Name);
            }
            else
            {
                _bundle = new BundleContainer(br);
            }
        }

        public virtual void Save(string newPath = null, string originalPath = null)
        {
            if (newPath == null && originalPath == null && Bundle.path == null)
#pragma warning disable CA2208 // 正確地將引數例外狀況具現化
				throw new ArgumentNullException();
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

        public virtual byte[] Save(BinaryReader br, long? Offset = null)
        {
            Read(br, Offset);
            var data = new MemoryStream();
            foreach (var (f, b) in FileToAdd)
            {
                f.Offset = (int)data.Position + Bundle.uncompressed_size;
                data.Write(b, 0, f.Size);
            }
            byte[] result;
            if (data.Length == 0)
            {
                if (br == null) {
                    result = File.ReadAllBytes(Bundle.path);
                } else {
                    br.BaseStream.Seek(Bundle.offset, SeekOrigin.Begin);
                    result = br.ReadBytes(Bundle.head_size + Bundle.compressed_size + 12);
                }
            }
            else
            {
                UncompressedSize = (int)data.Length + Bundle.uncompressed_size;
                result = Bundle.AppendAndSave(data, br?.BaseStream ?? Bundle.Read());
            }
            FileToAdd.Clear();
            data.Close();
            return result;
        }

        public virtual void SaveWithRecompression(string newPath = null, string originalPath = null) {
            if (newPath == null && originalPath == null && Bundle.path == null)
                throw new ArgumentNullException();
            var data = Bundle.Read(originalPath);
            data.SetLength(validSize);
            data.Position = validSize;
            foreach (var d in FileToAdd) {
                d.Key.Offset = (int)data.Position;
                data.Write(d.Value, 0, d.Key.Size);
            }
            UncompressedSize = (int)data.Length;
            FileToAdd.Clear();
            Bundle.Save(data, newPath ?? originalPath);
        }

        public virtual byte[] SaveWithRecompression(BinaryReader br, long? Offset = null) {
            Read(br, Offset);
            var data = Bundle.Read(br);
            data.SetLength(validSize);
            data.Position = validSize;
            foreach (var (f, b) in FileToAdd) {
                f.Offset = (int)data.Position;
                data.Write(b, 0, f.Size);
            }
            UncompressedSize = validSize = (int)data.Length;
            var result = Bundle.Save(data);
            FileToAdd.Clear();
            data.Close();
            return result;
        }
    }
}