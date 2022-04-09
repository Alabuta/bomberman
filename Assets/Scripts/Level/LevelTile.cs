using Game.Colliders;
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

        public ICollider Collider { get; }

        public IItem HoldedItem { get; private set; }

        public LevelTile(LevelTileType type, ICollider collider, int2 coordinate, fix2 worldPosition)
        {
            Type = type;
            Coordinate = coordinate;
            WorldPosition = worldPosition;
            Collider = collider;
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
