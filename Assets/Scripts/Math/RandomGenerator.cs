using Math.FixedPointMath;

namespace Math
{
    public class RandomGenerator
    {
        public uint Seed { get; }

        public RandomGenerator(uint seed)
        {
            Seed = seed;
        }

        public int Range(int minInclusive, int maxInclusive, params int[] ps)
        {
            var hash = XxHash32.ComputeHash(Seed, ps);

            var range = (uint) (maxInclusive - minInclusive);
            return (int) (hash * (ulong) range >> 32) + minInclusive;
        }

        public fix Range(fix minInclusive, fix maxInclusive, params int[] ps)
        {
            var hash = XxHash32.ComputeHash(Seed, ps);

            var range = maxInclusive - minInclusive;
            return fix.from_raw(hash) * range + minInclusive;
        }
    }
}
