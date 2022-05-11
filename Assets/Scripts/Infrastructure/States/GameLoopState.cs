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
        }

        public Task Enter()
        {
            _gameStateMachine.FixedUpdateCallback = () => { Game.World.UpdateWorldModel(); };

            _gameStateMachine.UpdateCallback = () =>
            {
                Game.World.UpdateWorldView();
                Game.GameStatsView.UpdateLevelStageTimer(Game.World.StageTimer);
            };

            Game.World.StartSimulation();

            return Task.CompletedTask;
        }
    }
}
