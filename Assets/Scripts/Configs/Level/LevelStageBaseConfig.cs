using System;
using Configs.Entity;
using Configs.Items;
using UnityEngine;

namespace Configs.Level
{
    public abstract class LevelStageBaseConfig : ConfigBase
    {
        [Header("General Parameters")]
        public int Index;

        public int RandomSeed;

        [Range(13, 65)]
        public int ColumnsNumber = 13;
        [Range(11, 63)]
        public int RowsNumber = 11;

        [Range(0, 100)]
        public int SoftBlocksCoverage = 30;

        [Space(16)]
        public EnemySpawnElement[] Enemies;

        public EnemyConfig[] PortalEnemies;
        public EnemyConfig[] TimeIsUpEnemyConfigs;

        [Space(16)]
        public ItemConfigBase[] PowerUpItems;

        public BombConfig BombConfig;
        // public ItemsConfigBase[] Items;
    }

    [Serializable]
    public struct EnemySpawnElement
    {
        public EnemyConfig EnemyConfig;
        public int Count;
    }
}
