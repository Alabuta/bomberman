using Unity.Mathematics;
using UnityEngine;

namespace Configs.Level
{
    [CreateAssetMenu(fileName = "PvPLevelStage", menuName = "Configs/Level/PvP Level Stage")]
    public class LevelStagePvPConfig : LevelStageConfig
    {
        [Space(16)]
        public int2[] PlayersSpawnCorners = { int2.zero };
    }
}
