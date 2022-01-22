using Services.Input;

namespace Infrastructure
{
    public class Game
    {
        public static IInputService InputService;

        public Game()
        {
            // Register InputService
            InputService = new InputService();
        }
    }
}
