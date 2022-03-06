using System.Linq;
using Configs.Behaviours;
using JetBrains.Annotations;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Entity.Behaviours
{
    public class SimpleMovementBehaviourAgent : MovementBehaviourAgentBase
    {
        private static bool _tryToSelectNewTile;

        public SimpleMovementBehaviourAgent(SimpleMovementBehaviourConfig config, IEntity entity)
            : base(config, entity)
        {
            _tryToSelectNewTile = config.TryToSelectNewTile;
        }

        public override void Update(GameContext gameContext, IEntity entity)
        {
            var levelGridModel = gameContext.LevelGridModel;

            if (!IsNeedToUpdate(entity))
                return;

            if (entity.Direction.x == 0 && entity.Direction.y == 0)
                entity.Direction = math.int2(1, 0);

            var entityDirection = (int2) math.normalize(entity.Direction);

            var currentTileCoordinate = levelGridModel.ToTileCoordinate(ToWorldPosition);
            var targetTileCoordinate = currentTileCoordinate + entityDirection;

            if (!levelGridModel.IsCoordinateInField(targetTileCoordinate))
                targetTileCoordinate =
                    GetRandomNeighborTile(levelGridModel, currentTileCoordinate, entityDirection)?.Coordinate ??
                    currentTileCoordinate;

            var targetTile = levelGridModel[targetTileCoordinate];
            targetTile = IsTileCanBeAsMovementTarget(targetTile)
                ? targetTile
                : GetRandomNeighborTile(levelGridModel, currentTileCoordinate, entityDirection);

            if (targetTile == null)
            {
                entity.Direction = int2.zero;
                entity.Speed = 1;

                return;
            }

            targetTileCoordinate = targetTile.Coordinate;

            entity.Direction = (int2) math.normalize(targetTileCoordinate - currentTileCoordinate);
            entity.Speed = 1;

            entity.WorldPosition =
                ToWorldPosition + (fix2) entity.Direction * fix2.distance(entity.WorldPosition, ToWorldPosition);

            FromWorldPosition = ToWorldPosition;
            ToWorldPosition = levelGridModel.ToWorldPosition(targetTileCoordinate);
        }

        protected override bool IsNeedToUpdate(IEntity entity)
        {
            var directionA = ToWorldPosition - FromWorldPosition;
            var directionC = entity.WorldPosition - ToWorldPosition;

            var lengthSqA = fix2.lengthsq(directionA);
            var lengthSqC = fix2.lengthsq(directionC);

            var isEntityMoved = lengthSqA > fix.zero;
            if (!isEntityMoved)
                return true;

            return lengthSqA <= fix2.distanceq(entity.WorldPosition, FromWorldPosition) + lengthSqC;
        }

        [CanBeNull]
        private static ILevelTileView GetRandomNeighborTile(GameLevelGridModel levelGridModel, int2 tileCoordinate,
            int2 entityDirection)
        {
            var tileCoordinates = MovementDirections
                .Select(d => tileCoordinate + d)
                .Where(levelGridModel.IsCoordinateInField)
                .Select(c => levelGridModel[c])
                .Where(IsTileCanBeAsMovementTarget)
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
