using System;

namespace Data
{
    [Serializable]
    public class PlayerProgress
    {
        public Score Score;
        public State HeroState;
        public WorldData WorldData;

        public PlayerProgress(Score score, LevelStage levelStage)
        {
            Score = score;
            HeroState = new State();
            WorldData = new WorldData(levelStage);
        }
    }
}
