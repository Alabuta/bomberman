using Configs.Enemy;
using Configs.Level.Tile;
using Configs.PowerUp;
using Unity.Mathematics;
using UnityEngine;

namespace Configs.Level
{
    [CreateAssetMenu(menuName = "Configs/Level/Level Config")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Header("General Parameters")]
        public string Name;

        [Range(13, 65)]
        public int ColumnsNumber;
        [Range(11, 63)]
        public int RowsNumber;

        public int OriginalPixelsPerUnits = 16;

        [Range(0, 100)]
        public int SoftBlocksCoverage = 30;

        public int4 ViewportPadding = int4.zero;

        public int2[] PlayersSpawnCorners = {int2.zero};

        [Header("General Prefabs")]
        public GameObject Walls;

        [Header("Enemy Parameters")]
        public EnemyConfig[] Enemies;
        public EnemyConfig[] PortalEnemies;

        public EnemyConfig[] TimeIsUpEnemyConfigs;

        [Header("Block Parameters")]
        public PortalBlock PortalBlock;

        public HardBlock HardBlock;
        public SoftBlock SoftBlock;

        [Space(24)]
        public PowerUpConfigBase[] PowerUps;

        // public ItemsConfigBase[] Items;
    }
}
