using Configs.Enemy;
using Configs.Items;
using Unity.Mathematics;
using UnityEngine;

namespace Configs.Level
{
    [CreateAssetMenu(fileName = "LevelStageConfig", menuName = "Configs/Level/Level Stage Config")]
    public sealed class LevelStageConfig : ScriptableObject
    {
        [Header("General Parameters")]
        public int Index;

        [Range(13, 65)]
        public int ColumnsNumber;
        [Range(11, 63)]
        public int RowsNumber;

        [Range(0, 100)]
        public int SoftBlocksCoverage = 30;

        [Space(16)]
        public int2[] PlayersSpawnCorners = {int2.zero};

        [Space(16)]
        public EnemyConfig[] Enemies;
        public EnemyConfig[] PortalEnemies;
        public EnemyConfig[] TimeIsUpEnemyConfigs;

        [Space(16)]
        public PowerUpItemConfigBase[] PowerUpItems;

        // public ItemsConfigBase[] Items;
    }
}
