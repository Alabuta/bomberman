using System;

namespace Data
{
    [Serializable]
    public class PlayerProgress
    {
        public Score Score;

        public WorldData WorldData;

        public PlayerProgress(LevelStage levelStage)
        {
        }
    }
}
