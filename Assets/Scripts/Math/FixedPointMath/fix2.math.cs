using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Math.FixedPointMath
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public readonly partial struct fix2
    {
        public static float3 ToXY(fix2 vector) => new((float) vector.x, (float) vector.y, 0f);

        public static fix dot(fix2 a, fix2 b)
        {
            return a.x * b.x + a.y * b.y;
        }

        public static fix lengthsq(fix2 vec)
        {
            return dot(vec, vec);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix length(fix2 vec)
        {
            return fix.sqrt(lengthsq(vec));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix distancesq(fix2 a, fix2 b)
        {
            return lengthsq(b - a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix distance(fix2 a, fix2 b)
        {
            return length(b - a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 round(fix2 vec)
        {
            return new fix2(fix.round(vec.x), fix.round(vec.y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix rsqrt(fix x)
        {
            return fix.one / fix.sqrt(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 normalize(fix2 vec)
        {
            return rsqrt(dot(vec, vec)) * vec;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 normalize_safe(fix2 vec, fix2 safeResult)
        {
            var length = fix2.length(vec);
            return length != fix.zero ? vec / length : safeResult;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 min(fix2 x, fix2 y)
        {
            return new fix2(fix.min(x.x, y.x), fix.min(x.y, y.y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 max(fix2 x, fix2 y)
        {
            return new fix2(fix.max(x.x, y.x), fix.max(x.y, y.y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 clamp(fix2 x, fix2 a, fix2 b)
        {
            return max(a, min(b, x));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(fix2 vec)
        {
            return vec.x != fix.zero && vec.y != fix.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 abs(fix2 vec)
        {
            return new fix2(fix.abs(vec.x), fix.abs(vec.y));
        }
    }
}
