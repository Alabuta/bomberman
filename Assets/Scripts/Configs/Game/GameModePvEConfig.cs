using Configs.Level;
using UnityEngine;

namespace Configs.Game
{
    [CreateAssetMenu(fileName = "GameModePvE", menuName = "Configs/Game/PvE Game Mode")]
    public sealed class GameModePvEConfig : ConfigBase
    {
        public LevelConfig[] Levels;

        public PlayerConfig[] Players;
    }
}
