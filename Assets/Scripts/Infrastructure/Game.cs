using App;
using Infrastructure.Services;
using Infrastructure.States;
using Level;
using UI;

namespace Infrastructure
{
    public class Game
    {
        public static GameLevelManager LevelManager;

        public static GameStatsView GameStatsView;

        public readonly GameStateMachine GameStateMachine;

        public Game(ICoroutineRunner coroutineRunner)
        {
            GameStateMachine = new GameStateMachine(new SceneLoader(coroutineRunner), ServiceLocator.Container);
        }
    }
}
