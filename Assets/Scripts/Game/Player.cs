using Configs.Game;
using Data;
using Entity;
using Input;
using Services.PersistentProgress;

namespace Game
{
    public class Player : IPlayer, ISavedProgressWriter
    {
        public PlayerTagConfig PlayerTagConfig { get; }
        public HeroController HeroController { get; }
        public IPlayerInputForwarder PlayerInputForwarder { get; }

        public Score Score;

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
