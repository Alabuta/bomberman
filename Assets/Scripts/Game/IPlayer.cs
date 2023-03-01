using Configs.Game;
using Leopotam.Ecs;

namespace Game
{
    public interface IPlayer
    {
        PlayerConfig PlayerConfig { get; }

        PlayerTagConfig PlayerTag { get; }

        EcsEntity HeroEntity { get; }

        bool HasRemoConBomb { get; } // :TODO: refactor

        void AttachHeroEntity(EcsEntity entity);
    }
}
