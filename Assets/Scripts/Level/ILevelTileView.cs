using Unity.Mathematics;

namespace Level
{
    public interface ILevelTileView
    {
        LevelTileType Type { get; }
        public int2 Coordinate { get; }
        public float3 WorldPosition { get; }
    }
}
