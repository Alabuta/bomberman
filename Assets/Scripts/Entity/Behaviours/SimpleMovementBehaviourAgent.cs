using Configs.Behaviours;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Entity.Behaviours
{
    public class SimpleMovementBehaviourAgent : MovementBehaviourAgentBase
    {
        public SimpleMovementBehaviourAgent(SimpleMovementBehaviourConfig config, IEntity entity)
            : base(config, entity)
        {
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
