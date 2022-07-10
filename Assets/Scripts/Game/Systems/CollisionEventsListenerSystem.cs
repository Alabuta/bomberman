using Game.Components;
using Game.Components.Entities;
using Game.Components.Events;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;

namespace Game.Systems
{
    public sealed class CollisionEventsListenerSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent, OnCollisionExitEventComponent, BombTag> _filter;

        public void Run()
        {
            var levelTiles = _world.LevelTiles;

            foreach (var index in _filter)
            {
                ref var bombEntity = ref _filter.GetEntity(index);
                ref var transformComponent = ref _filter.Get1(index);

                var worldPosition = transformComponent.WorldPosition;
                var coordinate = levelTiles.ToTileCoordinate(worldPosition);

                var levelTile = levelTiles[coordinate];
                ref var levelTileComponent = ref levelTile.Get<LevelTileComponent>();

                if (levelTileComponent.Entities.Count == 0)
                    bombEntity.Del<IsKinematicTag>();
            }
        }
    }
}
