using System.Collections.Generic;
using System.Linq;
using Configs.Game;
using Configs.Level;
using Data;
using Game;
using Unity.Mathematics;
using UnityEngine;

namespace Level
{
    public class GameLevelManager
    {
        public GameLevelGridModel LevelGridModel { get; private set; }

        private readonly Dictionary<PlayerTagConfig, IPlayer> _players = new();

        public void GenerateLevelStage(GameModeBaseConfig gameMode, LevelStage levelStage)
        {
            var levelConfig = gameMode.LevelConfigs[levelStage.LevelIndex];
            var levelStageConfig = levelConfig.LevelStages.First();

            LevelGridModel = new GameLevelGridModel(levelStageConfig);

            /*_hiddenItemsIndices = LevelGridModel
                .Select((_, i) => i)
                .Where(i => (LevelGridModel[i] & GridTileType.PowerUpItem) != 0)
                .ToArray();*/

            SpawnGameObjects(levelConfig, LevelGridModel);

            SetupWalls(levelConfig, LevelGridModel);
        }

        public void AddPlayer(PlayerTagConfig playerTagConfig, IPlayer player)
        {
            _players.Add(playerTagConfig, player);// :TODO: refactor
        }

        public IPlayer GetPlayer(PlayerTagConfig playerTagConfig)
        {
            return _players.TryGetValue(playerTagConfig, out var player) ? player : null;// :TODO: refactor
        }

        private static void SetupWalls(LevelConfig levelConfig, GameLevelGridModel gameLevelGridModel)
        {
            var columnsNumber = gameLevelGridModel.ColumnsNumber;
            var rowsNumber = gameLevelGridModel.RowsNumber;

            var walls = Object.Instantiate(levelConfig.Walls, Vector3.zero, Quaternion.identity);

            var sprite = walls.GetComponent<SpriteRenderer>();
            sprite.size += new Vector2(columnsNumber, rowsNumber);

            var offsetsAndSize = new[]
            {
                (math.float2(+columnsNumber / 2f + 1, 0), math.float2(2, rowsNumber)),
                (math.float2(-columnsNumber / 2f - 1, 0), math.float2(2, rowsNumber)),
                (math.float2(0, +rowsNumber / 2f + 1), math.float2(columnsNumber, 2)),
                (math.float2(0, -rowsNumber / 2f - 1), math.float2(columnsNumber, 2))
            };

            foreach (var (offset, size) in offsetsAndSize)
            {
                var collider = walls.AddComponent<BoxCollider2D>();
                collider.offset = offset;
                collider.size = size;
            }
        }

        private static void SpawnGameObjects(LevelConfig levelConfig, GameLevelGridModel gameLevelGridModel)
        {
            var columnsNumber = gameLevelGridModel.ColumnsNumber;
            var rowsNumber = gameLevelGridModel.RowsNumber;

            var hardBlocksGroup = new GameObject("HardBlocks");
            var softBlocksGroup = new GameObject("SoftBlocks");

            // :TODO: refactor
            var blocks = new Dictionary<GridTileType, (GameObject, GameObject)>
            {
                { GridTileType.HardBlock, (hardBlocksGroup, levelConfig.HardBlockConfig.Prefab) },
                { GridTileType.SoftBlock, (softBlocksGroup, levelConfig.SoftBlockConfig.Prefab) }
            };

            var startPosition = (math.float3(1) - math.float3(columnsNumber, rowsNumber, 0)) / 2;

            for (var i = 0; i < columnsNumber * rowsNumber; ++i)
            {
                var blockType = gameLevelGridModel[i];
                if (blockType == GridTileType.FloorTile)
                    continue;

                // ReSharper disable once PossibleLossOfFraction
                var position = startPosition + math.float3(i % columnsNumber, i / columnsNumber, 0);

                var (parent, prefab) = blocks[blockType & ~GridTileType.PowerUpItem];
                Object.Instantiate(prefab, position, Quaternion.identity, parent.transform);
            }
        }
    }
}
