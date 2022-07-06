using System.Linq;
using Game.Components;
using Game.Components.Behaviours;
using Game.Components.Entities;
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

        private EcsFilter<EntityComponent, TransformComponent, SimpleMovementBehaviourComponent> _filterEnemies;
        private EcsFilter<HeroTag, TransformComponent> _filterHeroes;

        public void Run()
        {
            if (!_filterEnemies.IsEmpty())
                foreach (var index in _filterEnemies)
                    UpdateEnemy(index);

            if (!_filterHeroes.IsEmpty())
                foreach (var index in _filterHeroes)
                    UpdateHero(index);
        }

        private void UpdateEnemy(int entityIndex)
        {
            ref var entityComponent = ref _filterEnemies.Get1(entityIndex); // :TODO: get rid of EntityComponent
            ref var transformComponent = ref _filterEnemies.Get2(entityIndex);
            ref var movementBehaviourComponent = ref _filterEnemies.Get3(entityIndex);

            var levelModel = _world.LevelModel;
            var deltaTime = _world.FixedDeltaTime;

            ref var worldPosition = ref transformComponent.WorldPosition;
            ref var direction = ref transformComponent.Direction;

            var path = transformComponent.Speed * deltaTime;
            var currentWorldPosition = worldPosition + (fix2) direction * path;

            if (!IsNeedToUpdate(ref currentWorldPosition, ref movementBehaviourComponent))
            {
                transformComponent.WorldPosition = currentWorldPosition;
                return;
            }

            var toWorldPosition = movementBehaviourComponent.ToWorldPosition;
            var currentTileCoordinate = levelModel.ToTileCoordinate(toWorldPosition);
            int2 targetTileCoordinate;

            var entityLayerMask = entityComponent.LayerMask;

            var randomValue = _world.RandomGenerator.Range(fix.zero, fix.one, (int) _world.Tick);
            if (randomValue < movementBehaviourComponent.DirectionChangeChance)
            {
                var neighborTiles = movementBehaviourComponent.MovementDirections
                    .Select(d => currentTileCoordinate + d)
                    .Where(levelModel.IsCoordinateInField).ToArray()
                    .Select(c => levelModel[c])
                    .Where(t => IsTileCanBeAsMovementTarget(t, entityLayerMask))
                    .ToArray();

                if (neighborTiles.Length == 0)
                {
                    transformComponent.Direction = int2.zero;
                    transformComponent.Speed = fix.zero;

                    return;
                }

                var index = _world.RandomGenerator.Range(0, neighborTiles.Length, (int) _world.Tick);
                targetTileCoordinate = neighborTiles[index].Get<LevelTileComponent>().Coordinate;

                transformComponent.Direction = targetTileCoordinate - currentTileCoordinate;
            }
            else
            {
                if (math.all(direction == int2.zero))
                    transformComponent.Direction = math.int2(1, 0);

                targetTileCoordinate = currentTileCoordinate + direction;

                if (!levelModel.IsCoordinateInField(targetTileCoordinate))
                {
                    var neighborTile = GetRandomNeighborTile(
                        _world,
                        currentTileCoordinate,
                        ref transformComponent,
                        ref movementBehaviourComponent,
                        entityLayerMask);

                    targetTileCoordinate = neighborTile != EcsEntity.Null
                        ? neighborTile.Get<LevelTileComponent>().Coordinate
                        : currentTileCoordinate;
                }
            }

            transformComponent.Direction = (int2) math.normalize(direction);

            var targetTile = levelModel[targetTileCoordinate];
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
                transformComponent.Speed = fix.zero;

                return;
            }

            targetTileCoordinate = targetTile.Get<LevelTileComponent>().Coordinate;

            transformComponent.Direction = (int2) math.normalize(targetTileCoordinate - currentTileCoordinate);
            transformComponent.Speed = fix.one;

            transformComponent.WorldPosition =
                movementBehaviourComponent.ToWorldPosition + (fix2) direction * fix2.distance(worldPosition,
                    movementBehaviourComponent.ToWorldPosition);

            movementBehaviourComponent.FromWorldPosition = movementBehaviourComponent.ToWorldPosition;
            movementBehaviourComponent.ToWorldPosition = levelModel.ToWorldPosition(targetTileCoordinate);
        }

        private void UpdateHero(int entityIndex)
        {
            var deltaTime = _world.FixedDeltaTime;

            ref var transformComponent = ref _filterHeroes.Get2(entityIndex);

            transformComponent.WorldPosition += (fix2) transformComponent.Direction * transformComponent.Speed * deltaTime;
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

            return (entityLayerMask & tileInteractionMask) == 0;
        }

        private EcsEntity GetRandomNeighborTile(World world, int2 tileCoordinate, ref TransformComponent component,
            ref SimpleMovementBehaviourComponent simpleMovementBehaviourComponent, int entityLayerMask)
        {
            var levelModel = world.LevelModel;

            var tileCoordinates = simpleMovementBehaviourComponent.MovementDirections
                .Select(d => tileCoordinate + d)
                .Where(levelModel.IsCoordinateInField)
                .Select(c => levelModel[c])
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
                    .Where(c => math.all(c.Get<LevelTileComponent>().Coordinate != tileCoordinate - entityDirection))
                    .ToArray();
            }

            var index = world.RandomGenerator.Range(0, tileCoordinates.Length, (int) world.Tick);
            return tileCoordinates[index];
        }
    }
}
