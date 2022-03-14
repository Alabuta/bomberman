using Configs.Level;
using UnityEngine;

namespace Configs.Game
{
    public abstract class GameModeConfig : ConfigBase
    {
        public LevelConfig[] LevelConfigs;

        public GameObject HeroStatsViewPrefab;
    }
}
