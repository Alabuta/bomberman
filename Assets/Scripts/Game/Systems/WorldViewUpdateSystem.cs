using Game.Components;
using Game.Components.Entities;
using Leopotam.Ecs;

namespace Game.Systems
{
    public sealed class WorldViewUpdateSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;

        private EcsFilter<EntityComponent, TransformComponent> _transformFilter;
        private EcsFilter<EntityComponent, MovementComponent> _movementFilter;

        public void Run()
        {
            UpdateEntities();
        }

        private void UpdateEntities()
        {
            foreach (var entityIndex in _transformFilter)
            {
                ref var entityComponent = ref _transformFilter.Get1(entityIndex);
                ref var transformComponent = ref _transformFilter.Get2(entityIndex);

                entityComponent.Controller.Direction = transformComponent.Direction;
                entityComponent.Controller.WorldPosition = transformComponent.WorldPosition;
            }

            foreach (var entityIndex in _movementFilter)
            {
                ref var entityComponent = ref _movementFilter.Get1(entityIndex);
                ref var movementComponent = ref _movementFilter.Get2(entityIndex);

                entityComponent.Controller.Speed = movementComponent.Speed;
            }
        }
    }
}
