using Math.FixedPointMath;
using Unity.Mathematics;

namespace Level
{
    public interface ILevelTileView
    {
        LevelTileType Type { get; }
        int2 Coordinate { get; }
        fix2 WorldPosition { get; }
        ITileLoad TileLoad { get; }
    }
}
