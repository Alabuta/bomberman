using UnityEngine;

namespace Configs.Game
{
    [CreateAssetMenu(fileName = "GameModePvP", menuName = "Configs/Game/PvP Game Mode")]
    public class GameModePvPConfig : GameModeConfig
    {
        public PlayerConfig[] PlayerConfigs;
    }
}
