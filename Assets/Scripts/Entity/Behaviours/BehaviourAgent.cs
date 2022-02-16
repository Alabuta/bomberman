using System.Linq;
using Configs;
using Level;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Entity.Behaviours
{
    public abstract class BehaviourAgent//<TConfig> where TConfig : BehaviourConfig
    {
        /*public TConfig Config { get; private set; }

        protected BehaviourAgent(TConfig config)
        {
            Config = config;
        }*/

        public abstract void Update(GameContext gameContext, IEntity entity);
    }

    public class BehaviourConfig : ConfigBase
    {
    }

    public class MovementBehaviourAgent : BehaviourAgent
    {
        public int2 TileCoordinate { get; protected set; }
        public float3 WorldPosition { get; protected set; }

        public override void Update(GameContext gameContext, IEntity entity)
        {
            var levelGridModel = gameContext.LevelGridModel;

            if (math.lengthsq(entity.Direction) < .01f)
            {
                // Random.Range(0, 100)
                entity.Direction = math.float2(1, 0);// :TODO: use random
            }

            var direction = (int2) math.normalize(entity.Direction);
            var worldPosition = entity.WorldPosition;

            var tileCoordinate = levelGridModel.ToTileCoordinate(worldPosition);
            var forwardCellCoordinate = tileCoordinate + direction;

            var tile = levelGridModel[forwardCellCoordinate];
            var tileType = tile.Type;

            forwardCellCoordinate = tileType switch
            {
                LevelTileType.HardBlock => GetRandomTileCoordinate(levelGridModel, tileCoordinate),
                LevelTileType.SoftBlock => GetRandomTileCoordinate(levelGridModel, tileCoordinate),
                _ => forwardCellCoordinate
            };

            TileCoordinate = forwardCellCoordinate;
            WorldPosition = levelGridModel.ToWorldPosition(tileCoordinate);
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
                .Where(c => math.all(c >= int2.zero) && math.all(c < levelGridModel.Size))
                .Where(c => levelGridModel[c].Type == LevelTileType.FloorTile)
                .ToArray();

            var index = (int) (Random.value * (tileCoordinates.Length - 1));
            return tileCoordinates[index];
        }
    }

    public class GameContext
    {
        public GameLevelGridModel LevelGridModel { get; }
    }

    public class GridGraph
    {
        public int2 Size { get; set; }
    }
}
