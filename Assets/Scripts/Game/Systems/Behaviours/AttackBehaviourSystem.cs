using Game.Components;
using Game.Components.Behaviours;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;

namespace Game.Systems.Behaviours
{
    public sealed class AttackBehaviourSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private EcsFilter<TransformComponent, HealthComponent> _targetsFilter;
        private EcsFilter<TransformComponent, SimpleAttackBehaviourComponent> _attackersFilter;

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

            var levelModel = _world.LevelModel;

            /*var overlappedHeroes = world.Players.Values
                .Select(p => p.Hero)
                .Where(h => AreEntitiesOverlapped(entity, h))
                .ToArray();*/

            // ref var target = _filter.GetEntity(index);
        }

        private static bool AreEntitiesOverlapped(IEntity entityA, IEntity entityB)
        {
            return fix2.distance(entityA.WorldPosition, entityB.WorldPosition) < entityA.HitRadius + entityB.HurtRadius;
        }
    }
}
