using Leopotam.Ecs;

namespace Game.Components.Events
{
    public readonly struct AttackEventComponent
    {
        public readonly EcsEntity Target;
        public readonly int DamageValue;

        public AttackEventComponent(EcsEntity target, int damageValue)
        {
            Target = target;
            DamageValue = damageValue;
        }
    }
}
