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

        // :TODO: use register of collisions and CollisionEnterEventComponent event
        private readonly EcsFilter<TransformComponent, DamageOnCollisionEnterComponent>.Exclude<DeadTag> _attackers;

        private readonly EcsFilter<TransformComponent, DamageableOnCollisionEnterComponent, LayerMaskComponent>
            .Exclude<DeadTag> _targets;

        public void Run()
        {
            using var _ = Profiling.AttackBehavioursUpdate.Auto();

            if (_attackers.IsEmpty() || _targets.IsEmpty())
                return;

            foreach (var index in _attackers)
                Update(index);
        }

        private void Update(int attackerIndex)
        {
            ref var transformComponentA = ref _attackers.Get1(attackerIndex);
            ref var damageOnCollisionEnter = ref _attackers.Get2(attackerIndex);
            // ref var collisionEnterEvent = ref _attackers.Get3(attackerIndex);

            foreach (var targetIndex in _targets)
            {
                ref var targetEntity = ref _targets.GetEntity(targetIndex);
                /*if (!collisionEnterEvent.Entities.Contains(targetEntity))
                    continue;*/

                ref var layerMaskComponent = ref _targets.Get3(targetIndex);
                if ((layerMaskComponent.Value & damageOnCollisionEnter.InteractionLayerMask.value) == 0)
                    continue;

                ref var transformComponentB = ref _targets.Get1(targetIndex);
                var damageableOnCollision = _targets.Get2(targetIndex);

                var hitRadius = damageOnCollisionEnter.HitRadius;
                var hurtRadius = damageableOnCollision.HurtRadius;

                var areEntitiesOverlapped = AreEntitiesOverlapped(
                    transformComponentA.WorldPosition,
                    transformComponentB.WorldPosition,
                    hitRadius,
                    hurtRadius);

                if (!areEntitiesOverlapped)
                    continue;

                targetEntity.Replace(new DamageApplyEventComponent(damageOnCollisionEnter.DamageValue));
            }
        }

        private static bool AreEntitiesOverlapped(fix2 positionA, fix2 positionB, fix hitRadius, fix hurtRadius) =>
            fix2.distance(positionA, positionB) <= hitRadius + hurtRadius;
    }
}
