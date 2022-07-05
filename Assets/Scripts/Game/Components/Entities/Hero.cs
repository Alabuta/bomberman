using Configs.Entity;
using Leopotam.Ecs;

namespace Game.Components.Entities
{
    public struct HeroTag : IEcsIgnoreInFilter
    {
    }

    public struct BombBlasterComponent
    {
        public BombConfig BombConfig;

        public int BombBlastDamage;
        public int BombBlastRadius;
    }
}
