using System;

namespace Data
{
    [Serializable]
    public class PlayerProgress
    {
        public Score Score;
        public HealthState HeroHealthState;
        public WorldData WorldData;

        public PlayerProgress(Score score, LevelStage levelStage)
        {
            Score = score;
            HeroHealthState = new HealthState();
            WorldData = new WorldData(levelStage);
        }
    }
}
