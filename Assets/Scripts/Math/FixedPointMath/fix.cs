using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Math.FixedPointMath
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public readonly partial struct fix : IEquatable<fix>, IComparable<fix>, IFormattable
    {
        private const int NUM_BITS = 64;
        private const int FRAC_BITS = 32;
        private const long ONE = 1L << FRAC_BITS;
        private const long MAX_VALUE = int.MaxValue * ONE;
        private const long MIN_VALUE = int.MinValue * ONE;
        private static readonly fix Log2Max = new(0x1F00000000, true);
        private static readonly fix Log2Min = new(-0x2000000000, true);
        private static readonly fix Ln2 = new(0xB17217F7, true);

        public static readonly fix one = new(ONE, true);
        public static readonly fix zero = new();

        public static readonly fix Epsilon = new(1L, true);
        public static readonly fix MaxValue = new(MAX_VALUE, true);
        public static readonly fix MinValue = new(MIN_VALUE, true);

        public static readonly fix Pi = new(0x3243F6A88, true);
        public static readonly fix PiOver2 = new(0x1921FB544, true);
        public static readonly fix PiTimes2 = new(0x6487ED511, true);

        public static fix from_raw(long raw) => new(raw, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private fix(long raw, bool _) => Raw = raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix(fix x)
            : this(x.Raw, true)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix(long v)
            : this(v * ONE, true)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix(int v)
            : this(v * ONE, true)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix(uint v)
            : this(v * ONE, true)
        {
            if (v >= (uint) MaxValue)
                Raw = MaxValue.Raw;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix(float v)
            : this((long) (v * ONE), true)
        {
            if (v >= (float) MaxValue)
                Raw = MaxValue.Raw;

            else if (v <= (float) MinValue)
                Raw = MinValue.Raw;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix(double v)
            : this((long) (v * ONE), true)
        {
            if (v >= (double) MaxValue)
                Raw = MaxValue.Raw;

            else if (v <= (double) MinValue)
                Raw = MinValue.Raw;
        }

        public long Raw { get; }

        public override bool Equals(object obj) => obj is fix other && ((ValueType) other).Equals(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => Raw.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(fix other) => Raw == other.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(fix other) => Raw.CompareTo(other.Raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => ((double) this).ToString(CultureInfo.InvariantCulture);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(IFormatProvider formatProvider) =>
            ((double) this).ToString(formatProvider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider) =>
            ((double) this).ToString(format, formatProvider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix(long value) => new(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix(int value) => new(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix(uint value) => new(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix(double value) => new(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix(float value) => new(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator long(fix value) => value.Raw >> FRAC_BITS;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(fix value) => (int) (value.Raw >> FRAC_BITS);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator uint(fix value) => (uint) (value.Raw >> FRAC_BITS);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator float(fix value) => (float) value.Raw / ONE;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator double(fix value) => (double) value.Raw / ONE;
    }
}
