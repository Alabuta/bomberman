using Configs.Enemy;
using Configs.Level.Tile;
using Unity.Mathematics;
using UnityEngine;

namespace Configs.Level
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Configs/Level/Level Config", order = 1)]
    public sealed class LevelConfig : ScriptableObject
    {
        [Header("General Configs")]
        public string Name;

        [Range(13, 65)]
        public int ColumnsNumber;
        [Range(11, 63)]
        public int RowsNumber;

        [Range(0, 100)]
        public int SoftBlocksCoverage = 30;

        public int4 CameraViewportPadding = int4.zero;

        public int2[] PlayersSpawnCorners = {int2.zero};

        [Header("General Prefabs")]
        public GameObject Walls;

        [Header("Enemy Configs")]
        public EnemyConfig[] Enemies;
        public EnemyConfig[] PortalEnemies;

        public uint TimeIsUpEnemiesNumber = 20;
        public EnemyConfig TimeIsUpEnemyConfig;

        [Header("Block Configs")]
        public PortalBlock PortalBlock;

        public HardBlock HardBlock;
        public SoftBlock SoftBlock;

        /*
         * [Header("PowerUp Configs")]
         * public PowerUpConfig[] PowerUps;
         *
         * [Header("Pit and Hazard Configs")]
         * ...
         */
    }
}
