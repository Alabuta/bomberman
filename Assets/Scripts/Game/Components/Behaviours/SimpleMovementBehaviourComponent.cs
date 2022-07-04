using Math.FixedPointMath;
using Unity.Mathematics;

namespace Game.Components.Behaviours
{
    public struct SimpleMovementBehaviourComponent
    {
        public int2[] MovementDirections;
        public bool TryToSelectNewTile;
        public fix DirectionChangeChance;

        public fix2 FromWorldPosition;
        public fix2 ToWorldPosition;
    }
}
