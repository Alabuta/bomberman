namespace Infrastructure.States
{
    public class GameLoopState : IGameState
    {
        private readonly GameStateMachine _gameStateMachine;

        public GameLoopState(GameStateMachine gameStateMachine)
        {
            _gameStateMachine = gameStateMachine;
        }

        public void Exit()
        {
        }

        public void Enter()
        {
            Game.LevelManager.StartSimulation();

            _gameStateMachine.UpdateCallback = () =>
            {
                Game.LevelManager.UpdateSimulation();
                Game.GameStatsView.UpdateLevelStageTimer(Game.LevelManager.LevelStageTimer);
            };
        }
    }
}
