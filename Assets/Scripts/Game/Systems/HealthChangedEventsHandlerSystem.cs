using Game.Components;
using Game.Components.Colliders;
using Game.Components.Entities;
using Game.Components.Events;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
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
                Assert.IsTrue(targetEntity.Has<HealthComponent>());

                ref var healthComponent = ref targetEntity.Get<HealthComponent>();
                if (healthComponent.IsAlive()) // :TODO: refactor
                    Kill(targetEntity);
                else
                    Damage(targetEntity);
            }
        }

        private void Kill(EcsEntity entity)
        {
            if (entity.Has<EntityComponent>())
            {
                ref var entityComponent = ref entity.Get<EntityComponent>();
                entityComponent.Controller.Kill();
            }

            if (entity.Has<MovementComponent>())
            {
                ref var transformComponent = ref entity.Get<MovementComponent>();
                transformComponent.Speed = fix.zero; // :TODO: refactor
            }

            if (entity.Has<HeroTag>())
            {
                // :TODO: refactor
                /*var (playerInputProvider, _) =
                    _playerInputProviders.FirstOrDefault(pi => pi.Value.HeroEntity == entity);

                if (playerInputProvider != null)
                    _playersInputHandlerSystem.UnsubscribePlayerInputProvider(playerInputProvider);*/

                _world.UnsubscribePlayerInputProvider(entity);
            }

            if (entity.Has<HasColliderTag>())
            {
                entity.Del<CircleColliderComponent>();
                entity.Del<BoxColliderComponent>();
                entity.Del<HasColliderTag>();
            }

            // DeathEvent?.Invoke(this); // :TODO:

            entity.Replace(new DeadTag());
        }

        private static void Damage(EcsEntity targetEntity)
        {
            if (targetEntity.Has<EntityComponent>())
            {
                ref var entityComponent = ref targetEntity.Get<EntityComponent>();
                entityComponent.Controller.TakeDamage();
            }

            // DamageEvent?.Invoke(this, damage); // :TODO:
        }
    }
}
