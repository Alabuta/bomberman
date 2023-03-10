using Game.Components;
using Game.Components.Colliders;
using Game.Components.Entities;
using Game.Components.Events;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using UnityEngine.Assertions;

namespace Game.Systems
{
    public sealed class HealthChangedEventsHandlerSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<HealthChangedEventComponent> _healthChangedEvents;

        public void Run()
        {
            if (_healthChangedEvents.IsEmpty())
                return;

            foreach (var index in _healthChangedEvents)
            {
                ref var eventComponent = ref _healthChangedEvents.Get1(index);

                var targetEntity = eventComponent.Target;
                if (!targetEntity.IsAlive()) // :TODO: refactor?
                    continue;

                Assert.IsTrue(targetEntity.Has<HealthComponent>());

                ref var healthComponent = ref targetEntity.Get<HealthComponent>();
                if (healthComponent.IsAlive()) // :TODO: refactor
                    Damage(targetEntity);
                else
                    Kill(targetEntity);
            }
        }

        private void Kill(EcsEntity entity)
        {
            if (entity.Has<EntityComponent>())
            {
                ref var entityComponent = ref entity.Get<EntityComponent>();
                entityComponent.Controller.Kill();
            }

            // :TODO: refactor
            if (entity.Has<HeroTag>())
                _world.HeroHasDied(entity);

            entity.Destroy();
        }

        private static void Damage(EcsEntity targetEntity)
        {
            if (targetEntity.Has<EntityComponent>())
            {
                ref var entityComponent = ref targetEntity.Get<EntityComponent>();
                entityComponent.Controller.TakeDamage();
            }
        }
    }
}
