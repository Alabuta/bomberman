using Level;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Game.Components.Entities
{
    public struct LevelTileComponent
    {
        public LevelTileType Type;

        public int2 Coordinate;
        public fix2 WorldPosition;
    }
}
