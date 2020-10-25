using System;
using System.IO;

namespace LibBundle
{
    public class BundleContainer
    {
        [System.Runtime.InteropServices.DllImport("oo2core_8_win64.dll")]
        public static extern int OodleLZ_Decompress(byte[] buffer, int bufferSize, byte[] result, long outputBufferSize, int a, int b, int c, IntPtr  d, long e, IntPtr f, IntPtr g, IntPtr h, long i, int ThreadModule);
        [System.Runtime.InteropServices.DllImport("oo2core_8_win64.dll")]
        public static extern int OodleLZ_Compress(ENCODE_TYPES format, byte[] buffer, long bufferSize, byte[] outputBuffer, COMPRESSTION_LEVEL level, IntPtr opts, long offs, long unused, IntPtr scratch, long scratch_size);
        public enum ENCODE_TYPES
        {
            LZH = 0,
            LZHLW = 1,
            LZNIB = 2,
            NONE = 3,
            LZB16 = 4,
            LZBLW = 5,
            LZA = 6,
            LZNA = 7,
            KRAKEN = 8,
            MERMAID = 9,
            BITKNIT = 10,
            SELKIE = 11,
            HYDRA = 12,
            LEVIATHAN = 13
        }
        public enum COMPRESSTION_LEVEL
        {
            None,
            SuperFast,
            VeryFast,
            Fast,
            Normal,
            Optimal1,
            Optimal2,
            Optimal3,
            Optimal4,
            Optimal5
        }

        public string path;
        public long offset;

        public int uncompressed_size;
        public int compressed_size;
        public int head_size; // entry_count * 4 + 48
        public ENCODE_TYPES encoder; // 13
        public int unknown; // 1
        public long size_decompressed; // uncompressed_size
        public long size_compressed; // compressed_size
        public int entry_count;
        public int chunk_size; // 256KB == 262144
        public int unknown3; // 0
        public int unknown4; // 0
        public int unknown5; // 0
        public int unknown6; // 0

        //For UnPacking
        public BundleContainer(string path)
        {
            this.path = path;
            var br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            Initialize(br);
            br.Close();
        }
        //For UnPacking
        public BundleContainer(BinaryReader br)
        {
            Initialize(br);
        }
        private void Initialize(BinaryReader br)
        {
            offset = br.BaseStream.Position;
            uncompressed_size = br.ReadInt32();
            compressed_size = br.ReadInt32();
            head_size = br.ReadInt32();
            encoder = (ENCODE_TYPES)br.ReadInt32();
            unknown = br.ReadInt32();
            size_decompressed = (int)br.ReadInt64();
            size_compressed = br.ReadInt64();
            entry_count = br.ReadInt32();
            chunk_size = br.ReadInt32();
            unknown3 = br.ReadInt32();
            unknown4 = br.ReadInt32();
            unknown5 = br.ReadInt32();
            unknown6 = br.ReadInt32();
        }

        //For Packing
        public BundleContainer()
        {
            offset = 0;
            encoder = ENCODE_TYPES.LEVIATHAN;
            chunk_size = 262144;
            unknown = 1;
            unknown3 = unknown4 = unknown5 = unknown6 = 0;
        }

        //UnPacking
        public virtual MemoryStream Read(string path = null)
        {
            if (path == null)
                path = this.path;
            offset = 0;
            var br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            var ms = Read(br);
            br.Close();
            return ms;
        }
        //UnPacking
        public virtual MemoryStream Read(BinaryReader br)
        {
            br.BaseStream.Seek(offset + 60, SeekOrigin.Begin);

            var chunks = new int[entry_count];
            for (int i = 0; i < entry_count; i++)
                chunks[i] = br.ReadInt32();
            
            var data = new MemoryStream(uncompressed_size);
            for (int i = 0; i < entry_count; i++)
            {
                var b = br.ReadBytes(chunks[i]);
                int size = (i + 1 == entry_count) ? uncompressed_size - (chunk_size * (entry_count - 1)) : chunk_size; // isLast ?
                var toSave = new byte[size + 64];
                OodleLZ_Decompress(b, b.Length, toSave, size, 0, 0, 0, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, 3);
                data.Write(toSave, 0, size);
            }
            return data;
        }

