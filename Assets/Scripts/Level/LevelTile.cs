using Items;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Level
{
    public class LevelTile : ILevelTileView
    {
        public LevelTileType Type { get; }

        public int2 Coordinate { get; }
        public fix2 WorldPosition { get; }

        public IItem HoldedItem { get; private set; }

        public LevelTile(LevelTileType type, int2 coordinate, fix2 worldPosition)
        {
            Type = type;
            Coordinate = coordinate;
            WorldPosition = worldPosition;
        }

        public void AddItem(IItem item)
        {
            HoldedItem = item;
        }
    }
}
