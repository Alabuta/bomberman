using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Math.FixedPointMath
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public readonly partial struct fix
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int sign(fix value)
        {
            return
                value.Raw < 0 ? -1 :
                value.Raw > 0 ? 1 :
                0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix abs(fix value) => value.Raw == MIN_VALUE ? MaxValue : fast_abs(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix max(fix x, fix y) => x > y ? x : y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix min(fix x, fix y) => x < y ? x : y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix select(fix a, fix b, bool c) => c ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix fast_abs(fix value)
        {
            // branchless implementation, see http://www.strchr.com/optimized_abs_function
            var mask = value.Raw >> 63;
            return new fix((value.Raw + mask) ^ mask, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix floor(fix value) => new((long) ((ulong) value.Raw & 0xFFFFFFFF00000000), true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix ceil(fix value) =>
            (value.Raw & 0x00000000FFFFFFFF) == 0 ? value : floor(value) + one;

        public static fix round(fix value)
        {
            var fractionalPart = value.Raw & 0x00000000FFFFFFFF;
            var integralPart = floor(value);
            if (fractionalPart < 0x80000000)
                return integralPart;
            if (fractionalPart > 0x80000000)
                return integralPart + one;

            // if number is halfway between two values, round to the nearest even number
            // this is the method used by System.Math.Round().
            return (integralPart.Raw & ONE) == 0
                ? integralPart
                : integralPart + one;
        }

        public static fix operator +(fix x, fix y)
        {
            var xl = x.Raw;
            var yl = y.Raw;
            var sum = xl + yl;
            // if signs of operands are equal and signs of sum and x are different
            if ((~(xl ^ yl) & (xl ^ sum) & MIN_VALUE) != 0)
                sum = xl > 0 ? MAX_VALUE : MIN_VALUE;
            return new fix(sum, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix fast_add(fix x, fix y) => new(x.Raw + y.Raw, true);

        public static fix operator -(fix x, fix y)
        {
            var xl = x.Raw;
            var yl = y.Raw;
            var diff = xl - yl;
            // if signs of operands are different and signs of sum and x are different
            if (((xl ^ yl) & (xl ^ diff) & MIN_VALUE) != 0)
                diff = xl < 0 ? MIN_VALUE : MAX_VALUE;
            return new fix(diff, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix fast_sub(fix x, fix y) => new(x.Raw - y.Raw, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long AddOverflowHelper(long x, long y, ref bool overflow)
        {
            var sum = x + y;
            // x + y overflows if sign(x) ^ sign(y) != sign(sum)
            overflow |= ((x ^ y ^ sum) & MIN_VALUE) != 0;
            return sum;
        }

        public static fix operator *(fix x, fix y)
        {
            var xl = x.Raw;
            var yl = y.Raw;

            var xlo = (ulong) (xl & 0x00000000FFFFFFFF);
            var xhi = xl >> FRAC_BITS;
            var ylo = (ulong) (yl & 0x00000000FFFFFFFF);
            var yhi = yl >> FRAC_BITS;

            var lolo = xlo * ylo;
            var lohi = (long) xlo * yhi;
            var hilo = xhi * (long) ylo;
            var hihi = xhi * yhi;

            var loResult = lolo >> FRAC_BITS;
            var midResult1 = lohi;
            var midResult2 = hilo;
            var hiResult = hihi << FRAC_BITS;

            var overflow = false;
            var sum = AddOverflowHelper((long) loResult, midResult1, ref overflow);
            sum = AddOverflowHelper(sum, midResult2, ref overflow);
            sum = AddOverflowHelper(sum, hiResult, ref overflow);

            var opSignsEqual = ((xl ^ yl) & MIN_VALUE) == 0;

            // if signs of operands are equal and sign of result is negative,
            // then multiplication overflowed positively
            // the reverse is also true
            if (opSignsEqual)
            {
                if (sum < 0 || overflow && xl > 0)
                    return MaxValue;
            }
            else if (sum > 0)
                return MinValue;

            // if the top 32 bits of hihi (unused in the result) are neither all 0s or 1s,
            // then this means the result overflowed.
            var topCarry = hihi >> FRAC_BITS;
            if (topCarry != 0 && topCarry != -1/*&& xl != -17 && yl != -17*/)
                return opSignsEqual ? MaxValue : MinValue;

            // If signs differ, both operands' magnitudes are greater than 1,
            // and the result is greater than the negative operand, then there was negative overflow.
            if (!opSignsEqual)
            {
                var (posOp, negOp) = xl > yl ? (xl, yl) : (yl, xl);
                if (sum > negOp && negOp < -ONE && posOp > ONE)
                    return MinValue;
            }

            return new fix(sum, true);
        }

        [Obsolete("Use operator *(fix x, fix y) instead")]
        public static fix fast_mul(fix x, fix y)
        {
            var xl = x.Raw;
            var yl = y.Raw;

            var xlo = (ulong) (xl & 0x00000000FFFFFFFF);
            var xhi = xl >> FRAC_BITS;
            var ylo = (ulong) (yl & 0x00000000FFFFFFFF);
            var yhi = yl >> FRAC_BITS;

            var lolo = xlo * ylo;
            var lohi = (long) xlo * yhi;
            var hilo = xhi * (long) ylo;
            var hihi = xhi * yhi;

            var loResult = lolo >> FRAC_BITS;
            var midResult1 = lohi;
            var midResult2 = hilo;
            var hiResult = hihi << FRAC_BITS;

            var sum = (long) loResult + midResult1 + midResult2 + hiResult;
            return new fix(sum, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountLeadingZeroes(ulong x)
        {
            var result = 0;
            while ((x & 0xF000000000000000) == 0)
            {
                result += 4;
                x <<= 4;
            }

            while ((x & 0x8000000000000000) == 0)
            {
                result += 1;
                x <<= 1;
            }

            return result;
        }

        public static fix operator /(fix x, fix y)
        {
            var xl = x.Raw;
            var yl = y.Raw;

            if (yl == 0)
                throw new DivideByZeroException();

            var remainder = (ulong) (xl >= 0 ? xl : -xl);
            var divider = (ulong) (yl >= 0 ? yl : -yl);
            var quotient = 0UL;
            var bitPos = NUM_BITS / 2 + 1;


            // If the divider is divisible by 2^n, take advantage of it.
            while ((divider & 0xF) == 0 && bitPos >= 4)
            {
                divider >>= 4;
                bitPos -= 4;
            }

            while (remainder != 0 && bitPos >= 0)
            {
                int shift = CountLeadingZeroes(remainder);
                if (shift > bitPos)
                    shift = bitPos;
                remainder <<= shift;
                bitPos -= shift;

                var div = remainder / divider;
                remainder = remainder % divider;
                quotient += div << bitPos;

                // Detect overflow
                if ((div & ~(0xFFFFFFFFFFFFFFFF >> bitPos)) != 0)
                    return ((xl ^ yl) & MIN_VALUE) == 0 ? MaxValue : MinValue;

                remainder <<= 1;
                --bitPos;
            }

            // rounding
            ++quotient;
            var result = (long) (quotient >> 1);
            if (((xl ^ yl) & MIN_VALUE) != 0)
                result = -result;

            return new fix(result, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix operator %(fix x, fix y) =>
            new(x.Raw == MIN_VALUE & y.Raw == -1 ? 0 : x.Raw % y.Raw, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix fast_mod(fix x, fix y) => new(x.Raw % y.Raw, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix operator -(fix x) => x.Raw == MIN_VALUE ? MaxValue : new fix(-x.Raw, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(fix x, fix y) => x.Raw == y.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(fix x, fix y) => x.Raw != y.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(fix x, fix y) => x.Raw > y.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(fix x, fix y) => x.Raw < y.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(fix x, fix y) => x.Raw >= y.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(fix x, fix y) => x.Raw <= y.Raw;

        private static fix Pow2(fix x)
        {
            if (x.Raw == 0)
                return one;

            // Avoid negative arguments by exploiting that exp(-x) = 1/exp(x).
            var neg = x.Raw < 0;
            if (neg)
                x = -x;

            if (x == one)
                return neg ? one / (fix) 2 : (fix) 2;
            if (x >= Log2Max)
                return neg ? one / MaxValue : MaxValue;
            if (x <= Log2Min)
                return neg ? MaxValue : zero;

            /* The algorithm is based on the power series for exp(x):
             * http://en.wikipedia.org/wiki/Exponential_function#Formal_definition
             *
             * From term n, we get term n+1 by multiplying with x/n.
             * When the sum term drops to zero, we can stop summing.
             */

            var integerPart = (int) floor(x);
            // Take fractional part of exponent
            x = new fix(x.Raw & 0x00000000FFFFFFFF, true);

            var result = one;
            var term = one;
            var i = 1;
            while (term.Raw != 0)
            {
                term = x * term * Ln2 / (fix) i;
                result += term;
                i++;
            }

            result = from_raw(result.Raw << integerPart);
            if (neg)
                result = one / result;
            return result;
        }

        private static fix Log2(fix x)
        {
            if (x.Raw <= 0)
                throw new ArgumentOutOfRangeException(nameof(x), "Non-positive value passed to Ln");

            // This implementation is based on Clay. S. Turner's fast binary logarithm
            // algorithm (C. S. Turner,  "A Fast Binary Logarithm Algorithm", IEEE Signal
            //     Processing Mag., pp. 124,140, Sep. 2010.)

            long b = 1U << (FRAC_BITS - 1);
            long y = 0;

            if (x == zero)
                return zero;

            long rawX = x.Raw;
            while (rawX < ONE)
            {
                rawX <<= 1;
                y -= ONE;
            }

            while (rawX >= ONE << 1)
            {
                rawX >>= 1;
                y += ONE;
            }

            var z = new fix(rawX, true);

            for (int i = 0; i < FRAC_BITS; i++)
            {
                z *= z;
                if (z.Raw >= ONE << 1)
                {
                    z = new fix(z.Raw >> 1, true);
                    y += b;
                }

                b >>= 1;
            }

            return new fix(y, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix ln(fix x) => Log2(x) * Ln2;

        public static fix pow(fix b, fix exp)
        {
            if (b == one)
                return one;
            if (exp.Raw == 0)
                return one;

            if (b.Raw == 0)
            {
                if (exp.Raw < 0)
                    throw new DivideByZeroException();

                return zero;
            }

            var log2 = Log2(b);
            return Pow2(exp * log2);
        }

        public static fix sqrt(fix x)
        {
            var xl = x.Raw;
            if (xl < 0)
                throw new ArgumentOutOfRangeException(nameof(x), "Negative value passed to Sqrt");

            var num = (ulong) xl;
            var result = 0UL;

            // second-to-top bit
            var bit = 1UL << (NUM_BITS - 2);

            while (bit > num)
                bit >>= 2;

            // The main part is executed twice, in order to avoid
            // using 128 bit values in computations.
            for (var i = 0; i < 2; ++i)
            {
                // First we get the top 48 bits of the answer.
                while (bit != 0)
                {
                    if (num >= result + bit)
                    {
                        num -= result + bit;
                        result = (result >> 1) + bit;
                    }
                    else
                    {
                        result = result >> 1;
                    }

                    bit >>= 2;
                }

                if (i == 0)
                {
                    // Then process it again to get the lowest 16 bits.
                    if (num > (1UL << (NUM_BITS / 2)) - 1)
                    {
                        // The remainder 'num' is too large to be shifted left
                        // by 32, so we have to add 1 to result manually and
                        // adjust 'num' accordingly.
                        // num = a - (result + 0.5)^2
                        //       = num + result^2 - (result + 0.5)^2
                        //       = num - result - 0.5
                        num -= result;
                        num = (num << (NUM_BITS / 2)) - 0x80000000UL;
                        result = (result << (NUM_BITS / 2)) + 0x80000000UL;
                    }
                    else
                    {
                        num <<= NUM_BITS / 2;
                        result <<= NUM_BITS / 2;
                    }

                    bit = 1UL << (NUM_BITS / 2 - 2);
                }
            }

            // Finally, if next bit would have been 1, round the result upwards.
            if (num > result)
                ++result;
            return new fix((long) result, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix clamp(fix x, fix a, fix b)
        {
            return max(a, min(b, x));
        }
    }
}
