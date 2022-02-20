using System.Linq;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Entity.Behaviours
{
    public class MovementBehaviourAgent : BehaviourAgent
    {
        private fix2 _prevWorldPosition;
        private fix2 _entityWorldPosition;

        public MovementBehaviourAgent(fix2 entityWorldPosition)
        {
            _prevWorldPosition = entityWorldPosition;
            _entityWorldPosition = entityWorldPosition;
        }

        public override void Update(GameContext gameContext, IEntity entity)
        {
            if (fix2.distanceq(_prevWorldPosition, _entityWorldPosition) < new fix(0.00001))
                _prevWorldPosition = _entityWorldPosition - new fix2(math.int2(1, 0));

            if (fix2.distanceq(_entityWorldPosition, _prevWorldPosition) < fix2.distanceq(entity.WorldPosition, _prevWorldPosition))
                return;
            /*if (fix2.distanceq(entity.WorldPosition, _entityWorldPosition) > new fix(0.01))
                return;*/

            var levelGridModel = gameContext.LevelGridModel;

            if (entity.Direction.x == 0 || entity.Direction.y == 0)
                entity.Direction = math.int2(1, 0);

            var currentTileCoordinate = levelGridModel.ToTileCoordinate(entity.WorldPosition);
            var nextTileCoordinate = currentTileCoordinate + (int2) math.normalize(entity.Direction);

            if (math.any(nextTileCoordinate < 0) || !math.all(nextTileCoordinate < levelGridModel.Size))
                nextTileCoordinate = GetRandomTileCoordinate(levelGridModel, currentTileCoordinate);

            var tile = levelGridModel[nextTileCoordinate];
            var tileType = tile.Type;

            nextTileCoordinate = GetNextTileCoordinate(tileType, levelGridModel, currentTileCoordinate, nextTileCoordinate);

            entity.Direction = (int2) math.normalize(nextTileCoordinate - currentTileCoordinate);
            entity.Speed = 1;

            entity.WorldPosition = _prevWorldPosition + (fix2) entity.Direction * fix2.distance(entity.WorldPosition, _entityWorldPosition);

            /*var tileCoordinate = levelGridModel.ToTileCoordinate(_entityWorldPosition);
            entity.WorldPosition = tileCoordinate + ;*/

            _prevWorldPosition = _entityWorldPosition;
            _entityWorldPosition = levelGridModel.ToWorldPosition(nextTileCoordinate);
        }

        private static int2 GetNextTileCoordinate(LevelTileType tileType, GameLevelGridModel levelGridModel,
            int2 tileCoordinate, int2 nextTileCoordinate)
        {
            return tileType switch
            {
                LevelTileType.HardBlock => GetRandomTileCoordinate(levelGridModel, tileCoordinate),
                LevelTileType.SoftBlock => GetRandomTileCoordinate(levelGridModel, tileCoordinate),
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
