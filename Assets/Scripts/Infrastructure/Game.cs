using App;
using Infrastructure.States;
using Level;
using Services.Input;

namespace Infrastructure
{
    public class Game
    {
        public static IInputService InputService;
        public static ILevelManager LevelManager;

        public readonly GameStateMachine GameStateMachine;

        public Game(ICoroutineRunner coroutineRunner)
        {
            GameStateMachine = new GameStateMachine(new SceneLoader(coroutineRunner));
        }
    }
}
