using Unity.Mathematics;

namespace Level
{
    public class LevelTile : ILevelTileView
    {
        public LevelTileType Type { get; }

        public int2 Coordinate { get; }
        public float3 WorldPosition { get; }

        public LevelTile(LevelTileType type, int2 coordinate, float3 worldPosition)
        {
            Type = type;
            Coordinate = coordinate;
            WorldPosition = worldPosition;
        }
    }
}
