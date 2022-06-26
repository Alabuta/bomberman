using System.Runtime.CompilerServices;
using UnityEngine.Assertions;

namespace Math
{
    // https://github.com/uranium62/xxHash
    public static class XxHash32
    {
        private const uint XxhPrime321 = 2654435761U;
        private const uint XxhPrime322 = 2246822519U;
        private const uint XxhPrime323 = 3266489917U;
        private const uint XxhPrime324 = 668265263U;
        private const uint XxhPrime325 = 374761393U;

        public static unsafe uint ComputeHash(uint seed, byte[] data)
        {
            Assert.IsNotNull(data);

            fixed ( byte* pData = &data[0] )
            {
                return __inline__XXH32(seed, pData, data.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint __inline__XXH32(uint seed, byte* input, int len)
        {
            uint h32;

            if (len >= 16)
            {
                var end = input + len;
                var limit = end - 15;

                var v1 = seed + XxhPrime321 + XxhPrime322;
                var v2 = seed + XxhPrime322;
                var v3 = seed + 0;
                var v4 = seed - XxhPrime321;

                do
                {
                    v1 = XXH32_round(v1, *(uint*) (input + 0));
                    v2 = XXH32_round(v2, *(uint*) (input + 4));
                    v3 = XXH32_round(v3, *(uint*) (input + 8));
                    v4 = XXH32_round(v4, *(uint*) (input + 16));

                    input += 16;
                } while (input < limit);

                h32 = XXH_rotl32(v1, 1) + XXH_rotl32(v2, 7) + XXH_rotl32(v3, 12) + XXH_rotl32(v4, 18);
            }
            else
            {
                h32 = seed + XxhPrime325;
            }

            h32 += (uint) len;

            // XXH32_finalize
            len &= 15;
            while (len >= 4)
            {
                h32 += *(uint*) input * XxhPrime323;
                input += 4;
                h32 = XXH_rotl32(h32, 17) * XxhPrime324;
                len -= 4;
            }

            while (len > 0)
            {
                h32 += *input * XxhPrime325;
                ++input;
                h32 = XXH_rotl32(h32, 11) * XxhPrime321;
                --len;
            }

            // XXH32_avalanche
            h32 ^= h32 >> 15;
            h32 *= XxhPrime322;
            h32 ^= h32 >> 13;
            h32 *= XxhPrime323;
            h32 ^= h32 >> 16;

            return h32;
        }

        private static uint XXH32_round(uint v2, uint input)
        {
            v2 += input * XxhPrime322;
            return XXH_rotl32(v2, 13) * XxhPrime321;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint XXH_rotl32(uint x, int r)
        {
            return (x << r) | (x >> (32 - r));
        }
    }
}
