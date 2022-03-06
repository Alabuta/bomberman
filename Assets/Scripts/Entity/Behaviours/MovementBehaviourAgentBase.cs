using System.Linq;
using Configs.Behaviours;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Entity.Behaviours
{
    public abstract class MovementBehaviourAgentBase : BehaviourAgent
    {
        protected static int2[] MovementDirections;
        protected fix2 FromWorldPosition;
        protected fix2 ToWorldPosition;

        protected static LevelTileType[] FordableTileTypes;

        protected MovementBehaviourAgentBase(MovementBehaviourBaseConfig config, IEntity entity)
        {
            MovementDirections = config.MovementDirections;

            FromWorldPosition = entity.WorldPosition;
            ToWorldPosition = entity.WorldPosition;

            FordableTileTypes = entity.EntityConfig.FordableTileTypes;
        }

        protected static bool IsTileCanBeAsMovementTarget(ILevelTileView tile)
        {
            return FordableTileTypes.Contains(tile.Type);
        }

        protected abstract bool IsNeedToUpdate(IEntity entity);
    }
}
