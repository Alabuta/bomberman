using System.Diagnostics.CodeAnalysis;

namespace Math.FixedPointMath
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct AABB
    {
        public fix2 min;
        public fix2 max;

        public static AABB Invalid = new(new fix2(fix.MaxValue), new fix2(fix.MinValue));

        public AABB(fix2 min, fix2 max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
