using System;
using Game.Components;
using Game.Components.Behaviours;
using Leopotam.Ecs;
using Unity.Mathematics;

namespace Game.Systems
{
    public sealed class HealthSystem : IEcsRunSystem
    {
        public Action<EcsEntity> HealthChangedEvent;

        private EcsWorld _ecsWorld;

        private EcsFilter<DamageComponent> _filter;

        public void Run()
        {
            if (_filter.IsEmpty())
                return;

            foreach (var index in _filter)
            {
                ref var damageComponent = ref _filter.Get1(index);
                ref var entity = ref damageComponent.Entity;

                if (!entity.Has<HealthComponent>())
                    continue;

                ref var healthComponent = ref entity.Get<HealthComponent>();
                var health = healthComponent.CurrentHealth;

                ApplyDamage(ref healthComponent, damageComponent.DamageValue);

                if (health != healthComponent.CurrentHealth)
                    HealthChangedEvent?.Invoke(entity);
            }
        }

        private static void ApplyDamage(ref HealthComponent healthComponent, int damage)
        {
            healthComponent.CurrentHealth = math.max(0, healthComponent.CurrentHealth - damage);
        }
    }
}
