using System.Linq;
using Configs.Behaviours;
using Game.Colliders;
using JetBrains.Annotations;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Game.Behaviours.MovementBehaviours
{
    public abstract class MovementBehaviourAgentBase : BehaviourAgent
    {
        protected fix2 FromWorldPosition;
        protected fix2 ToWorldPosition;

        protected static int2[] MovementDirections;

        private static bool _tryToSelectNewTile;

        private readonly int _interactionLayerMask;

        protected MovementBehaviourAgentBase(MovementBehaviourBaseConfig config, IEntity entity)
        {
            MovementDirections = config.MovementDirections;

            FromWorldPosition = entity.WorldPosition;
            ToWorldPosition = entity.WorldPosition;

            _tryToSelectNewTile = config.TryToSelectNewTile;

            _interactionLayerMask = entity.EntityConfig.Collider.InteractionLayerMask;
        }

        [CanBeNull]
        protected ILevelTileView GetRandomNeighborTile(LevelModel levelModel, int2 tileCoordinate,
            int2 entityDirection)
        {
            var tileCoordinates = MovementDirections
                .Select(d => tileCoordinate + d)
                .Where(levelModel.IsCoordinateInField)
                .Select(c => levelModel[c])
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

        protected bool IsTileCanBeAsMovementTarget(ILevelTileView tile)
        {
            var tileLoad = tile.TileLoad;
            if (tileLoad == null || (_interactionLayerMask & tileLoad.LayerMask) == 0)
                return true;

            return !(tileLoad.Components?.OfType<ColliderComponent>().Any() ?? false);
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
