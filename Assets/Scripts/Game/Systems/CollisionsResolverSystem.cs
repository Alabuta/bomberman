using Game.Colliders;
using Game.Components;
using Game.Components.Colliders;
using Game.Components.Entities;
using Leopotam.Ecs;
using Level;

namespace Game.Systems
{
    public class CollisionsResolverSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private EcsFilter<TransformComponent, HasColliderTag, HeroTag> _filter;

        public void Run()
        {
            if (_filter.IsEmpty())
                return;

            foreach (var index in _filter)
            {
                var ecsEntity = _filter.GetEntity(index);

                if (ecsEntity.Has<CircleColliderComponent>())
                    ResolveCircleColliderCollisions(index);

                else if (ecsEntity.Has<BoxColliderComponent>())
                    ResolveBoxColliderCollisions(index);
            }
        }

        private void ResolveCircleColliderCollisions(int index)
        {
            var LevelModel = _world.LevelModel;

            ref var transformComponent = ref _filter.Get1(index);

            var heroPosition = transformComponent.WorldPosition;
            var heroTileCoordinate = LevelModel.ToTileCoordinate(heroPosition);

            /*var neighborTiles = LevelModel
                .GetNeighborTiles(heroTileCoordinate)
                .Where(t => t.TileLoad?.Components?.Any(c => c is ColliderComponent2) ?? false);*/
        }

        private void ResolveBoxColliderCollisions(int index)
        {
            ref var transformComponent = ref _filter.Get1(index);
        }
    }
}
