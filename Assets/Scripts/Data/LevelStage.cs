using System;
using Configs.Game;
using Configs.Level;

namespace Data
{
    [Serializable]
    public class LevelStage
    {
        public GameModeConfig GameModeConfig;

        public LevelConfig LevelConfig;
        public LevelStageConfig LevelStageConfig;

        public uint RandomSeed { get; }

        public LevelStage(GameModeConfig gameModeConfig, LevelConfig levelConfig, LevelStageConfig levelStageConfig)
        {
            GameModeConfig = gameModeConfig;
            LevelConfig = levelConfig;
            LevelStageConfig = levelStageConfig;
            RandomSeed = levelStageConfig.RandomSeed;
        }
    }
}
