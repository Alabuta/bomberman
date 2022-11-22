using App;
using Game.Components;
using Game.Components.Behaviours;
using Game.Components.Entities;
using Game.Components.Events;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;

namespace Game.Systems.Behaviours
{
    public sealed class AttackBehaviourSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent, SimpleAttackBehaviourComponent> _attackersFilter;
        private readonly EcsFilter<TransformComponent, LayerMaskComponent, DamageableComponent> _targetsFilter;

        public void Run()
        {
            using var _ = Profiling.AttackBehavioursUpdate.Auto();

            if (_attackersFilter.IsEmpty() || _targetsFilter.IsEmpty())
                return;

            foreach (var index in _attackersFilter)
                Update(index);
        }

        private void Update(int attackerIndex)
        {
            ref var attackerTransform = ref _attackersFilter.Get1(attackerIndex);
            ref var attackerBehaviour = ref _attackersFilter.Get2(attackerIndex);

            foreach (var targetEntityIndex in _targetsFilter)
            {
                ref var targetEntity = ref _targetsFilter.GetEntity(targetEntityIndex);

                ref var layerMaskComponent = ref _targetsFilter.Get2(targetEntityIndex);
                if ((layerMaskComponent.Value & attackerBehaviour.InteractionLayerMask.value) == 0)
                    continue;

                ref var targetTransform = ref _targetsFilter.Get1(targetEntityIndex);
                ref var damageableComponent = ref _targetsFilter.Get3(targetEntityIndex);

                var areEntitiesOverlapped = AreEntitiesOverlapped(ref attackerTransform, attackerBehaviour.HitRadius,
                    ref targetTransform, damageableComponent.HurtRadius);

                if (!areEntitiesOverlapped)
                    continue;

                targetEntity.Replace(new AttackEventComponent
                {
                    DamageValue = attackerBehaviour.DamageValue
                });
            }
        }

        private static bool AreEntitiesOverlapped(ref TransformComponent transformComponentA, fix hitRadius,
            ref TransformComponent transformComponentB, fix hurtRadius)
        {
            return fix2.distance(transformComponentA.WorldPosition, transformComponentB.WorldPosition) < hitRadius + hurtRadius;
        }
    }
}
