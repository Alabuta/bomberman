using System.Linq;
using Game.Colliders;
using Game.Components;
using Game.Components.Colliders;
using Game.Components.Entities;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;

namespace Game.Systems
{
    public class CollisionsResolverSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private EcsFilter<TransformComponent, LayerMaskComponent, CircleColliderComponent>.Exclude<NonPlayerPositionControlTag>
            _circleColliders;
        private EcsFilter<TransformComponent, LayerMaskComponent, BoxColliderComponent>.Exclude<NonPlayerPositionControlTag>
            _boxColliders;

        public void Run()
        {
            if (!_circleColliders.IsEmpty())
                foreach (var index in _circleColliders)
                    ResolveCircleColliderCollisions(index);

            if (!_boxColliders.IsEmpty())
                foreach (var index in _boxColliders)
                    ResolveBoxColliderCollisions(index);
        }

        private void ResolveCircleColliderCollisions(int index)
        {
            var levelModel = _world.LevelModel;

            ref var transformComponentA = ref _circleColliders.Get1(index);
            var entityLayerMaskA = _circleColliders.Get2(index).Value;
            ref var colliderComponentA = ref _circleColliders.Get3(index);

            var entityPositionA = transformComponentA.WorldPosition;
            var entityCoordinateA = levelModel.ToTileCoordinate(entityPositionA);

            var neighborTiles = levelModel
                .GetNeighborTiles(entityCoordinateA)
                .Where(t => t.Has<LevelTileComponent>());

            foreach (var neighborTile in neighborTiles)
            {
                if ((entityLayerMaskA & neighborTile.GetCollidersInteractionMask()) == 0)
                    continue;

                var intersectionPoint = fix2.zero;
                var isIntersected = false;

                if (neighborTile.Has<CircleColliderComponent>())
                    isIntersected = CircleCircleIntersectionPoint(ref transformComponentA, ref colliderComponentA, neighborTile,
                        out intersectionPoint);

                else if (neighborTile.Has<BoxColliderComponent>())
                    isIntersected = CircleBoxIntersectionPoint(ref transformComponentA, ref colliderComponentA, neighborTile,
                        out intersectionPoint);

                if (!isIntersected)
                    continue;

                var travelledPath = transformComponentA.Speed / (fix) _world.TickRate;
                var prevPosition = entityPositionA - (fix2) transformComponentA.Direction * travelledPath;

                ref var transformComponentB = ref neighborTile.Get<TransformComponent>();
                var prevDistance = fix2.distance(prevPosition, transformComponentB.WorldPosition);

                var R = colliderComponentA.Radius;
                var minDistance = R;

                if (neighborTile.Has<CircleColliderComponent>())
                {
                    ref var colliderB = ref neighborTile.Get<CircleColliderComponent>();
                    minDistance += colliderB.Radius;
                }
                else if (neighborTile.Has<BoxColliderComponent>())
                {
                    ref var colliderB = ref neighborTile.Get<BoxColliderComponent>();
                    minDistance += colliderB.InnerRadius;
                }

                if (minDistance < prevDistance)
                {
                    var vector = fix2.normalize_safe(entityPositionA - intersectionPoint, fix2.zero);
                    transformComponentA.WorldPosition = intersectionPoint + vector * R; // :TODO: use actual radius
                }
            }
        }

        private void ResolveBoxColliderCollisions(int index)
        {
            throw new System.NotImplementedException(); // :TODO: implement
        }

        private static bool CircleCircleIntersectionPoint(ref TransformComponent transformA,
            ref CircleColliderComponent colliderA, EcsEntity entityB, out fix2 intersection)
        {
            var positionA = transformA.WorldPosition;

            ref var transformB = ref entityB.Get<TransformComponent>();
            var positionB = transformB.WorldPosition;

            ref var colliderB = ref entityB.Get<CircleColliderComponent>();

            return fix.circle_and_circle_intersection_point(
                positionA, colliderA.Radius,
                positionB, colliderB.Radius,
                out intersection);
        }

        private static bool BoxBoxIntersectionPoint(ref TransformComponent transformA, ref BoxColliderComponent colliderA,
            EcsEntity entityB, out fix2 intersection)
        {
            throw new System.NotImplementedException(); // :TODO: implement

            /*return fix.box_and_box_intersection_point(centerA, colliderA.InnerRadius, centerB, boxCollider.InnerRadius,
                out intersection);*/
        }

        private static bool CircleBoxIntersectionPoint(ref TransformComponent transformA, ref CircleColliderComponent colliderA,
            EcsEntity entityB, out fix2 intersection)
        {
            var positionA = transformA.WorldPosition;

            ref var transformB = ref entityB.Get<TransformComponent>();
            var positionB = transformB.WorldPosition;

            ref var colliderB = ref entityB.Get<BoxColliderComponent>();

            return fix.circle_and_box_intersection_point(
                positionA, colliderA.Radius,
                positionB, colliderB.InnerRadius,
                out intersection);
        }
    }
}
