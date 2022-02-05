using Configs;
using Data;
using Infrastructure.Services.PersistentProgress;
using Input;

namespace Game
{
    public class Player : IPlayer, ISavedProgressWriter
    {
        public PlayerConfig Config { get; }
        public IPlayerInput PlayerInput { get; }

        public Score Score;

        public Player(IPlayerInput playerInput, PlayerConfig config)
        {
            Config = config;
            PlayerInput = playerInput;
        }

        public void LoadProgress(PlayerProgress progress)
        {
            Score = progress.Score;
        }

        public void UpdateProgress(PlayerProgress progress)
        {
            progress.Score = Score;
        }
    }
}
