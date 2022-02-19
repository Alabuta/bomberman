using System.Linq;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Entity.Behaviours
{
    public class MovementBehaviourAgent : BehaviourAgent
    {
        private fix2 NextWorldPosition { get; set; }

        public MovementBehaviourAgent(fix2 nextWorldPosition)
        {
            NextWorldPosition = nextWorldPosition;
        }

        public override void Update(GameContext gameContext, IEntity entity)
        {
            if (fix2.distanceq(entity.WorldPosition, NextWorldPosition) > new fix(0.001))
                return;

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

            NextWorldPosition = levelGridModel.ToWorldPosition(nextTileCoordinate);
            entity.Direction = (int2) math.normalize(nextTileCoordinate - currentTileCoordinate);
            entity.Speed = 1;
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
