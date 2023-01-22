using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace Math.FixedPointMath
{
    [BurstCompile]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public readonly struct AABB
    {
        public readonly fix2 min;
        public readonly fix2 max;

        public static readonly AABB Empty = new(new fix2(fix.MaxValue), new fix2(fix.MinValue));

        public AABB(fix2 min, fix2 max)
        {
            this.min = min;
            this.max = max;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid() =>
            math.all(max - min > fix2.zero);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(AABB x, AABB y) =>
            math.all(x.min == y.min) && math.all(x.max == y.max);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(AABB x, AABB y) =>
            !(x == y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AABB other) =>
            this == other;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) =>
            obj is AABB other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(min, max);

        public fix2 GetCenter() => min + (max - min) / new fix2(2);
    }
}
