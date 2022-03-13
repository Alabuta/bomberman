using Unity.Mathematics;
using UnityEngine;

namespace Configs.Level
{
    [CreateAssetMenu(fileName = "PvELevelStage", menuName = "Configs/Level/PvE Level Stage")]
    public class PvELevelStageConfig : LevelStageConfig
    {
        public int2 PlayerSpawnCorner;
    }
}
