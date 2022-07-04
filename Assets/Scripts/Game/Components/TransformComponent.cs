using Math.FixedPointMath;
using Unity.Mathematics;

namespace Game.Components
{
    public struct TransformComponent
    {
        public fix2 WorldPosition;
        public int2 Direction;
        public fix Speed;
    }
}
