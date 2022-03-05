using System.Linq;
using Configs.Behaviours;
using JetBrains.Annotations;
using Level;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Entity.Behaviours
{
    public class AdvancedMovementBehaviourAgent : MovementBehaviourAgentBase
    {
        private readonly int _changeFrequency;

        public AdvancedMovementBehaviourAgent(AdvancedMovementBehaviourConfig config, IEntity entity)
            : base(config, entity)
        {
            _changeFrequency = config.DirectionChangeFrequency;
        }

        public override void Update(GameContext gameContext, IEntity entity)
        {
            var levelGridModel = gameContext.LevelGridModel;

            entity.Direction = int2.zero;
            entity.Speed = 0;
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

            var index = (int)math.round(Random.value * (tileCoordinates.Length - 1));
            return tileCoordinates[index];
        }
    }
}
