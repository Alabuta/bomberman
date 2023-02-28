using Configs.Game;
using Leopotam.Ecs;

namespace Game
{
    public interface IPlayer
    {
        PlayerConfig PlayerConfig { get; }

        PlayerTagConfig PlayerTag { get; }

        EcsEntity HeroEntity { get; }

        void AttachHeroEntity(EcsEntity entity);
    }
}
