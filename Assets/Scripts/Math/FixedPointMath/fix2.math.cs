using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Math.FixedPointMath
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public readonly partial struct fix2
    {
        public static float3 ToXY(fix2 vector) => new((float) vector.x, (float) vector.y, 0f);

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
    }
}
