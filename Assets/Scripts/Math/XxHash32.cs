using System.Runtime.CompilerServices;
using UnityEngine.Assertions;

namespace Math
{
    // https://github.com/uranium62/xxHash
    public static class XxHash32
    {
        // ReSharper disable InconsistentNaming
        private const uint XxhPrime32_1 = 2654435761U;
        private const uint XxhPrime32_2 = 2246822519U;
        private const uint XxhPrime32_3 = 3266489917U;
        private const uint XxhPrime32_4 = 668265263U;
        private const uint XxhPrime32_5 = 374761393U;
        // ReSharper restore InconsistentNaming

        public static unsafe uint ComputeHash(uint seed, int[] data)
        {
            Assert.IsNotNull(data);

            fixed ( int* pData = &data[0] )
            {
                return __inline__XXH32(seed, pData, data.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint __inline__XXH32(uint seed, int* input, int len)
        {
            uint h32;

            if (len >= 4)
            {
                var end = input + len;
                var limit = end - 3;

                var v1 = seed + XxhPrime32_1 + XxhPrime32_2;
                var v2 = seed + XxhPrime32_2;
                var v3 = seed + 0;
                var v4 = seed - XxhPrime32_1;

                do
                {
                    v1 = XXH32_round(v1, *(uint*) input);
                    ++input;

                    v2 = XXH32_round(v2, *(uint*) input);
                    ++input;

                    v3 = XXH32_round(v3, *(uint*) input);
                    ++input;

                    v4 = XXH32_round(v4, *(uint*) input);
                    ++input;
                } while (input < limit);

                h32 = XXH_left_rotation(v1, 1) + XXH_left_rotation(v2, 7) + XXH_left_rotation(v3, 12) +
                      XXH_left_rotation(v4, 18);
            }
            else
            {
                h32 = seed + XxhPrime32_5;
            }

            h32 += (uint) len * 4;

            // XXH32_finalize
            len &= 3;
            while (len > 0)
            {
                h32 += *(uint*) input * XxhPrime32_3;
                h32 = XXH_left_rotation(h32, 17) * XxhPrime32_4;

                ++input;
                --len;
            }

            // XXH32_avalanche
            h32 ^= h32 >> 15;
            h32 *= XxhPrime32_2;
            h32 ^= h32 >> 13;
            h32 *= XxhPrime32_3;
            h32 ^= h32 >> 16;

            return h32;
        }

        private static uint XXH32_round(uint v2, uint input)
        {
            v2 += input * XxhPrime32_2;
            return XXH_left_rotation(v2, 13) * XxhPrime32_1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint XXH_left_rotation(uint x, int r)
        {
            return (x << r) | (x >> (32 - r));
        }
    }
}
