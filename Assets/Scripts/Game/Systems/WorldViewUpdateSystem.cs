using Leopotam.Ecs;

namespace Game.Systems
{
    public sealed class WorldViewUpdateSystem : IEcsRunSystem
    {
        private EcsWorld _ecsWorld;

        private EcsFilter<EnemyComponent, TransformComponent> _enemiesFilter;

        public void Run()
        {
            if (_enemiesFilter.IsEmpty())
                return;

            foreach (var entityIndex in _enemiesFilter)
            {
                ref var enemyComponent = ref _enemiesFilter.Get1(entityIndex);
                ref var movementComponent = ref _enemiesFilter.Get2(entityIndex);

                enemyComponent.Controller.WorldPosition = movementComponent.WorldPosition;
            }
        }
    }
}
