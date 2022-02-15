using System;

namespace Level
{
    [Flags]
    public enum LevelTileType
    {
        FloorTile = 0,
        HardBlock = 1,
        SoftBlock = 2
    }
}
