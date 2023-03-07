using Leopotam.Ecs;
using Math.FixedPointMath;

namespace Game.Components.Events
{
    public readonly struct AttackEventComponent
    {
        public readonly EcsEntity Target;
        public readonly fix DamageValue;

        public AttackEventComponent(EcsEntity target, fix damageValue)
        {
            Target = target;
            DamageValue = damageValue;
        }
    }
}
