using System;
using System.Text;

//-----------------------------------------------------------------------------
// MurmurHash2, by Austin Appleby

// Note - This code makes a few assumptions about how your machine behaves -

// 1. We can read a 4-byte value from any address without crashing
// 2. sizeof(int) == 4

// And it has a few limitations -

// 1. It will not work incrementally.
// 2. It will not produce the same results on little-endian and big-endian
//	machines.
//-----------------------------------------------------------------------------

namespace LibGGPK
{
    public static class Murmur
    {
        private const uint DefaultMurmurSeed = 0;

        public static uint Hash2(string str)
        {
            return Hash2(Encoding.Unicode.GetBytes(str.ToLower()), DefaultMurmurSeed);
        }

        public static uint Hash2(byte[] data, uint seed = DefaultMurmurSeed)
        {
            // 'm' and 'r' are mixing constants generated offline.
            // They're not really 'magic', they just happen to work well.

            const uint m = 0x5bd1e995;
            const int r = 24;

            // Initialize the hash to a 'random' value

            uint h = seed ^ (uint)data.Length;

            // Mix 4 bytes at a time into the hash

            int dataIndex = 0;
            int len = data.Length;

            while (len >= 4)
            {
                uint k = BitConverter.ToUInt32(data, dataIndex);

                k *= m;
                k ^= k >> r;
                k *= m;

                h *= m;
                h ^= k;

                dataIndex += 4;
                len -= 4;
            }

            // Handle the last few bytes of the input array
            switch (len)
            {
                case 3:
                    {
                        h ^= (uint)data[dataIndex + 2] << 16;
                        goto case 2;
                    }
                case 2:
                    {
                        h ^= (uint)data[dataIndex + 1] << 8;
                        goto case 1;
                    }
                case 1:
                    {
                        h ^= (uint)data[dataIndex + 0];
                        h *= m;
                        break;
                    }
            }

            // Do a few final mixes of the hash to ensure the last few
            // bytes are well-incorporated.

            h ^= h >> 13;
            h *= m;
            h ^= h >> 15;

            return h;
        }
    }
}
