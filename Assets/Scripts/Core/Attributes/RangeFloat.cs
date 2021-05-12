using System;
using System.Text;
using Random = UnityEngine.Random;

namespace Core.Attributes
{
    [Serializable]
    public struct RangeFloat
    {
        public float Min;
        public float Max;

        public RangeFloat(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float GetRandom() => Random.Range(Min, Max);

        public static implicit operator float(RangeFloat rangeFloat) => rangeFloat.GetRandom();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("[Class: {0}, Min: {1}, Max: {2}]", nameof(RangeFloat), Min, Max);

            return sb.ToString();
        }
    }
}
