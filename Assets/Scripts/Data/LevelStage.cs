using System;

namespace Data
{
    [Serializable]
    public class LevelStage
    {
        public int LevelIndex;
        public int LevelStageIndex;

        public LevelStage(int levelIndex, int levelStageIndex)
        {
            LevelIndex = levelIndex;
            LevelStageIndex = levelStageIndex;
        }
    }
}
