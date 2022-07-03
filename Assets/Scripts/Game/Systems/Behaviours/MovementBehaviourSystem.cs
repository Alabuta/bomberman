using System.Linq;
using Game.Colliders;
using JetBrains.Annotations;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Game.Systems.Behaviours
{
    public sealed class MovementBehaviourSystem : IEcsRunSystem
    {
        private EcsWorld _ecsWorld;
        private EcsFilter<EnemyComponent, TransformComponent, SimpleMovementBehaviourComponent> _filter;

        public void Run()
        {
            if (_filter.IsEmpty())
                return;

            foreach (var index in _filter)
                Update(index);
        }

        private void Update(int entityIndex)
        {
            ref var enemyComponent = ref _filter.Get1(entityIndex);
            ref var movementComponent = ref _filter.Get2(entityIndex);
            ref var movementBehaviourComponent = ref _filter.Get3(entityIndex);

            var world = Infrastructure.Game.World;
            var levelModel = world.LevelModel;

            var deltaTime = world.FixedDeltaTime;

            ref var worldPosition = ref movementComponent.WorldPosition;
            ref var direction = ref movementComponent.Direction;

            var path = movementComponent.Speed * deltaTime;
            var currentWorldPosition = worldPosition + (fix2) direction * path;

            if (!IsNeedToUpdate(ref currentWorldPosition, ref movementBehaviourComponent))
            {
                movementComponent.WorldPosition = currentWorldPosition;
                return;
            }

            var toWorldPosition = movementBehaviourComponent.ToWorldPosition;
            var currentTileCoordinate = levelModel.ToTileCoordinate(toWorldPosition);
            int2 targetTileCoordinate;

            var interactionLayerMask = enemyComponent.InteractionLayerMask;

            var randomValue = world.RandomGenerator.Range(fix.zero, fix.one, (int) world.Tick);
            if (randomValue < movementBehaviourComponent.DirectionChangeChance)
            {
                var neighborTiles2 = movementBehaviourComponent.MovementDirections
                    .Select(d => currentTileCoordinate + d)
                    .Where(levelModel.IsCoordinateInField).ToArray();

                var neighborTiles = neighborTiles2
                    .Select(c => levelModel[c])
                    .Where(t => IsTileCanBeAsMovementTarget(t, interactionLayerMask))
                    .ToArray();

                if (neighborTiles.Length == 0)
                {
                    movementComponent.Direction = int2.zero;
                    movementComponent.Speed = fix.zero;

                    return;
                }

                var index = world.RandomGenerator.Range(0, neighborTiles.Length, (int) world.Tick);
                targetTileCoordinate = neighborTiles[index].Coordinate;

                movementComponent.Direction = targetTileCoordinate - currentTileCoordinate;
            }
            else
            {
                if (math.all(direction == int2.zero))
                    movementComponent.Direction = math.int2(1, 0);

                targetTileCoordinate = currentTileCoordinate + direction;

                if (!levelModel.IsCoordinateInField(targetTileCoordinate))
                    targetTileCoordinate = GetRandomNeighborTile(
                        world,
                        currentTileCoordinate,
                        ref movementComponent,
                        ref movementBehaviourComponent,
                        interactionLayerMask)?.Coordinate ?? currentTileCoordinate;
            }

            movementComponent.Direction = (int2) math.normalize(direction);

            var targetTile = levelModel[targetTileCoordinate];
            targetTile = IsTileCanBeAsMovementTarget(targetTile, interactionLayerMask)
                ? targetTile
                : GetRandomNeighborTile(world,
                    currentTileCoordinate,
                    ref movementComponent,
                    ref movementBehaviourComponent,
                    interactionLayerMask);

            if (targetTile == null)
            {
                movementComponent.Direction = int2.zero;
                movementComponent.Speed = fix.zero;

                return;
            }

            targetTileCoordinate = targetTile.Coordinate;

            movementComponent.Direction = (int2) math.normalize(targetTileCoordinate - currentTileCoordinate);
            movementComponent.Speed = fix.one;

            movementComponent.WorldPosition =
                movementBehaviourComponent.ToWorldPosition + (fix2) direction * fix2.distance(worldPosition,
                    movementBehaviourComponent.ToWorldPosition);

            movementBehaviourComponent.FromWorldPosition = movementBehaviourComponent.ToWorldPosition;
            movementBehaviourComponent.ToWorldPosition = levelModel.ToWorldPosition(targetTileCoordinate);
        }

        private static bool IsNeedToUpdate(ref fix2 currentWorldPosition,
            ref SimpleMovementBehaviourComponent simpleMovementBehaviourComponent)
        {
            var directionA = simpleMovementBehaviourComponent.ToWorldPosition -
                             simpleMovementBehaviourComponent.FromWorldPosition;
            var directionC = currentWorldPosition - simpleMovementBehaviourComponent.ToWorldPosition;

            var lengthSqA = fix2.lengthsq(directionA);
            var lengthSqC = fix2.lengthsq(directionC);

            var isEntityMoved = lengthSqA > fix.zero;
            if (!isEntityMoved)
                return true;

            return lengthSqA <= fix2.distancesq(currentWorldPosition, simpleMovementBehaviourComponent.FromWorldPosition) +
                lengthSqC;
        }

        private static bool IsTileCanBeAsMovementTarget(ILevelTileView tile, int interactionLayerMask)
        {
            var tileLoad = tile.TileLoad;
            if (tileLoad == null || (interactionLayerMask & tileLoad.LayerMask) == 0)
                return true;

            return !(tileLoad.Components?.OfType<ColliderComponent>().Any() ?? false);
        }

        [CanBeNull]
        private ILevelTileView GetRandomNeighborTile(World world, int2 tileCoordinate, ref TransformComponent component,
            ref SimpleMovementBehaviourComponent simpleMovementBehaviourComponent, int interactionLayerMask)
        {
            var levelModel = world.LevelModel;

            var tileCoordinates = simpleMovementBehaviourComponent.MovementDirections
                .Select(d => tileCoordinate + d)
                .Where(levelModel.IsCoordinateInField)
                .Select(c => levelModel[c])
                .Where(t => IsTileCanBeAsMovementTarget(t, interactionLayerMask))
                .ToArray();

            switch (tileCoordinates.Length)
            {
                case 0:
                    return null;

                case 1:
                    return tileCoordinates[0];
            }

            if (simpleMovementBehaviourComponent.TryToSelectNewTile)
            {
                var entityDirection = component.Direction;
                tileCoordinates = tileCoordinates
                    .Where(c => math.all(c.Coordinate != tileCoordinate - entityDirection))
                    .ToArray();
            }

            var index = world.RandomGenerator.Range(0, tileCoordinates.Length, (int) world.Tick);
            return tileCoordinates[index];
        }
    }
}
