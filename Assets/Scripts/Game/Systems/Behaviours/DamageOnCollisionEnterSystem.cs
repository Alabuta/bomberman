using App;
using Game.Components;
using Game.Components.Behaviours;
using Game.Components.Entities;
using Game.Components.Events;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;

namespace Game.Systems.Behaviours
{
    public sealed class DamageOnCollisionEnterSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        // :TODO: use register of collisions
        private readonly EcsFilter<DamageOnCollisionEnterComponent, CollisionEnterEventComponent>.Exclude<DeadTag> _attackers;
        private readonly EcsFilter<DamageableOnCollisionEnterTag, LayerMaskComponent>.Exclude<DeadTag> _targets;

        public void Run()
        {
            using var _ = Profiling.AttackBehavioursUpdate.Auto();

            if (_attackers.IsEmpty() || _targets.IsEmpty())
                return;

            foreach (var attackerIndex in _attackers)
            {
                ref var damageOnCollision = ref _attackers.Get1(attackerIndex);
                ref var collisionEnterEvent = ref _attackers.Get2(attackerIndex);

                foreach (var targetIndex in _targets)
                {
                    ref var targetEntity = ref _targets.GetEntity(targetIndex);
                    if (!collisionEnterEvent.Entities.Contains(targetEntity))
                        continue;

                    var layerMaskComponent = _targets.Get2(targetIndex);
                    if ((layerMaskComponent.Value & damageOnCollision.InteractionLayerMask.value) == 0)
                        continue;

                    targetEntity.Replace(new DamageApplyEventComponent(damageOnCollision.DamageValue));
                }
            }

            /*foreach (var index in _attackers)
            {
                ref var attackerTransform = ref _attackersFilter.Get1(index);
                ref var attackerBehaviour = ref _attackersFilter.Get2(index);

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

                    targetEntity.Replace(new DamageApplyEventComponent(attackerBehaviour.DamageValue));
                }
            }*/
        }

        private static bool AreEntitiesOverlapped(ref TransformComponent transformComponentA, fix hitRadius,
            ref TransformComponent transformComponentB, fix hurtRadius)
        {
            return fix2.distance(transformComponentA.WorldPosition, transformComponentB.WorldPosition) < hitRadius + hurtRadius;
        }
    }
}
