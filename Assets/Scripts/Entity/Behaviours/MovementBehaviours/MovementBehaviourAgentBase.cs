using System.Linq;
using Configs.Behaviours;
using JetBrains.Annotations;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Entity.Behaviours
{
    public abstract class MovementBehaviourAgentBase : BehaviourAgent
    {
        protected fix2 FromWorldPosition;
        protected fix2 ToWorldPosition;

        protected static int2[] MovementDirections;

        private static LevelTileType[] _fordableTileTypes;

        private static bool _tryToSelectNewTile;

        protected MovementBehaviourAgentBase(MovementBehaviourBaseConfig config, IEntity entity)
        {
            MovementDirections = config.MovementDirections;

            FromWorldPosition = entity.WorldPosition;
            ToWorldPosition = entity.WorldPosition;

            _fordableTileTypes = entity.EntityConfig.FordableTileTypes;

            _tryToSelectNewTile = config.TryToSelectNewTile;
        }

        protected static bool IsTileCanBeAsMovementTarget(ILevelTileView tile)
        {
            return _fordableTileTypes.Contains(tile.Type);
        }

        [CanBeNull]
        protected static ILevelTileView GetRandomNeighborTile(GameLevelGridModel levelGridModel, int2 tileCoordinate,
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

        protected virtual bool IsNeedToUpdate(fix2 worldPosition)
        {
            var directionA = ToWorldPosition - FromWorldPosition;
            var directionC = worldPosition - ToWorldPosition;

            var lengthSqA = fix2.lengthsq(directionA);
            var lengthSqC = fix2.lengthsq(directionC);

            var isEntityMoved = lengthSqA > fix.zero;
            if (!isEntityMoved)
                return true;

            return lengthSqA <= fix2.distancesq(worldPosition, FromWorldPosition) + lengthSqC;
        }
    }
}
