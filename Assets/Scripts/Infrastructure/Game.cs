using Infrastructure.Services;
using Infrastructure.States;
using Level;
using UI;

namespace Infrastructure
{
    public class Game
    {
        public static World World;
        public static GameStatsView GameStatsView;

        public readonly GameStateMachine GameStateMachine;

        public Game(ICoroutineRunner coroutineRunner, LoadingScreenController loadingScreenController)
        {
            GameStateMachine = new GameStateMachine(ServiceLocator.Container, loadingScreenController);
        }

        public void Update()
        {
            GameStateMachine.Update();
        }

        public void FixedUpdate()
        {
            GameStateMachine.FixedUpdate();
        }
    }
}
