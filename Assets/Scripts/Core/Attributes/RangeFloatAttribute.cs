using UnityEngine;

namespace Core.Attributes
{
    public class RangeFloatAttribute : PropertyAttribute
    {
        public readonly float Min;
        public readonly float Max;

        public RangeFloatAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}
