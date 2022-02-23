using System.Linq;
using JetBrains.Annotations;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Entity.Behaviours
{
    public class MovementBehaviourAgent : BehaviourAgent
    {
        private static int2[] _possibleMovementDirections;

        private fix2 _fromWorldPosition;
        private fix2 _toWorldPosition;

        public MovementBehaviourAgent(IEntity entity)
        {
            _possibleMovementDirections = new[]
            {
                math.int2(0, 1),
                math.int2(0, -1),
                math.int2(1, 0),
                math.int2(-1, 0)
            };

            _fromWorldPosition = entity.WorldPosition;
            _toWorldPosition = entity.WorldPosition;
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

            var currentTileCoordinate = levelGridModel.ToTileCoordinate(_toWorldPosition);
            var targetTileCoordinate = currentTileCoordinate + (int2) math.normalize(entity.Direction);

            if (!levelGridModel.IsCoordinateInField(targetTileCoordinate))
                targetTileCoordinate = GetRandomNeighborTile(levelGridModel, currentTileCoordinate)?.Coordinate ??
                                       currentTileCoordinate;

            var targetTile = levelGridModel[targetTileCoordinate];
            targetTile = IsTileCanBeAsMovementTarget(targetTile)
                ? targetTile
                : GetRandomNeighborTile(levelGridModel, currentTileCoordinate);

            targetTileCoordinate = targetTile?.Coordinate ?? currentTileCoordinate;

            entity.Direction = (int2) math.normalize(targetTileCoordinate - currentTileCoordinate);
            entity.Speed = 1;

            entity.WorldPosition = _toWorldPosition + (fix2) entity.Direction * fix.sqrt(lengthSqC);

            _fromWorldPosition = _toWorldPosition;
            _toWorldPosition = levelGridModel.ToWorldPosition(targetTileCoordinate);
        }

        private static bool IsTileCanBeAsMovementTarget(ILevelTileView tile)
        {
            return tile.Type switch
            {
                LevelTileType.HardBlock => false,
                LevelTileType.SoftBlock => false,
                _ => true
            };
        }

        [CanBeNull]
        private static ILevelTileView GetRandomNeighborTile(GameLevelGridModel levelGridModel, int2 tileCoordinate)
        {
            var tileCoordinates = _possibleMovementDirections
                .Select(d => tileCoordinate + d)
                .Where(levelGridModel.IsCoordinateInField)
                .Select(c => levelGridModel[c])
                .Where(t => t.Type == LevelTileType.FloorTile)
                .ToArray();

            if (tileCoordinates.Length == 0)
                return null;

            var index = (int) math.round(Random.value * (tileCoordinates.Length - 1));
            return tileCoordinates[index];
        }
    }
}
