using App;
using Services.Input;

namespace Infrastructure
{
    public class Game
    {
        public static IInputService InputService;
        public readonly GameStateMachine GameStateMachine;

        public Game(ICoroutineRunner coroutineRunner)
        {
            GameStateMachine = new GameStateMachine(new SceneLoader(coroutineRunner));
        }
    }
}
