using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Math.FixedPointMath
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct AABB : IEquatable<AABB>
    {
        public fix2 min;
        public fix2 max;

        public static AABB Invalid = new(new fix2(fix.MaxValue), new fix2(fix.MinValue));

        public AABB(fix2 min, fix2 max)
        {
            this.min = min;
            this.max = max;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(AABB x, AABB y) => x.Equals(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(AABB x, AABB y) => !(x == y);

        public bool Equals(AABB other) =>
            math.all(min == other.min) && math.all(max == other.max);
    }
}
