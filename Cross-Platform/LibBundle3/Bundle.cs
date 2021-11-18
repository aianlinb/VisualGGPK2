using System.Runtime.InteropServices;
using static LibBundle3.Oodle;

namespace LibBundle3 {
	public unsafe class Bundle {
        [StructLayout(LayoutKind.Sequential, Size = 48)]
		public struct Header {
            public int uncompressed_size;
            public int compressed_size;
            public int head_size; // entry_count * 4 + 48
            public Compressor compressor; // Leviathan = 13
            public int unknown; // 1
            public long size_decompressed; // uncompressed_size
            public long size_compressed; // compressed_size
            public int entry_count;
            public int chunk_size; // 256KB == 262144
            public int unknown3; // 0
            public int unknown4; // 0
            public int unknown5; // 0
            public int unknown6; // 0
        }
    }
}