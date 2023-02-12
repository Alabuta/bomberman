using System;
using Configs.Entity;
using Configs.Items;
using UnityEngine;

namespace Configs.Level
{
    public abstract class LevelStageConfig : ConfigBase
    {
        [Header("General Parameters")]
        public int Index;

        public uint RandomSeed;

        public int LevelStageTimer = 4 * 60;

        [Range(13, 99)]
        public int ColumnsNumber = 13;
        [Range(11, 99)]
        public int RowsNumber = 11;

        [Range(0, 100)]
        public int SoftBlocksCoverage = 30;

        [Space(16)]
        public EnemySpawnElement[] Enemies;

        public EnemyConfig[] PortalEnemies;
        public EnemyConfig[] TimeIsUpEnemyConfigs;

        [Space(16)]
        public ItemConfig[] PowerUpItems;
        // public ItemsConfigBase[] Items;
    }

    [Serializable]
    public struct EnemySpawnElement
    {
        public EnemyConfig EnemyConfig;
        public int Count;
    }
}
