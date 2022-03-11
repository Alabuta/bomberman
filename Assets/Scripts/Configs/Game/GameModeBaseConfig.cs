using Configs.Level;

namespace Configs.Game
{
    public abstract class GameModeBaseConfig : ConfigBase
    {
        public LevelConfig[] LevelConfigs;
    }
}
