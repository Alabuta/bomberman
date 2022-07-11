using System.Linq;
using Game.Components;
using Game.Components.Behaviours;
using Game.Components.Entities;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Game.Systems.Behaviours
{
    public sealed class MovementBehaviourSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<LayerMaskComponent, TransformComponent, MovementComponent, SimpleMovementBehaviourComponent>.
            Exclude<DeadTag>
            _steeredEntities;
        private readonly EcsFilter<PositionControlledByResolverTag, TransformComponent, MovementComponent>
            _playerControlledEntities;

        public void Run()
        {
            if (!_steeredEntities.IsEmpty())
                foreach (var index in _steeredEntities)
                    UpdateSteeredEntities(index);

            if (!_playerControlledEntities.IsEmpty())
                foreach (var index in _playerControlledEntities)
                    UpdatePlayerControlledEntities(index);
        }

        private void UpdateSteeredEntities(int entityIndex)
        {
            var entityLayerMask = _steeredEntities.Get1(entityIndex).Value;
            ref var transformComponent = ref _steeredEntities.Get2(entityIndex);
            ref var movementComponent = ref _steeredEntities.Get3(entityIndex);
            ref var movementBehaviourComponent = ref _steeredEntities.Get4(entityIndex);

            var levelTiles = _world.LevelTiles;
            var deltaTime = _world.FixedDeltaTime;

            var worldPosition = transformComponent.WorldPosition;
            var direction = transformComponent.Direction;

            var path = movementComponent.Speed * deltaTime;
            var currentWorldPosition = worldPosition + (fix2) direction * path;

            if (!IsNeedToUpdate(ref currentWorldPosition, ref movementBehaviourComponent))
            {
                transformComponent.WorldPosition = currentWorldPosition;
                return;
            }

            var toWorldPosition = movementBehaviourComponent.ToWorldPosition;
            var currentTileCoordinate = levelTiles.ToTileCoordinate(toWorldPosition);
            int2 targetTileCoordinate;

            var randomValue = _world.RandomGenerator.Range(fix.zero, fix.one, (int) _world.Tick);
            if (randomValue < movementBehaviourComponent.DirectionChangeChance)
            {
                var neighborTiles = movementBehaviourComponent.MovementDirections
                    .Select(d => currentTileCoordinate + d)
                    .Where(levelTiles.IsCoordinateInField)
                    .Select(c => levelTiles[c])
                    .Where(t => IsTileCanBeAsMovementTarget(t, entityLayerMask))
                    .ToArray();

                if (neighborTiles.Length == 0)
                {
                    transformComponent.Direction = int2.zero;
                    movementComponent.Speed = fix.zero;

                    return;
                }

                var index = _world.RandomGenerator.Range(0, neighborTiles.Length, (int) _world.Tick);
                var neighborTile = neighborTiles[index];
                targetTileCoordinate = levelTiles.ToTileCoordinate(neighborTile.Get<TransformComponent>().WorldPosition);

                transformComponent.Direction = targetTileCoordinate - currentTileCoordinate;
            }
            else
            {
                if (math.all(direction == int2.zero))
                    transformComponent.Direction = math.int2(1, 0);

                targetTileCoordinate = currentTileCoordinate + direction;

                if (!levelTiles.IsCoordinateInField(targetTileCoordinate))
                {
                    var neighborTile = GetRandomNeighborTile(
                        _world,
                        currentTileCoordinate,
                        ref transformComponent,
                        ref movementBehaviourComponent,
                        entityLayerMask);

                    targetTileCoordinate = neighborTile != EcsEntity.Null
                        ? levelTiles.ToTileCoordinate(neighborTile.Get<TransformComponent>().WorldPosition)
                        : currentTileCoordinate;
                }
            }

            transformComponent.Direction = (int2) math.normalize(direction);

            var targetTile = levelTiles[targetTileCoordinate];
            if (!IsTileCanBeAsMovementTarget(targetTile, entityLayerMask))
            {
                targetTile = GetRandomNeighborTile(_world,
                    currentTileCoordinate,
                    ref transformComponent,
                    ref movementBehaviourComponent,
                    entityLayerMask);
            }

            if (targetTile == EcsEntity.Null)
            {
                transformComponent.Direction = int2.zero;
                movementComponent.Speed = fix.zero;

                return;
            }

            targetTileCoordinate = levelTiles.ToTileCoordinate(targetTile.Get<TransformComponent>().WorldPosition);

            transformComponent.Direction = (int2) math.normalize(targetTileCoordinate - currentTileCoordinate);
            movementComponent.Speed = fix.one;

            transformComponent.WorldPosition =
                movementBehaviourComponent.ToWorldPosition + (fix2) direction * fix2.distance(worldPosition,
                    movementBehaviourComponent.ToWorldPosition);

            movementBehaviourComponent.FromWorldPosition = movementBehaviourComponent.ToWorldPosition;
            movementBehaviourComponent.ToWorldPosition = levelTiles.ToWorldPosition(targetTileCoordinate);
        }

        private void UpdatePlayerControlledEntities(int entityIndex)
        {
            var deltaTime = _world.FixedDeltaTime;

            ref var transformComponent = ref _playerControlledEntities.Get2(entityIndex);
            ref var movementComponent = ref _playerControlledEntities.Get3(entityIndex);

            transformComponent.WorldPosition += (fix2) transformComponent.Direction * movementComponent.Speed * deltaTime;
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

        private static bool IsTileCanBeAsMovementTarget(EcsEntity tile, int entityLayerMask)
        {
            var tileInteractionMask = tile.GetCollidersInteractionMask();

            ref var tileComponent = ref tile.Get<LevelTileComponent>();
            var hasEntities = tileComponent.EntitiesHolder?
                .Select(e => e.GetCollidersInteractionMask())
                .Any(m => (m & entityLayerMask) != 0);

            return (entityLayerMask & tileInteractionMask) == 0 && !(hasEntities ?? false);
        }

        private EcsEntity GetRandomNeighborTile(World world, int2 tileCoordinate, ref TransformComponent component,
            ref SimpleMovementBehaviourComponent simpleMovementBehaviourComponent, int entityLayerMask)
        {
            var levelTiles = world.LevelTiles;

            var tileCoordinates = simpleMovementBehaviourComponent.MovementDirections
                .Select(d => tileCoordinate + d)
                .Where(levelTiles.IsCoordinateInField)
                .Select(c => levelTiles[c])
                .Where(t => IsTileCanBeAsMovementTarget(t, entityLayerMask))
                .ToArray();

            switch (tileCoordinates.Length)
            {
                case 0:
                    return EcsEntity.Null;

                case 1:
                    return tileCoordinates[0];
            }

            if (simpleMovementBehaviourComponent.TryToSelectNewTile)
            {
                var entityDirection = component.Direction;
                tileCoordinates = tileCoordinates
                    .Where(c =>
                    {
                        var coordinate = levelTiles.ToTileCoordinate(c.Get<TransformComponent>().WorldPosition);
                        return math.all(coordinate != tileCoordinate - entityDirection);
                    })
                    .ToArray();
            }

            var index = world.RandomGenerator.Range(0, tileCoordinates.Length, (int) world.Tick);
            return tileCoordinates[index];
        }
    }
}
