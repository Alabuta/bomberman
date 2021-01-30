namespace App.Level
{
    public struct LevelState
    {
        /*
         * In-Game current state data
         * hero HP, alive mobs number, etc...
         */

        public readonly LevelGrid LevelGrid;

        public LevelState(LevelGrid levelGrid)
        {
            LevelGrid = levelGrid;
        }
    }
}
