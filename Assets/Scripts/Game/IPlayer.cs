using Configs.Game;
using Input;
using Leopotam.Ecs;

namespace Game
{
    public interface IPlayer
    {
        PlayerConfig PlayerConfig { get; }

        EcsEntity HeroEntity { get; }

        void AttachHero(EcsEntity entity);
    }
}
