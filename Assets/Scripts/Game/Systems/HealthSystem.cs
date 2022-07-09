using System;
using Game.Components;
using Game.Components.Events;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Unity.Mathematics;

namespace Game.Systems
{
    public sealed class HealthSystem : IEcsRunSystem
    {
        public Action<EcsEntity> HealthChangedEvent;

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<AttackEventComponent, HealthComponent>.Exclude<DeadTag> _entitiesWithHealth;

        public void Run()
        {
            if (_entitiesWithHealth.IsEmpty())
                return;

            foreach (var index in _entitiesWithHealth)
            {
                ref var targetEntity = ref _entitiesWithHealth.GetEntity(index);

                ref var attackComponent = ref _entitiesWithHealth.Get1(index);
                ref var healthComponent = ref _entitiesWithHealth.Get2(index);

                var health = healthComponent.CurrentHealth;

                ApplyDamage(ref healthComponent, attackComponent.DamageValue);

                if (health != healthComponent.CurrentHealth)
                    HealthChangedEvent?.Invoke(targetEntity);
            }
        }

        private static void ApplyDamage(ref HealthComponent healthComponent, int damage)
        {
            healthComponent.CurrentHealth = math.max(0, healthComponent.CurrentHealth - damage);
        }
    }
}
