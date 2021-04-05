using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace LibBundle {
    public class BundleContainer {
        [DllImport("oo2core_8_win64.dll")]
        public static extern int OodleLZ_Decompress(byte[] buffer, int bufferSize, byte[] result, long outputBufferSize, int a, int b, int c, IntPtr d, long e, IntPtr f, IntPtr g, IntPtr h, long i, int ThreadModule);
        [DllImport("oo2core_8_win64.dll")]
        public static extern int OodleLZ_Compress(ENCODE_TYPES format, byte[] buffer, long bufferSize, byte[] outputBuffer, COMPRESSTION_LEVEL level, IntPtr opts, long offs, long unused, IntPtr scratch, long scratch_size);
        public enum ENCODE_TYPES {
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
        public enum COMPRESSTION_LEVEL {
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
        public COMPRESSTION_LEVEL Compression_Level = COMPRESSTION_LEVEL.Normal;

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

        // For UnPacking
        public BundleContainer(string path) {
            this.path = path;
            var br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            Initialize(br);
            br.Close();
        }
        // For UnPacking
        public BundleContainer(BinaryReader br) {
            Initialize(br);
        }
        protected virtual void Initialize(BinaryReader br) {
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

        // For Packing
        public BundleContainer() {
            offset = 0;
            encoder = ENCODE_TYPES.LEVIATHAN;
            chunk_size = 262144;
            unknown = 1;
            unknown3 = unknown4 = unknown5 = unknown6 = 0;
        }

        // UnPacking
        public virtual MemoryStream Read(string path = null) {
            offset = 0;
            var br = new BinaryReader(File.OpenRead(path ?? this.path));
            var ms = Read(br);
            br.Close();
            return ms;
        }
        // UnPacking
        public virtual MemoryStream Read(BinaryReader br) {
            if (br == null) return Read();
            br.BaseStream.Seek(offset + 60, SeekOrigin.Begin);

            var chunks = new int[entry_count];
            for (var i = 0; i < entry_count; i++)
                chunks[i] = br.ReadInt32();

            var data = new MemoryStream(uncompressed_size);
            for (var i = 0; i < entry_count; i++) {
                var b = br.ReadBytes(chunks[i]);
                var size = (i + 1 == entry_count) ? uncompressed_size - (chunk_size * (entry_count - 1)) : chunk_size; // isLast ?
                var toSave = new byte[size + 64];
                _ = OodleLZ_Decompress(b, b.Length, toSave, size, 0, 0, 0, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, 3);
                data.Write(toSave, 0, size);
            }

            return data;
        }

        public virtual byte[] AppendAndSave(Stream newData, string originalPath = null) {
            offset = 0;
            return AppendAndSave(newData, File.Open(originalPath ?? path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        public virtual byte[] AppendAndSave(Stream newData, Stream originalData) {
            originalData.Seek(offset + 60, SeekOrigin.Begin);
            var OldChunkCompressedSizes = new byte[(entry_count - 1) * 4];
            originalData.Read(OldChunkCompressedSizes, 0, OldChunkCompressedSizes.Length);

            var lastCunkCompressedSize = originalData.ReadByte() | originalData.ReadByte() << 8 | originalData.ReadByte() << 16 | originalData.ReadByte() << 24; //ReadInt32

            var lastCunkDecompressedSize = uncompressed_size - (chunk_size * (entry_count - 1));

            uncompressed_size = (int)(size_decompressed += newData.Length);
            entry_count = uncompressed_size / chunk_size;
            if (uncompressed_size % chunk_size != 0) entry_count++;
            head_size = entry_count * 4 + 48;

            var msToSave = new MemoryStream();
            var bw = new BinaryWriter(msToSave);

            msToSave.Seek(60 + (entry_count * 4), SeekOrigin.Begin);
            var o = new byte[compressed_size - lastCunkCompressedSize];
            originalData.Read(o, 0, o.Length);
            bw.Write(o);

            var lastChunkCompressedData = new byte[lastCunkCompressedSize];
            originalData.Read(lastChunkCompressedData, 0, lastCunkCompressedSize);
            var lastCunkDecompressedData = new byte[lastCunkDecompressedSize + 64];
			_ = OodleLZ_Decompress(lastChunkCompressedData, lastCunkCompressedSize, lastCunkDecompressedData, lastCunkDecompressedSize, 0, 0, 0, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, 3);

            newData.Seek(0, SeekOrigin.Begin);
            compressed_size -= lastCunkCompressedSize;
            var NewChunkCompressedSizes = new int[entry_count - (OldChunkCompressedSizes.Length / 4)];

            var FirstNewDataChunk = new byte[Math.Min(chunk_size - lastCunkDecompressedSize, newData.Length)];
            newData.Read(FirstNewDataChunk, 0, FirstNewDataChunk.Length);
            FirstNewDataChunk = lastCunkDecompressedData.Take(lastCunkDecompressedSize).Concat(FirstNewDataChunk).ToArray();
            var CompressedChunk = new byte[FirstNewDataChunk.Length + 548];
            var CompressedLength = OodleLZ_Compress(encoder, FirstNewDataChunk, FirstNewDataChunk.Length, CompressedChunk, Compression_Level, IntPtr.Zero, 0, 0, IntPtr.Zero, 0);
            compressed_size += NewChunkCompressedSizes[0] = CompressedLength;
            bw.Write(CompressedChunk, 0, CompressedLength);
            for (var i = 1; i < NewChunkCompressedSizes.Length; i++) {
                var size = (i + 1 == NewChunkCompressedSizes.Length) ? uncompressed_size - (chunk_size * (entry_count - 1)) : chunk_size;
                var b = new byte[size];
                newData.Read(b, 0, size);
                var by = new byte[b.Length + 548];
                var l = OodleLZ_Compress(encoder, b, size, by, Compression_Level, IntPtr.Zero, 0, 0, IntPtr.Zero, 0);
                compressed_size += NewChunkCompressedSizes[i] = l;
                bw.Write(by, 0, l);
            }
            size_compressed = compressed_size;

            msToSave.Seek(60, SeekOrigin.Begin);
            bw.Write(OldChunkCompressedSizes);
            for (var i = 0; i < NewChunkCompressedSizes.Length; i++)
                bw.Write(NewChunkCompressedSizes[i]);

            msToSave.Seek(0, SeekOrigin.Begin);
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

        // Packing
        public virtual void Save(Stream newData, string savePath) {
            var bw = new BinaryWriter(File.OpenWrite(savePath ?? path));

            uncompressed_size = (int)(size_decompressed = newData.Length);
            entry_count = uncompressed_size / chunk_size;
            if (uncompressed_size % chunk_size != 0) entry_count++;
            head_size = entry_count * 4 + 48;

            bw.BaseStream.Seek(60 + entry_count * 4, SeekOrigin.Begin);
            newData.Seek(0, SeekOrigin.Begin);
            compressed_size = 0;
            var chunks = new int[entry_count];
            for (var i = 0; i < entry_count; i++) {
                var b = new byte[i + 1 == entry_count ? uncompressed_size - (entry_count - 1) * chunk_size : chunk_size];
                newData.Read(b, 0, b.Length);
                var by = new byte[b.Length + 548];
                var l = OodleLZ_Compress(encoder, b, b.Length, by, Compression_Level, IntPtr.Zero, 0, 0, IntPtr.Zero, 0);
                compressed_size += chunks[i] = l;
                bw.Write(by, 0, l);
            }
            size_compressed = compressed_size;

            bw.BaseStream.Seek(60, SeekOrigin.Begin);
            foreach (var c in chunks) bw.Write(c);

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
        // Packing
        public virtual byte[] Save(Stream newData) {
            var msToSave = new MemoryStream();
            var bw = new BinaryWriter(msToSave);

            uncompressed_size = (int)(size_decompressed = newData.Length);
            entry_count = uncompressed_size / chunk_size;
            if (uncompressed_size % chunk_size != 0) entry_count++;
            head_size = entry_count * 4 + 48;

            msToSave.Seek(60 + (entry_count * 4), SeekOrigin.Begin);
            newData.Seek(0, SeekOrigin.Begin);
            compressed_size = 0;
            var chunks = new int[entry_count];
            for (var i = 0; i < entry_count; i++) {
                var b = new byte[i + 1 == entry_count ? uncompressed_size - (entry_count - 1) * chunk_size : chunk_size];
                newData.Read(b, 0, b.Length);
                var by = new byte[b.Length + 548];
                var l = OodleLZ_Compress(encoder, b, b.Length, by, Compression_Level, IntPtr.Zero, 0, 0, IntPtr.Zero, 0);
                compressed_size += chunks[i] = l;
                bw.Write(by, 0, l);
            }
            size_compressed = compressed_size;

            msToSave.Seek(60, SeekOrigin.Begin);
            for (var i = 0; i < entry_count; i++)
                bw.Write(chunks[i]);

            msToSave.Seek(0, SeekOrigin.Begin);
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