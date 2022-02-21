using System.Linq;
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
            var directionA = _toWorldPosition - _fromWorldPosition;
            var directionB = entity.WorldPosition - _fromWorldPosition;
            var directionC = entity.WorldPosition - _toWorldPosition;

            var lengthSqA = fix2.lengthsq(directionA);
            var lengthSqC = fix2.lengthsq(directionC);

            if (lengthSqA > fix.zero && lengthSqA > fix2.lengthsq(directionB) + lengthSqC)
                return;

            var levelGridModel = gameContext.LevelGridModel;

            if (entity.Direction.x == 0 && entity.Direction.y == 0)
                entity.Direction = math.int2(1, 0);

            var expectedTileCoordinate = levelGridModel.ToTileCoordinate(_fromWorldPosition);
            var targetTileCoordinate = expectedTileCoordinate + (int2) math.normalize(entity.Direction);

            if (math.any(targetTileCoordinate < 0) || !math.all(targetTileCoordinate < levelGridModel.Size))
                targetTileCoordinate = GetRandomTileCoordinate(levelGridModel, expectedTileCoordinate);

            var tile = levelGridModel[targetTileCoordinate];
            var tileType = tile.Type;

            targetTileCoordinate =
                GetNextTileCoordinate(tileType, levelGridModel, expectedTileCoordinate, targetTileCoordinate);

            entity.Direction = (int2) math.normalize(targetTileCoordinate - expectedTileCoordinate);
            entity.Speed = 1;

            entity.WorldPosition = _toWorldPosition + (fix2) entity.Direction * fix.sqrt(lengthSqC);

            _fromWorldPosition = _toWorldPosition;
            _toWorldPosition = levelGridModel.ToWorldPosition(targetTileCoordinate);
        }

        private static int2 GetNextTileCoordinate(LevelTileType tileType, GameLevelGridModel levelGridModel,
            int2 fromTileCoordinate, int2 nextTileCoordinate)
        {
            return tileType switch
            {
                LevelTileType.HardBlock => GetRandomTileCoordinate(levelGridModel, fromTileCoordinate),
                LevelTileType.SoftBlock => GetRandomTileCoordinate(levelGridModel, fromTileCoordinate),
                _ => nextTileCoordinate
            };
        }

        private static int2 GetRandomTileCoordinate(GameLevelGridModel levelGridModel, int2 tileCoordinate)
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
                .Where(c => levelGridModel[c].Type == LevelTileType.FloorTile)
                .ToArray();

            var index = (int) math.round(Random.value * (tileCoordinates.Length - 1));
            return tileCoordinates[index];
        }
    }
}
