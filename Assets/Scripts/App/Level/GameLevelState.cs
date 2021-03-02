namespace App.Level
{
    public struct GameLevelState
    {
        /*
         * In-Game current state data
         * hero HP, alive mobs number, etc...
         */

        public readonly GameLevelGridModel GameLevelGridModel;

        public GameLevelState(GameLevelGridModel gameLevelGridModel)
        {
            GameLevelGridModel = gameLevelGridModel;
        }
    }
}
