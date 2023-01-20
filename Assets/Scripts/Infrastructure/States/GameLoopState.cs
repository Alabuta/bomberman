using System.Threading.Tasks;

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
            Game.World?.Dispose();
        }

        public Task Enter()
        {
            _gameStateMachine.FixedUpdateCallback = FixedUpdateCallback;
            _gameStateMachine.UpdateCallback = UpdateCallback;

            Game.World.StartSimulation();

            return Task.CompletedTask;
        }

        private static void UpdateCallback()
        {
            Game.World.UpdateWorldView();
            Game.GameStatsView.UpdateLevelStageTimer(Game.World.StageTimer);
        }

        private static void FixedUpdateCallback()
        {
            Game.World.UpdateWorldModel();
        }
    }
}
