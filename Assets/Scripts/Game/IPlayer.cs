using Configs;
using Input;
using Leopotam.Ecs;
using Level;

namespace Game
{
    public interface IPlayer
    {
        PlayerConfig PlayerConfig { get; }

        // Hero.Hero Hero { get; }
        EcsEntity HeroEntity { get; }

        // void AttachHero(Hero.Hero hero);

        void ApplyInputAction(World world, PlayerInputAction inputAction);

        void AttachHero(EcsEntity entity);
    }
}
