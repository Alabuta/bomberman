using Game.Components;
using Game.Components.Entities;
using Leopotam.Ecs;

namespace Game.Systems
{
    public sealed class WorldViewUpdateSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;

        private EcsFilter<EntityComponent, TransformComponent> _entitiesFilter;

        public void Run()
        {
            UpdateEntities();
        }

        private void UpdateEntities()
        {
            if (_entitiesFilter.IsEmpty())
                return;

            foreach (var entityIndex in _entitiesFilter)
            {
                ref var entityComponent = ref _entitiesFilter.Get1(entityIndex);
                ref var transformComponent = ref _entitiesFilter.Get2(entityIndex);

                entityComponent.Controller.Direction = transformComponent.Direction;
                entityComponent.Controller.Speed = transformComponent.Speed;
                entityComponent.Controller.WorldPosition = transformComponent.WorldPosition;
            }
        }
    }
}
