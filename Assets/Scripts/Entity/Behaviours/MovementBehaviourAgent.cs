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
        private fix2 _fromWorldPosition;
        private fix2 _toWorldPosition;

        public MovementBehaviourAgent(IEntity entity)
        {
            _fromWorldPosition = entity.WorldPosition;
            _toWorldPosition = entity.WorldPosition;
        }

        public override void Update(GameContext gameContext, IEntity entity)
        {
            var levelGridModel = gameContext.LevelGridModel;

            var directionA = _toWorldPosition - _fromWorldPosition;

            var lengthSqA = fix2.lengthsq(directionA);
            if (lengthSqA == fix.zero)
            {
                if (entity.Direction.x == 0 && entity.Direction.y == 0)
                    entity.Direction = math.int2(1, 0);

                var currentTileCoordinate = levelGridModel.ToTileCoordinate(_fromWorldPosition);
                var targetTileCoordinate = currentTileCoordinate + (int2) math.normalize(entity.Direction);

                targetTileCoordinate = levelGridModel.ClampCoordinate(targetTileCoordinate);
            }
            else
            {
                var directionB = entity.WorldPosition - _fromWorldPosition;
                var directionC = entity.WorldPosition - _toWorldPosition;

                var lengthSqC = fix2.lengthsq(directionC);
                if (lengthSqA > fix2.lengthsq(directionB) + lengthSqC)
                    return;

                if (entity.Direction.x == 0 && entity.Direction.y == 0)
                    entity.Direction = math.int2(1, 0);
            }

            /*var currentTileCoordinate = levelGridModel.ToTileCoordinate(_toWorldPosition);
            var targetTileCoordinate = currentTileCoordinate + (int2) math.normalize(entity.Direction);

            if (math.any(targetTileCoordinate < 0) || !math.all(targetTileCoordinate < levelGridModel.Size))
                targetTileCoordinate = GetRandomTile(levelGridModel, currentTileCoordinate)?.Coordinate ??
                                       currentTileCoordinate;*/

            var tile = levelGridModel[targetTileCoordinate];
            var targetTile = GetNeighborTile(levelGridModel, tile, currentTileCoordinate);
            targetTileCoordinate = targetTile?.Coordinate ?? currentTileCoordinate;

            entity.Direction = (int2) math.normalize(targetTileCoordinate - currentTileCoordinate);
            entity.Speed = 1;

            entity.WorldPosition = _toWorldPosition + (fix2) entity.Direction * fix.sqrt(lengthSqC);

            _fromWorldPosition = _toWorldPosition;
            _toWorldPosition = levelGridModel.ToWorldPosition(targetTileCoordinate);
        }

        [CanBeNull]
        private static ILevelTileView GetNeighborTile(GameLevelGridModel levelGridModel,
            ILevelTileView tile,
            int2 fromTileCoordinate)
        {
            return tile.Type switch
            {
                LevelTileType.HardBlock => GetRandomTile(levelGridModel, fromTileCoordinate),
                LevelTileType.SoftBlock => GetRandomTile(levelGridModel, fromTileCoordinate),
                _ => tile
            };
        }

        [CanBeNull]
        private static ILevelTileView GetRandomTile(GameLevelGridModel levelGridModel, int2 tileCoordinate)
        {
            var directions = new[]
            {
                math.int2(0, 1),
                math.int2(0, -1),
                math.int2(1, 0),
                math.int2(-1, 0)
            };

            var tileCoordinates = directions
                .Select(d => tileCoordinate + d)
                .Where(c => math.all(c >= 0) && math.all(c < levelGridModel.Size))
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
