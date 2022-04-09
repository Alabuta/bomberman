using Items;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Level
{
    public interface ILevelTileView
    {
        LevelTileType Type { get; }
        public int2 Coordinate { get; }
        public fix2 WorldPosition { get; }
        IItem HoldedItem { get; }

        void AddItem(IItem item);

        void RemoveItem();
    }
}
