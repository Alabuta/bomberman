using Leopotam.Ecs;
using Math.FixedPointMath;

namespace Game.Components.Events
{
    public readonly struct DamageApplyEventComponent
    {
        public readonly EcsEntity Target;
        public readonly fix DamageValue;

        public DamageApplyEventComponent(EcsEntity target, fix damageValue)
        {
            Target = target;
            DamageValue = damageValue;
        }
    }
}
