using System;
using Game.Components;
using Game.Components.Entities;
using Game.Components.Events;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game.Systems
{
    public sealed class AttackEventsHandlerSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<AttackEventComponent> _attackEvents;

        public void Run()
        {
            if (_attackEvents.IsEmpty())
                return;

            foreach (var index in _attackEvents)
            {
                ref var eventComponent = ref _attackEvents.Get1(index);

                var targetEntity = eventComponent.Target;
                Assert.IsTrue(targetEntity.IsAlive());
                if (!targetEntity.Has<HealthComponent>())
                    continue;

                if (eventComponent.DamageValue == fix.zero)
                    continue;

                ref var healthComponent = ref targetEntity.Get<HealthComponent>();
                healthComponent.CurrentHealth = fix.max(fix.zero, healthComponent.CurrentHealth - eventComponent.DamageValue);

                var eventEntity = _ecsWorld.NewEntity();
                eventEntity.Replace(new HealthChangedEventComponent(
                    targetEntity,
                    eventComponent.DamageValue
                ));
            }
        }
    }
}
