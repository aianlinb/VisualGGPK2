using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibBundle3 {
	public unsafe class Bundle : IDisposable {
		[StructLayout(LayoutKind.Sequential, Size = 60)]
		public struct Header {
			public int uncompressed_size;
			public int compressed_size;
			public int head_size; // chunk_count * 4 + 48
			public Oodle.Compressor compressor; // Leviathan == 13
			public int unknown; // 1
			public long size_decompressed; // == uncompressed_size
			public long size_compressed; // == compressed_size
			public int chunk_count;
			public int chunk_size; // 256KB == 262144
			public int unknown3; // 0
			public int unknown4; // 0
			public int unknown5; // 0
			public int unknown6; // 0

			public int GetLastChunkSize() {
				return uncompressed_size - (chunk_size * (chunk_count - 1));
			}
		}

		public int UncompressedSize => header.uncompressed_size;
		public int CompressedSize => header.compressed_size;

		protected readonly Stream baseStream;
		protected readonly long streamOffset;
		protected readonly bool streamLeaveOpen; // If false, close the baseStream when dispose

		protected Header header;
		protected int[] compressed_chunk_sizes;

		public byte[]? CachedData;

		public Bundle(string filePath) : this(File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)) { }

		public Bundle(Stream stream, bool leaveOpen = false) {
			baseStream = stream ?? throw new ArgumentNullException(nameof(stream));
			streamOffset = stream.Position;
			streamLeaveOpen = leaveOpen;

			var p = stackalloc byte[60];

			stream.Read(new(p, 60));
			Marshal.PtrToStructure((IntPtr)p, header); // Read bundle header

			compressed_chunk_sizes = new int[header.chunk_count];
			fixed (int* p2 = compressed_chunk_sizes)
				stream.Read(new(p2, header.chunk_count << 2));
		}

		public virtual byte[] ReadData() {
			if (CachedData is not null)
				return CachedData;
			return ReadChunks(0, compressed_chunk_sizes.Length);
		}

		public virtual void ReadDataAndCache() {
			CachedData = ReadData();
		}

		public virtual Span<byte> ReadData(int offset, int length) {
			if (offset < 0 || offset >= header.uncompressed_size)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (length < 0 || offset + length > header.uncompressed_size)
				throw new ArgumentOutOfRangeException(nameof(length));

			if (CachedData is not null)
				return CachedData.AsSpan()[offset..length];
			return new(ReadChunks(offset / header.chunk_size, (length - offset) / header.chunk_size), offset, length);
		}

		public virtual byte[] ReadChunks(int start, int count = 1) {
			if (start < 0 || start >= compressed_chunk_sizes.Length)
				throw new ArgumentOutOfRangeException(nameof(start));
			if (count < 0 || start + count > compressed_chunk_sizes.Length)
				throw new ArgumentOutOfRangeException(nameof(count));

			var ofs = 0;
			for (var i = 0; i < start; ++i)
				ofs += compressed_chunk_sizes[i];
			baseStream.Seek(streamOffset + 60 + ofs, SeekOrigin.Begin);

			if (count == 0)
				return Array.Empty<byte>();

			var result = new byte[header.chunk_size * count];

			var tmp = stackalloc byte[header.chunk_size];

			ofs = 0;
			fixed (byte* p = result) {
				count -= 1;
				for (var i = start; i < count; ++i) {
					var len = 0;
					while (len < compressed_chunk_sizes[i])
						len += baseStream.Read(new(tmp + len, compressed_chunk_sizes[i] - len));
					ofs += (int)Oodle.OodleLZ_Decompress(tmp, len, p + ofs, result.Length - ofs);
				}
				var len2 = 0;
				var lastChunkSize = header.GetLastChunkSize();
				while (len2 < lastChunkSize)
					len2 += baseStream.Read(new(tmp + len2, lastChunkSize - len2));
				ofs += (int)Oodle.OodleLZ_Decompress(tmp, len2, p + ofs, header.chunk_size);
			}

			return result;
		}

		public virtual void SaveData(ReadOnlySpan<byte> newData, Oodle.CompressionLevel compressionLevel = Oodle.CompressionLevel.Normal) {
			CachedData = null;

			header.size_decompressed = header.uncompressed_size = newData.Length;
			header.chunk_count = header.uncompressed_size / header.chunk_size;
			if (header.uncompressed_size % header.chunk_size != 0)
				header.chunk_count++;
			header.head_size = header.chunk_count * 4 + 48;

			baseStream.Seek(streamOffset + 12 + header.head_size, SeekOrigin.Begin);
			header.compressed_size = 0;
			var chunkSizes = new int[header.chunk_count];
			var compressed = new byte[header.chunk_size];
			fixed (byte* cp = compressed, p = newData) {
				var ptr = p;
				var count = header.chunk_count - 1;
				for (var i = 0; i < count; ++i) {
					var l = (int)Oodle.OodleLZ_Compress(header.compressor, ptr, header.chunk_size, cp, compressionLevel);
					ptr += header.chunk_size;
					header.compressed_size += chunkSizes[i] = l;
					baseStream.Write(compressed, 0, l);
				}
				var l2 = (int)Oodle.OodleLZ_Compress(header.compressor, ptr, header.GetLastChunkSize(), cp, compressionLevel);
				header.compressed_size += chunkSizes[^1] = l2;
				baseStream.Write(compressed, 0, l2);
			}
			header.size_compressed = header.compressed_size;

			baseStream.Seek(streamOffset, SeekOrigin.Begin);
			var tmp = new byte[60];
			fixed (byte* p = tmp)
				Marshal.StructureToPtr(header, (IntPtr)p, false);
			baseStream.Write(tmp, 0, 60);
			fixed (int* p = chunkSizes)
				baseStream.Write(new(p, chunkSizes.Length * sizeof(int)));

			baseStream.SetLength(12 + header.head_size + header.compressed_size);
			baseStream.Flush();
		}

		public virtual void Dispose() {
			GC.SuppressFinalize(this);
			if (!streamLeaveOpen)
				baseStream.Close();
		}

		~Bundle() {
			Dispose();
		}
	}
}