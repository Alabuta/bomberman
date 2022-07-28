using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Math.FixedPointMath
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public readonly partial struct fix2 : IEquatable<fix2>, IFormattable
    {
        public readonly fix x;
        public readonly fix y;

        public static readonly fix2 zero = new();
        public static readonly fix2 one = new(fix.one);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(fix x, fix y)
        {
            this.x = x;
            this.y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(fix2 xy)
            : this(xy.x, xy.y)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(fix v)
            : this(v, v)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(bool v)
            : this(v ? fix.one : fix.zero)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(bool2 v)
            : this(v.x ? fix.one : fix.zero, v.y ? fix.one : fix.zero)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(int v)
            : this(new fix(v))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(int x, int y)
            : this((fix) x, (fix) y)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(int2 v)
            : this((fix) v.x, (fix) v.y)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(uint v)
            : this(new fix(v))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(uint x, uint y)
            : this((fix) x, (fix) y)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(uint2 v)
            : this(new fix(v.x), new fix(v.y))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(float v)
            : this(new fix(v))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(float x, float y)
            : this((fix) x, (fix) y)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(float2 v)
            : this(new fix(v.x), new fix(v.y))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(double v)
            : this(new fix(v))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(double x, double y)
            : this((fix) x, (fix) y)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(double2 v)
            : this(new fix(v.x), new fix(v.y))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(Vector2 v)
            : this(new fix(v.x), new fix(v.y))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public fix2(Vector3 v)
            : this(new fix(v.x), new fix(v.y))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator fix2(fix v) => new(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix2(bool v) => new(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix2(bool2 v) => new(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix2(int v) => new(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix2(int2 v) => new(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix2(uint v) => new(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix2(uint2 v) => new(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix2(float v) => new(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix2(float2 v) => new(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix2(double v) => new(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix2(double2 v) => new(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix2(Vector2 v) => new(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator fix2(Vector3 v) => new(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int2(fix2 v) => new((int) v.x, (int) v.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator uint2(fix2 v) => new((uint) v.x, (uint) v.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator float2(fix2 v) => new((float) v.x, (float) v.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator double2(fix2 v) => new((double) v.x, (double) v.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator *(fix2 lhs, fix2 rhs) => new(lhs.x * rhs.x, lhs.y * rhs.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator *(fix2 lhs, fix rhs) => new(lhs.x * rhs, lhs.y * rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator *(fix lhs, fix2 rhs) => new(lhs * rhs.x, lhs * rhs.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator +(fix2 lhs, fix2 rhs) => new(lhs.x + rhs.x, lhs.y + rhs.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator +(fix2 lhs, fix rhs) => new(lhs.x + rhs, lhs.y + rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator +(fix lhs, fix2 rhs) => new(lhs + rhs.x, lhs + rhs.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator -(fix2 lhs, fix2 rhs) => new(lhs.x - rhs.x, lhs.y - rhs.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator -(fix2 lhs, fix rhs) => new(lhs.x - rhs, lhs.y - rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator -(fix lhs, fix2 rhs) => new(lhs - rhs.x, lhs - rhs.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator -(fix2 rhs) => new(-rhs.x, -rhs.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator /(fix2 lhs, fix2 rhs) => new(lhs.x / rhs.x, lhs.y / rhs.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator /(fix2 lhs, fix rhs) => new(lhs.x / rhs, lhs.y / rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator /(fix lhs, fix2 rhs) => new(lhs / rhs.x, lhs / rhs.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator %(fix2 lhs, fix2 rhs) => new(lhs.x % rhs.x, lhs.y % rhs.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator %(fix2 lhs, fix rhs) => new(lhs.x % rhs, lhs.y % rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 operator %(fix lhs, fix2 rhs) => new(lhs % rhs.x, lhs % rhs.y);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator ==(fix2 lhs, fix2 rhs)
        {
            return new bool2(lhs.x == rhs.x, lhs.y == rhs.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator ==(fix2 lhs, fix rhs)
        {
            return new bool2(lhs.x == rhs, lhs.y == rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator ==(fix lhs, fix2 rhs)
        {
            return new bool2(lhs == rhs.x, lhs == rhs.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator !=(fix2 lhs, fix2 rhs) => new(lhs.x != rhs.x, lhs.y != rhs.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator !=(fix2 lhs, fix rhs) => new(lhs.x != rhs, lhs.y != rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator !=(fix lhs, fix2 rhs) => new(lhs != rhs.x, lhs != rhs.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator >(fix2 x, fix2 y) => new(x.x > y.x, x.y > y.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator <(fix2 x, fix2 y) => new(x.x < y.x, x.y < y.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator >=(fix2 x, fix2 y) => new(x.x >= y.x, x.y >= y.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator <=(fix2 x, fix2 y) => new(x.x <= y.x, x.y <= y.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(fix2 rhs) => x == rhs.x && y == rhs.y;

        public override bool Equals(object o) => o is fix2 other && other.Equals(this);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            unchecked
            {
                return (x.GetHashCode() * 397) ^ y.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() =>
            $"fix2({x}, {y})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider) =>
            $"fix2({x.ToString(format, formatProvider)}, {y.ToString(format, formatProvider)})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 select(fix2 a, fix2 b, bool c) => c ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static fix2 select(fix2 a, fix2 b, bool2 c) => new(c.x ? b.x : a.x, c.y ? b.y : a.y);
    }
}
