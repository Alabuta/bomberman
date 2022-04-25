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

            _gameStateMachine.FixedUpdateCallback = () => { Game.World.UpdateWorldModel(); };

            _gameStateMachine.UpdateCallback = () =>
            {
                Game.World.UpdateWorldView();
                Game.GameStatsView.UpdateLevelStageTimer(Game.World.StageTimer);
            };
        }
    }
}
