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

        public TileLoad TileLoad { get; private set; }

        public IItem HoldedItem { get; private set; }

        public LevelTile(LevelTileType type, int2 coordinate, fix2 worldPosition)
        {
            Type = type;
            Coordinate = coordinate;
            WorldPosition = worldPosition;
        }

        public void SetLoad(TileLoad load)
        {
            TileLoad = load;
        }

        public void AddItem(IItem item)
        {
            HoldedItem = item;
        }

        public void RemoveItem()
        {
            HoldedItem = null;
        }
    }
}
