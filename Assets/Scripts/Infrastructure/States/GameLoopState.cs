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
            Game.World.StartSimulation();

            _gameStateMachine.FixedUpdateCallback = () => { Game.World.UpdateSimulation(); };

            _gameStateMachine.UpdateCallback = () => { Game.GameStatsView.UpdateLevelStageTimer(Game.World.LevelStageTimer); };
        }
    }
}
