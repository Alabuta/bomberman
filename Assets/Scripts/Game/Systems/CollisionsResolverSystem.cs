using System.Linq;
using Game.Colliders;
using Game.Components;
using Game.Components.Colliders;
using Game.Components.Entities;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;

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
            var levelModel = _world.LevelModel;

            var ecsEntityA = _filter.GetEntity(index);

            ref var entityComponentA = ref ecsEntityA.Get<EntityComponent>();
            var entityLayerMaskA = entityComponentA.LayerMask;

            ref var transformComponentA = ref _filter.Get1(index);
            ref var colliderComponentA = ref ecsEntityA.Get<CircleColliderComponent>();

            var entityPositionA = transformComponentA.WorldPosition;
            var entityCoordinateA = levelModel.ToTileCoordinate(entityPositionA);

            var neighborTiles = levelModel
                .GetNeighborTiles(entityCoordinateA)
                .Where(t => t.Has<HasColliderTag>());

            foreach (var neighborTile in neighborTiles)
            {
                /*if (!neighborTile.Has<TransformComponent>()) :TODO: refactor
                    continue;*/
                if (!neighborTile.Has<LevelTileComponent>())
                    continue;

                if ((entityLayerMaskA & neighborTile.GetCollidersInteractionMask()) == 0)
                    continue;

                var intersectionPoint = fix2.zero;
                var isIntersected = false;

                if (neighborTile.Has<CircleColliderComponent>())
                    isIntersected = CircleCircleIntersectionPoint(ecsEntityA, ref transformComponentA, neighborTile,
                        out intersectionPoint);

                else if (neighborTile.Has<BoxColliderComponent>())
                    isIntersected = CircleBoxIntersectionPoint(ecsEntityA, ref transformComponentA, neighborTile,
                        out intersectionPoint);

                if (!isIntersected)
                    continue;

                var travelledPath = transformComponentA.Speed / (fix) _world.TickRate;
                var prevPosition = entityPositionA - (fix2) transformComponentA.Direction * travelledPath;

                ref var transformComponentB = ref neighborTile.Get<TransformComponent>();
                var prevDistance = fix2.distance(prevPosition, transformComponentB.WorldPosition);
                var r = new fix(0.49);
                var R = colliderComponentA.Radius;
                var minDistance = r + R; // :TODO: use actual radius playerHero.ColliderRadius
                if (minDistance < prevDistance)
                {
                    var vector = fix2.normalize_safe(entityPositionA - intersectionPoint, fix2.zero);
                    transformComponentA.WorldPosition = intersectionPoint + vector * R; // :TODO: use actual radius
                }
            }
        }

        public static bool
            CircleCircleIntersectionPoint(EcsEntity ecsEntity, ref TransformComponent transformComponent,
                EcsEntity neighborTile,
                out fix2 intersection)
        {
            var positionA = transformComponent.WorldPosition;
            ref var colliderA = ref ecsEntity.Get<CircleColliderComponent>();

            ref var entityB = ref neighborTile.Get<LevelTileComponent>();
            var positionB = entityB.WorldPosition; // :TODO: get position from TransformComponent
            ref var colliderB = ref neighborTile.Get<CircleColliderComponent>();

            return fix.circle_and_circle_intersection_point(
                positionA, colliderA.Radius,
                positionB, colliderB.Radius,
                out intersection);
        }

        public static bool
            CircleBoxIntersectionPoint(EcsEntity ecsEntity, ref TransformComponent transformComponent, EcsEntity neighborTile,
                out fix2 intersection)
        {
            var positionA = transformComponent.WorldPosition;
            ref var colliderA = ref ecsEntity.Get<CircleColliderComponent>();

            ref var entityB = ref neighborTile.Get<LevelTileComponent>();
            var positionB = entityB.WorldPosition; // :TODO: get position from TransformComponent
            ref var colliderB = ref neighborTile.Get<BoxColliderComponent>();

            return fix.circle_and_box_intersection_point(
                positionA, colliderA.Radius,
                positionB, colliderB.InnerRadius,
                out intersection);
        }

        private void ResolveBoxColliderCollisions(int index)
        {
            ref var transformComponent = ref _filter.Get1(index);
        }
    }
}
