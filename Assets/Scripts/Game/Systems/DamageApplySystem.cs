using System;
using System.Collections.Generic;
using Game.Components;
using Game.Components.Entities;
using Game.Components.Events;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using UnityEngine.Assertions;

namespace Game.Systems
{
    public sealed class DamageApplySystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<DamageApplyEventComponent, DamageableOnCollisionEnterComponent, HealthComponent>.Exclude<
                BombTag>
            _attackEvents;

        public void Run()
        {
            if (_attackEvents.IsEmpty())
                return;

            /*
             * Например, вместо того, чтобы менять HealthComponent в каждой системе, где есть урон, лучше создать одну DamageSystem,
             * цель которой - наносить урон сущностям с HealthComponent.
             */

            foreach (var index in _attackEvents)
            {
                ref var eventComponent = ref _attackEvents.Get1(index);
                Assert.AreNotEqual(eventComponent.DamageValue, fix.zero);

                var targetEntity = _attackEvents.GetEntity(index);
                if (!targetEntity.IsAlive()) // :TODO: refactor?
                    continue;

                ref var healthComponent = ref _attackEvents.Get3(index);
                Assert.IsTrue(healthComponent.IsAlive());

                if (healthComponent.CurrentHealth - eventComponent.DamageValue <= fix.zero) // :TODO: refactor
                {
                    Kill(targetEntity);
                    continue;
                }

                var prevHealthValue = healthComponent.CurrentHealth;
                healthComponent.CurrentHealth = fix.max(fix.zero, prevHealthValue - eventComponent.DamageValue);

                var eventEntity = _ecsWorld.NewEntity();
                eventEntity.Replace(new HealthChangedEventComponent(
                    targetEntity,
                    prevHealthValue - healthComponent.CurrentHealth
                ));

                Damage(targetEntity);
            }
        }

        private static void Damage(EcsEntity targetEntity)
        {
            if (targetEntity.Has<EntityComponent>())
            {
                ref var entityComponent = ref targetEntity.Get<EntityComponent>();
                entityComponent.Controller.TakeDamage();
            }
        }

        private void Kill(EcsEntity entity)
        {
            if (entity.Has<EntityComponent>())
            {
                ref var entityComponent = ref entity.Get<EntityComponent>();
                entityComponent.Controller.Kill();
            }

            // :TODO: refactor, listen DeadTag instead in world simulation
            if (entity.Has<HeroTag>())
                _world.HeroHasDied(entity);

            entity.Replace(new DeadTag());

            entity.Destroy(); // :TODO: destroy entity at the end of tick
        }
    }
}
