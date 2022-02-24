using System.Linq;
using Configs.Behaviours;
using JetBrains.Annotations;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Entity.Behaviours
{
    public class MovementBehaviourAgent : BehaviourAgent
    {
        private static int2[] _movementDirections;

        private fix2 _fromWorldPosition;
        private fix2 _toWorldPosition;

        private static bool _tryToSelectNewTile;

        private static LevelTileType[] _fordableTileTypes;

        public MovementBehaviourAgent(MovementBehaviourConfig config, IEntity entity)
        {
            _movementDirections = config.MovementDirections;

            _fromWorldPosition = entity.WorldPosition;
            _toWorldPosition = entity.WorldPosition;

            _tryToSelectNewTile = config.TryToSelectNewTile;

            _fordableTileTypes = entity.EntityConfig.FordableTileTypes;
        }

        public override void Update(GameContext gameContext, IEntity entity)
        {
            var levelGridModel = gameContext.LevelGridModel;

            var directionA = _toWorldPosition - _fromWorldPosition;
            var directionC = entity.WorldPosition - _toWorldPosition;

            var lengthSqA = fix2.lengthsq(directionA);
            var lengthSqC = fix2.lengthsq(directionC);

            var isEntityMoved = lengthSqA > fix.zero;
            if (isEntityMoved)
            {
                var directionB = entity.WorldPosition - _fromWorldPosition;

                if (lengthSqA > fix2.lengthsq(directionB) + lengthSqC)
                    return;
            }

            if (entity.Direction.x == 0 && entity.Direction.y == 0)
                entity.Direction = math.int2(1, 0);

            var entityDirection = (int2) math.normalize(entity.Direction);

            var currentTileCoordinate = levelGridModel.ToTileCoordinate(_toWorldPosition);
            var targetTileCoordinate = currentTileCoordinate + entityDirection;

            if (!levelGridModel.IsCoordinateInField(targetTileCoordinate))
                targetTileCoordinate =
                    GetRandomNeighborTile(levelGridModel, currentTileCoordinate, entityDirection)?.Coordinate ??
                    currentTileCoordinate;

            var targetTile = levelGridModel[targetTileCoordinate];
            targetTile = IsTileCanBeAsMovementTarget(targetTile)
                ? targetTile
                : GetRandomNeighborTile(levelGridModel, currentTileCoordinate, entityDirection);

            targetTileCoordinate = targetTile?.Coordinate ?? currentTileCoordinate;

            entity.Direction = (int2) math.normalize(targetTileCoordinate - currentTileCoordinate);
            entity.Speed = 1;

            entity.WorldPosition = _toWorldPosition + (fix2) entity.Direction * fix.sqrt(lengthSqC);

            _fromWorldPosition = _toWorldPosition;
            _toWorldPosition = levelGridModel.ToWorldPosition(targetTileCoordinate);
        }

        private static bool IsTileCanBeAsMovementTarget(ILevelTileView tile)
        {
            return _fordableTileTypes.Contains(tile.Type);
        }

        [CanBeNull]
        private static ILevelTileView GetRandomNeighborTile(GameLevelGridModel levelGridModel, int2 tileCoordinate,
            int2 entityDirection)
        {
            var tileCoordinates = _movementDirections
                .Select(d => tileCoordinate + d)
                .Where(levelGridModel.IsCoordinateInField)
                .Select(c => levelGridModel[c])
                .Where(t => t.Type == LevelTileType.FloorTile)
                .ToArray();

            switch (tileCoordinates.Length)
            {
                case 0:
                    return null;

                case 1:
                    return tileCoordinates[0];
            }

            if (_tryToSelectNewTile)
            {
                tileCoordinates = tileCoordinates
                    .Where(c => math.all(c.Coordinate != tileCoordinate - entityDirection))
                    .ToArray();
            }

            var index = (int) math.round(Random.value * (tileCoordinates.Length - 1));
            return tileCoordinates[index];
        }
    }
}
