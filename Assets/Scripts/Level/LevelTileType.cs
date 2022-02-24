using System;

namespace Level
{
    [Flags]
    public enum LevelTileType
    {
        Nothing = 0,
        FloorTile = 1,
        HardBlock = 2,
        SoftBlock = 3
    }
}
