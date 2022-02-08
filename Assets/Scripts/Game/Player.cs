using Configs;
using Data;
using Entity.Hero;
using Infrastructure.Services.PersistentProgress;
using Input;

namespace Game
{
    public class Player : IPlayer, ISavedProgressWriter
    {
        public PlayerConfig Config { get; }
        public IHero Hero { get; private set; }

        public Score Score;

        public Player(PlayerConfig config)
        {
            Config = config;
        }

        public void AttachHero(IHero hero, IPlayerInput playerInput)
        {
            Hero = hero;

            var heroController = (HeroController) hero;
            heroController.AttachPlayerInput(playerInput);
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
