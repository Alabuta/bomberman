using App;
using Infrastructure.Services;
using Infrastructure.States;
using Level;

namespace Infrastructure
{
    public class Game
    {
        public static ILevelManager LevelManager;

        public readonly GameStateMachine GameStateMachine;

        public Game(ICoroutineRunner coroutineRunner)
        {
            GameStateMachine = new GameStateMachine(new SceneLoader(coroutineRunner), ServiceLocator.Container);
        }
    }
}
