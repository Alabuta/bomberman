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
            return vec.x * vec.x + vec.y * vec.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix length(fix2 vec)
        {
            return fix.sqrt(lengthsq(vec));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix distanceq(fix2 a, fix2 b)
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
    }
}
