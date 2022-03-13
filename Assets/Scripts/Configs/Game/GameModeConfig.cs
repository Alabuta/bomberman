using Configs.Level;

namespace Configs.Game
{
    public abstract class GameModeConfig : ConfigBase
    {
        public LevelConfig[] LevelConfigs;
    }
}
