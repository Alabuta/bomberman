using Configs.Level;
using UnityEngine;

namespace Configs.Game
{
    [CreateAssetMenu(fileName = "GameModePvE", menuName = "Configs/Game/PvE Mode")]
    public sealed class GameModePvE : ScriptableObject
    {
        public LevelConfig[] LevelConfigs;
    }
}
