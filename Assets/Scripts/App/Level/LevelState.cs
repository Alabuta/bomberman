namespace App.Level
{
    public struct LevelState
    {
        /*
         * In-Game current state data
         * hero HP, alive mobs number, etc...
         */

        public readonly LevelGridModel LevelGridModel;

        public LevelState(LevelGridModel levelGridModel)
        {
            LevelGridModel = levelGridModel;
        }
    }
}
