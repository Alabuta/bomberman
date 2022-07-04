using Game.Components;
using Game.Hero;
using Leopotam.Ecs;

namespace Game.Systems
{
    public sealed class WorldViewUpdateSystem : IEcsRunSystem
    {
        private EcsWorld _ecsWorld;

        private EcsFilter<EnemyComponent, TransformComponent> _enemiesFilter;
        private EcsFilter<HeroComponent, TransformComponent> _heroesFilter;

        public void Run()
        {
            UpdateHeroes();
            UpdateEnemies();
        }

        private void UpdateHeroes()
        {
            if (_heroesFilter.IsEmpty())
                return;

            foreach (var entityIndex in _heroesFilter)
            {
                ref var heroComponent = ref _heroesFilter.Get1(entityIndex);
                ref var movementComponent = ref _heroesFilter.Get2(entityIndex);

                heroComponent.Controller.WorldPosition = movementComponent.WorldPosition;
            }
        }

        private void UpdateEnemies()
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
