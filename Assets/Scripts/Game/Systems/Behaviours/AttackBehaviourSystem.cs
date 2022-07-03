using Game.Components;
using Leopotam.Ecs;
using Math.FixedPointMath;

namespace Game.Systems.Behaviours
{
    public sealed class AttackBehaviourSystem : IEcsRunSystem
    {
        private EcsWorld _ecsWorld;

        private EcsFilter<HealthComponent> _targetsFilter;
        private EcsFilter<SimpleAttackBehaviourComponent> _attackersFilter;

        public void Run()
        {
            if (_targetsFilter.IsEmpty())
                return;

            if (_attackersFilter.IsEmpty())
                return;

            foreach (var index in _attackersFilter)
                Update(index);
        }

        private void Update(int attackerIndex)
        {
            ref var attackBehaviourComponent = ref _attackersFilter.Get1(attackerIndex);

            var world = Infrastructure.Game.World;
            var levelModel = world.LevelModel;

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
