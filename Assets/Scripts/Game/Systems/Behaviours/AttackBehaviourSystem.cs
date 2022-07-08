using Game.Components;
using Game.Components.Behaviours;
using Game.Components.Entities;
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
        private readonly EcsFilter<LayerMaskComponent, TransformComponent, DamageableComponent> _targetsFilter;

        public void Run()
        {
            if (_attackersFilter.IsEmpty() || _targetsFilter.IsEmpty())
                return;

            foreach (var index in _attackersFilter)
                Update(index);
        }

        private void Update(int attackerIndex)
        {
            ref var attackerTransform = ref _attackersFilter.Get1(attackerIndex);
            ref var attackComponent = ref _attackersFilter.Get2(attackerIndex);

            foreach (var targetEntityIndex in _targetsFilter)
            {
                ref var targetEntity = ref _targetsFilter.GetEntity(targetEntityIndex);

                ref var layerMaskComponent = ref _targetsFilter.Get1(targetEntityIndex);
                if ((layerMaskComponent.Value & attackComponent.InteractionLayerMask.value) == 0)
                    continue;

                ref var targetTransform = ref _targetsFilter.Get2(targetEntityIndex);
                ref var damageableComponent = ref _targetsFilter.Get3(targetEntityIndex);

                var areEntitiesOverlapped = AreEntitiesOverlapped(ref attackerTransform, attackComponent.HitRadius,
                    ref targetTransform, damageableComponent.HurtRadius);

                if (!areEntitiesOverlapped)
                    continue;

                targetEntity.Replace(new AttackComponent
                {
                    DamageValue = attackComponent.DamageValue
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
