using System;

namespace Data
{
    [Serializable]
    public class WorldData
    {
        public LevelStage LevelStage;

        public WorldData(LevelStage levelStage)
        {
            LevelStage = levelStage;
        }
    }
}
