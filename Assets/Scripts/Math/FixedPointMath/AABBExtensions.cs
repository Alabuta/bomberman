using System;

namespace Math.FixedPointMath
{
    public static class AABBExtensions
    {
        [Flags]
        public enum OutCode
        {
            Inside = 0b0000,
            Left = 0b0001,
            Right = 0b0010,
            Bottom = 0b0100,
            Top = 0b1000
        }

        public static OutCode ComputeOutCode(this AABB aabb, fix2 point)
        {
            var outCode = OutCode.Inside;

            if (point.x < aabb.min.x)
                outCode |= OutCode.Left;
            else if (point.x > aabb.max.x)
                outCode |= OutCode.Right;

            if (point.y < aabb.min.y)
                outCode |= OutCode.Bottom;
            else if (point.y > aabb.max.y)
                outCode |= OutCode.Top;

            return outCode;
        }

        // Cohen–Sutherland clipping algorithm clips a line from
        // p0 to p1 against a rectangle with diagonal from min to max.
        // https://en.wikipedia.org/wiki/Cohen%E2%80%93Sutherland_algorithm
        public static bool CohenSutherlandLineClip(this AABB aabb, ref fix2 p0, ref fix2 p1)
        {
            // Compute outcodes for p0, p1, and whatever point lies outside the clip rectangle
            var outCode0 = ComputeOutCode(aabb, p0);
            var outCode1 = ComputeOutCode(aabb, p1);

            var accept = false;

            while (true)
            {
                if ((outCode0 | outCode1) == 0)
                {
                    // Bitwise OR is 0: both points inside window; trivially accept and exit loop
                    accept = true;
                    break;
                }

                if ((outCode0 & outCode1) != 0)
                {
                    // Bitwise AND is not 0: both points share an outside zone (LEFT, RIGHT, TOP,
                    // or BOTTOM), so both must be outside window; exit loop (accept is false)
                    break;
                }

                // Failed both tests, so calculate the line segment to clip
                // from an outside point to an intersection with clip edge
                var outside = new[] { fix.zero, fix.zero };

                // At least one endpoint is outside the clip rectangle; pick it.
                var outCodeOut = outCode1 > outCode0 ? outCode1 : outCode0;

                // Now find the intersection point;
                // use formulas:
                //   slope = (y1 - y0) / (x1 - x0)
                //   x = x0 + (1 / slope) * (ym - y0), where ym is ymin or ymax
                //   y = y0 + slope * (xm - x0), where xm is xmin or xmax

                var direction = p1 - p0;

                if ((outCodeOut & OutCode.Top) != 0)
                {
                    // Point is above the clip window
                    outside[0] = p0.x + (aabb.max.y - p0.y) * (direction.x / direction.y);
                    outside[1] = aabb.max.y;
                }
                else if ((outCodeOut & OutCode.Bottom) != 0)
                {
                    // Point is below the clip window
                    outside[0] = p0.x + (aabb.min.y - p0.y) * (direction.x / direction.y);
                    outside[1] = aabb.min.y;
                }
                else if ((outCodeOut & OutCode.Right) != 0)
                {
                    // Point is to the right of clip window
                    outside[0] = aabb.max.x;
                    outside[1] = p0.y + (aabb.max.x - p0.x) * (direction.y / direction.x);
                }
                else if ((outCodeOut & OutCode.Left) != 0)
                {
                    // Point is to the left of clip window
                    outside[0] = aabb.min.x;
                    outside[1] = p0.y + (aabb.min.x - p0.x) * (direction.y / direction.x);
                }

                // Now we move outside point to intersection point to clip
                // and get ready for next pass.
                if (outCodeOut == outCode0)
                {
                    p0 = new fix2(outside[0], outside[1]);
                    outCode0 = ComputeOutCode(aabb, p0);
                }
                else
                {
                    p1 = new fix2(outside[0], outside[1]);
                    outCode1 = ComputeOutCode(aabb, p1);
                }
            }

            return accept;
        }

        public static AABB CreateFromPositionAndSize(fix2 position, fix2 size)
        {
            var halfSize = size / new fix2(2);
            return new AABB(position - halfSize, position + halfSize);
        }
    }
}
