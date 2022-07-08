using Configs.Game;
using Input;
using Leopotam.Ecs;
using Level;

namespace Game
{
    public interface IPlayer
    {
        PlayerConfig PlayerConfig { get; }

        EcsEntity HeroEntity { get; }

        void AttachHero(EcsEntity entity);

        void ApplyInputAction(World world, PlayerInputAction inputAction);
    }
}
