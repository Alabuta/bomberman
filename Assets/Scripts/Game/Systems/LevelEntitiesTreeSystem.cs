using System.Collections.Generic;
using Game.Components;
using Game.Components.Entities;
using Leopotam.Ecs;
using Level;

namespace Game.Systems
{
    public sealed class LevelEntitiesTreeSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent>.Exclude<LevelTileComponent> _filter;

        private readonly HashSet<EcsEntity> _clearedTiles = new();

        public void Run()
        {
            if (_filter.IsEmpty())
                return;

            var levelTiles = _world.LevelTiles;

            foreach (var index in _filter)
            {
                ref var entity = ref _filter.GetEntity(index);
                ref var transformComponent = ref _filter.Get1(index);
                var tileCoordinate = levelTiles.ToTileCoordinate(transformComponent.WorldPosition);

                var levelTileEntity = levelTiles[tileCoordinate];
                ref var levelTileComponent = ref levelTileEntity.Get<LevelTileComponent>();

                if (!_clearedTiles.Contains(levelTileEntity))
                {
                    levelTileComponent.EntitiesHolder?.Clear();
                    _clearedTiles.Add(levelTileEntity);
                }

                if (levelTileComponent.EntitiesHolder != null)
                    levelTileComponent.EntitiesHolder.Add(entity);
                else
                    levelTileComponent.EntitiesHolder = new HashSet<EcsEntity> { entity };
            }

            _clearedTiles.Clear();
        }
    }
}
