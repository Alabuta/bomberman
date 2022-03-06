using System.Linq;
using Configs.Behaviours;
using JetBrains.Annotations;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Entity.Behaviours
{
    public class AdvancedMovementBehaviourAgent : MovementBehaviourAgentBase
    {
        private readonly int2[] _coordinateOffsets;
        private readonly int _changeFrequency;
        private int _tilesCount;

        public AdvancedMovementBehaviourAgent(AdvancedMovementBehaviourConfig config, IEntity entity)
            : base(config, entity)
        {
            _coordinateOffsets = new[]
            {
                math.int2(1, 0),
                math.int2(-1, 0),
                math.int2(0, 1),
                math.int2(0, -1)
            };

            _changeFrequency = config.DirectionChangeFrequency;
        }

        public override void Update(GameContext gameContext, IEntity entity)
        {
            var levelGridModel = gameContext.LevelGridModel;

            if (!IsNeedToUpdate(entity))
                return;

            var targetTileCoordinate = int2.zero;
            ;

            var currentTileCoordinate = levelGridModel.ToTileCoordinate(ToWorldPosition);

            if (--_tilesCount < 1)
            {
                _tilesCount = (int) math.max(1, math.round(Random.value * _changeFrequency));

                var neighborTiles = _coordinateOffsets
                    .Select(o => currentTileCoordinate + o)
                    .Where(levelGridModel.IsCoordinateInField)
                    .Select(c => levelGridModel[c])
                    .Where(IsTileCanBeAsMovementTarget)
                    .ToArray();

                if (neighborTiles.Length == 0)
                {
                    entity.Direction = int2.zero;
                    entity.Speed = 1;

                    return;
                }

                var index = (int) math.round(Random.value * (neighborTiles.Length - 1));
                targetTileCoordinate = neighborTiles[index].Coordinate;

                entity.Direction = targetTileCoordinate - currentTileCoordinate;
            }

            else if (entity.Direction.x == 0 && entity.Direction.y == 0)
            {
                entity.Direction = math.int2(1, 0);

                targetTileCoordinate = currentTileCoordinate + entity.Direction;

                if (!levelGridModel.IsCoordinateInField(targetTileCoordinate))
                    targetTileCoordinate =
                        GetRandomNeighborTile(levelGridModel, currentTileCoordinate, entity.Direction)?.Coordinate ??
                        currentTileCoordinate;
            }

            var targetTile = levelGridModel[targetTileCoordinate];
            targetTile = IsTileCanBeAsMovementTarget(targetTile)
                ? targetTile
                : GetRandomNeighborTile(levelGridModel, currentTileCoordinate, entity.Direction);

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

        [CanBeNull]
        private static ILevelTileView GetRandomNeighborTile(GameLevelGridModel levelGridModel, int2 tileCoordinate,
            int2 entityDirection)
        {
            var tileCoordinates = MovementDirections
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

            var index = (int) math.round(Random.value * (tileCoordinates.Length - 1));
            return tileCoordinates[index];
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
    }
}
