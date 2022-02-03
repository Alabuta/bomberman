using System;

namespace Data
{
    [Serializable]
    public class PlayerProgress
    {
        public Score Score;

        public WorldData WorldData;

        public PlayerProgress(Score score, LevelStage levelStage)
        {
            Score = score;
            WorldData = new WorldData(levelStage);
        }
    }
}
