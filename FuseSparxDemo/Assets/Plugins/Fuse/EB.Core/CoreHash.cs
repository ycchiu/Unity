using UnityEngine;
using System.Collections;

namespace EB
{
		// a basic hashing function (FNV32/64)
	public static class Hash
    {
        public static ulong HASH_PRIME_64 = 1099511628211;
        public static ulong HASH_INIT_64 = 14695981039346656037;

        public static uint HASH_PRIME_32 = 16777619;
        public static uint HASH_INIT_32 = 2166136261;

        public static long FNV64(byte[] data, long init)
        {
            ulong hash = (ulong)init;
            foreach (byte b in data)
            {
                hash = hash * HASH_PRIME_64;
                hash = hash ^ b;
            }
            return (long)hash;
        }

        public static int StringHash(string data)
        {
            return FNV32(data);
        }

        public static int FNV32(string data)
        {
            return FNV32(data, (int)HASH_INIT_32);
        }

        public static int FNV32(string data, int init)
        {
            return FNV32(Encoding.GetBytes(data), init);
        }
		
		public static int FNV32(byte[] data, int init)
		{
			return FNV32(data,0,data.Length,init);
		}

        public static int FNV32(byte[] data, int offset, int count, int init)
        {
            uint hash = (uint)init;
            for ( int i = 0; i < count; ++i )
            {
                hash = hash * HASH_PRIME_32;
                hash = hash ^ data[i+offset];
            }
            return (int)hash;
        }
    }
}
