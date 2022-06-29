using System.Linq;
using Configs.Behaviours;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Game.Behaviours.MovementBehaviours
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

        public override void Update(GameContext2 gameContext2, IEntity entity, fix deltaTime)
        {
            var levelGridModel = gameContext2.LevelModel;

            var path = entity.Speed * deltaTime;
            var worldPosition = entity.WorldPosition + (fix2) entity.Direction * path;

            if (!IsNeedToUpdate(worldPosition))
            {
                entity.WorldPosition = worldPosition;
                return;
            }

            var currentTileCoordinate = levelGridModel.ToTileCoordinate(ToWorldPosition);
            int2 targetTileCoordinate;

            if (--_directionChangesCount < 1)
            {
                _directionChangesCount = gameContext2.World.RandomGenerator.Range(_changeFrequencyLowerBound,
                    _changeFrequencyUpperBound + 1, (int) gameContext2.World.Tick);

                var neighborTiles = MovementDirections
                    .Select(d => currentTileCoordinate + d)
                    .Where(levelGridModel.IsCoordinateInField)
                    .Select(c => levelGridModel[c])
                    .Where(IsTileCanBeAsMovementTarget)
                    .ToArray();

                if (neighborTiles.Length == 0)
                {
                    entity.Direction = int2.zero;
                    entity.Speed = fix.zero;

                    return;
                }

                var index = gameContext2.World.RandomGenerator.Range(0, neighborTiles.Length, (int) gameContext2.World.Tick);
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
                        GetRandomNeighborTile(gameContext2.World, levelGridModel, currentTileCoordinate, entity.Direction)
                            ?.Coordinate ??
                        currentTileCoordinate;
            }

            entity.Direction = (int2) math.normalize(entity.Direction);

            var targetTile = levelGridModel[targetTileCoordinate];
            targetTile = IsTileCanBeAsMovementTarget(targetTile)
                ? targetTile
                : GetRandomNeighborTile(gameContext2.World, levelGridModel, currentTileCoordinate, entity.Direction);

            if (targetTile == null)
            {
                entity.Direction = int2.zero;
                entity.Speed = fix.zero;

                return;
            }

            targetTileCoordinate = targetTile.Coordinate;

            entity.Direction = (int2) math.normalize(targetTileCoordinate - currentTileCoordinate);
            entity.Speed = fix.one;

            entity.WorldPosition =
                ToWorldPosition + (fix2) entity.Direction * fix2.distance(entity.WorldPosition, ToWorldPosition);

            FromWorldPosition = ToWorldPosition;
            ToWorldPosition = levelGridModel.ToWorldPosition(targetTileCoordinate);
        }
    }
}
