using System;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.Attributes
{

    [Serializable]
    public struct RangeInt
    {
        public int Min;
        public int Max;

        public RangeInt(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public int GetRandom() => (int) Mathf.Round(Random.Range(Min, Max));

        public static implicit operator int(RangeInt rangeInt) => rangeInt.GetRandom();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("[Class: {0}, Min: {1}, Max: {2}]", nameof(RangeInt), Min, Max);

            return sb.ToString();
        }
    }
}
