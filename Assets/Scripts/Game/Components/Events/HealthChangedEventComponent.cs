using Leopotam.Ecs;
using Math.FixedPointMath;

namespace Game.Components.Events
{
    public readonly struct HealthChangedEventComponent
    {
        public readonly EcsEntity Target;
        public readonly fix ChangeValue;

        public HealthChangedEventComponent(EcsEntity target, fix changeValue)
        {
            Target = target;
            ChangeValue = changeValue;
        }
    }
}
