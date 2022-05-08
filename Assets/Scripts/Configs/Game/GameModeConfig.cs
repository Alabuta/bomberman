using Configs.Level;
using UnityEngine.AddressableAssets;

namespace Configs.Game
{
    public abstract class GameModeConfig : ConfigBase
    {
        public LevelConfig[] LevelConfigs;

        public AssetReferenceGameObject GameStatsViewPrefab;
    }
}