        //Packing
        public virtual void Save(Stream ms, string path)
        {
            var bw = new BinaryWriter(File.Open(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite));

            uncompressed_size = (int)(size_decompressed = ms.Length);
            entry_count = uncompressed_size / chunk_size;
            if (uncompressed_size % chunk_size != 0) entry_count++;
            head_size = entry_count * 4 + 48;

            bw.BaseStream.Seek(60 + (entry_count * 4), SeekOrigin.Begin);
            ms.Position = 0;
            compressed_size = 0;
            var chunks = new int[entry_count];
            for (int i = 0; i < entry_count - 1; i++)
            {
                var b = new byte[chunk_size];
                ms.Read(b, 0, chunk_size);
                var by = new byte[b.Length + 548];
                var l = OodleLZ_Compress(ENCODE_TYPES.LEVIATHAN, b, b.Length, by, COMPRESSTION_LEVEL.Normal, IntPtr.Zero, 0, 0, IntPtr.Zero, 0);
                compressed_size += chunks[i] = l;
                bw.Write(by, 0, l);
            }
            var b2 = new byte[ms.Length - (entry_count - 1) * chunk_size];
            ms.Read(b2, 0, b2.Length);
            var by2 = new byte[b2.Length + 548];
            var l2 = OodleLZ_Compress(ENCODE_TYPES.LEVIATHAN, b2, b2.Length, by2, COMPRESSTION_LEVEL.Normal, IntPtr.Zero, 0, 0, IntPtr.Zero, 0);
            compressed_size += chunks[entry_count - 1] = l2;
            bw.Write(by2, 0, l2);
            size_compressed = compressed_size;

            bw.BaseStream.Seek(60, SeekOrigin.Begin);
            for (int i = 0; i < entry_count; i++)
                bw.Write(chunks[i]);

            bw.BaseStream.Seek(0, SeekOrigin.Begin);
            bw.Write(uncompressed_size);
            bw.Write(compressed_size);
            bw.Write(head_size);
            bw.Write((uint)encoder);
            bw.Write(unknown);
            bw.Write(size_decompressed);
            bw.Write(size_compressed);
            bw.Write(entry_count);
            bw.Write(chunk_size);
            bw.Write(unknown3);
            bw.Write(unknown4);
            bw.Write(unknown5);
            bw.Write(unknown6);

            bw.Flush();
            bw.Close();
        }
        //Packing
        public virtual byte[] Save(Stream ms)
        {
            var msToSave = new MemoryStream();
            var bw = new BinaryWriter(msToSave);

            uncompressed_size = (int)(size_decompressed = ms.Length);
            entry_count = uncompressed_size / chunk_size;
            if (uncompressed_size % chunk_size != 0) entry_count++;
            head_size = entry_count * 4 + 48;

            bw.BaseStream.Seek(60 + (entry_count * 4), SeekOrigin.Begin);
            ms.Position = 0;
            compressed_size = 0;
            var chunks = new int[entry_count];
            for (int i = 0; i < entry_count - 1; i++)
            {
                var b = new byte[chunk_size];
                ms.Read(b, 0, chunk_size);
                var by = new byte[b.Length + 548];
                var l = OodleLZ_Compress(ENCODE_TYPES.LEVIATHAN, b, b.Length, by, COMPRESSTION_LEVEL.Normal, IntPtr.Zero, 0, 0, IntPtr.Zero, 0);
                compressed_size += chunks[i] = l;
                bw.Write(by, 0, l);
            }
            var b2 = new byte[ms.Length - (entry_count - 1) * chunk_size];
            ms.Read(b2, 0, b2.Length);
            var by2 = new byte[b2.Length + 548];
            var l2 = OodleLZ_Compress(ENCODE_TYPES.LEVIATHAN, b2, b2.Length, by2, COMPRESSTION_LEVEL.Normal, IntPtr.Zero, 0, 0, IntPtr.Zero, 0);
            compressed_size += chunks[entry_count - 1] = l2;
            bw.Write(by2, 0, l2);
            size_compressed = compressed_size;

            bw.BaseStream.Seek(60, SeekOrigin.Begin);
            for (int i = 0; i < entry_count; i++)
                bw.Write(chunks[i]);

            bw.BaseStream.Seek(0, SeekOrigin.Begin);
            bw.Write(uncompressed_size);
            bw.Write(compressed_size);
            bw.Write(head_size);
            bw.Write((uint)encoder);
            bw.Write(unknown);
            bw.Write(size_decompressed);
            bw.Write(size_compressed);
            bw.Write(entry_count);
            bw.Write(chunk_size);
            bw.Write(unknown3);
            bw.Write(unknown4);
            bw.Write(unknown5);
            bw.Write(unknown6);

            bw.Flush();
            var result = msToSave.ToArray();
            bw.Close();
            return result;
        }
    }
}