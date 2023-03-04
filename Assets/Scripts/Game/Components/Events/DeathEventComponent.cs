using Leopotam.Ecs;

namespace Game.Components.Events
{
    public readonly struct DeathEventComponent
    {
        public readonly EcsEntity Target;

        public DeathEventComponent(EcsEntity target)
        {
            Target = target;
        }
    }
}
