using System.Linq;
using Configs.Behaviours;
using Math.FixedPointMath;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Entity.Behaviours.MovementBehaviours
{
    public class AdvancedMovementBehaviourAgent : MovementBehaviourAgentBase
    {
        private readonly int _changeFrequencyLowerBound;
        private readonly int _changeFrequencyUpperBound;

        private int _directionChangesCount;

        public AdvancedMovementBehaviourAgent(AdvancedMovementBehaviourConfig config, IEntity entity)
            : base(config, entity)
        {
            _changeFrequencyLowerBound = config.DirectionChangeFrequency.Min;
            _changeFrequencyUpperBound = config.DirectionChangeFrequency.Max;
        }

        public override void Update(GameContext gameContext, IEntity entity)
        {
            var levelGridModel = gameContext.LevelGridModel;

            if (!IsNeedToUpdate(entity))
                return;

            var currentTileCoordinate = levelGridModel.ToTileCoordinate(ToWorldPosition);
            int2 targetTileCoordinate;

            if (--_directionChangesCount < 1)
            {
                var range = _changeFrequencyUpperBound - _changeFrequencyLowerBound;
                _directionChangesCount = (int) math.round(Random.value * range) + _changeFrequencyLowerBound;

                var neighborTiles = MovementDirections
                    .Select(d => currentTileCoordinate + d)
                    .Where(levelGridModel.IsCoordinateInField)
                    .Select(c => levelGridModel[c])
                    .Where(IsTileCanBeAsMovementTarget)
                    .ToArray();

                if (neighborTiles.Length == 0)
                {
                    entity.Direction = int2.zero;
                    entity.Speed = 0;

                    return;
                }

                var index = (int) math.round(Random.value * (neighborTiles.Length - 1));
                targetTileCoordinate = neighborTiles[index].Coordinate;

                entity.Direction = targetTileCoordinate - currentTileCoordinate;
            }
            else
            {
                if (entity.Direction.x == 0 && entity.Direction.y == 0)
                    entity.Direction = math.int2(1, 0);

                targetTileCoordinate = currentTileCoordinate + entity.Direction;

                if (!levelGridModel.IsCoordinateInField(targetTileCoordinate))
                    targetTileCoordinate =
                        GetRandomNeighborTile(levelGridModel, currentTileCoordinate, entity.Direction)?.Coordinate ??
                        currentTileCoordinate;
            }

            entity.Direction = (int2) math.normalize(entity.Direction);

            var targetTile = levelGridModel[targetTileCoordinate];
            targetTile = IsTileCanBeAsMovementTarget(targetTile)
                ? targetTile
                : GetRandomNeighborTile(levelGridModel, currentTileCoordinate, entity.Direction);

            if (targetTile == null)
            {
                entity.Direction = int2.zero;
                entity.Speed = 0;

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
    }
}
