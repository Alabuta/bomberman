using Leopotam.Ecs;

namespace Game.Components.Events
{
    public readonly struct HealthChangedEventComponent
    {
        public readonly EcsEntity Target;
        public readonly int ChangeValue;

        public HealthChangedEventComponent(EcsEntity target, int changeValue)
        {
            Target = target;
            ChangeValue = changeValue;
        }
    }
}
