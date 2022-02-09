using System;
using Configs.Entity;
using Configs.Items;
using Unity.Mathematics;
using UnityEngine;

namespace Configs.Level
{
    [CreateAssetMenu(fileName = "LevelStage", menuName = "Configs/Level/Level Stage")]
    public sealed class LevelStageConfig : ConfigBase
    {
        [Header("General Parameters")]
        public int Index;

        public int RandomSeed;

        [Range(13, 65)]
        public int ColumnsNumber;
        [Range(11, 63)]
        public int RowsNumber;

        [Range(0, 100)]
        public int SoftBlocksCoverage = 30;

        [Space(16)]
        public int2[] PlayersSpawnCorners = { int2.zero };

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
