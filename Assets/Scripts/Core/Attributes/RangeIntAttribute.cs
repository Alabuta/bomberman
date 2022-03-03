using UnityEngine;

namespace Core.Attributes
{
    public class RangeIntAttribute : PropertyAttribute
    {
        public readonly int Min;
        public readonly int Max;

        public RangeIntAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }
}
