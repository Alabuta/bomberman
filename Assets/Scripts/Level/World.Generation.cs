using System.Collections.Generic;
using Configs.Level;
using Configs.Level.Tile;
using Data;
using Infrastructure.Factory;
using Unity.Mathematics;
using UnityEngine;

namespace Level
{
    public partial class World
    {
        public void GenerateLevelStage(LevelStage levelStage, IGameFactory gameFactory)
        {
            var levelConfig = levelStage.LevelConfig;
            var levelStageConfig = levelStage.LevelStageConfig;

            LevelModel = new LevelModel(levelConfig, levelStageConfig);

            SpawnBlocks(levelConfig, LevelModel, gameFactory);

            SpawnWalls(levelConfig, LevelModel);
        }

        private static void SpawnWalls(LevelConfig levelConfig, LevelModel levelModel)
        {
            var columnsNumber = levelModel.ColumnsNumber;
            var rowsNumber = levelModel.RowsNumber;

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

        private static void SpawnBlocks(LevelConfig levelConfig, LevelModel levelModel,
            IGameFactory gameFactory)
        {
            var columnsNumber = levelModel.ColumnsNumber;
            var rowsNumber = levelModel.RowsNumber;

            var hardBlocksGroup = new GameObject("HardBlocks");
            var softBlocksGroup = new GameObject("SoftBlocks");

            // :TODO: refactor
            var blocks = new Dictionary<LevelTileType, (GameObject, BlockConfig)>
            {
                { LevelTileType.HardBlock, (hardBlocksGroup, levelConfig.HardBlockConfig) },
                { LevelTileType.SoftBlock, (softBlocksGroup, levelConfig.SoftBlockConfig) }
            };

            var startPosition = (math.float3(1) - math.float3(columnsNumber, rowsNumber, 0)) / 2;

            for (var i = 0; i < columnsNumber * rowsNumber; ++i)
            {
                var tile = levelModel[i];
                var tileType = tile.Type;
                if (tileType == LevelTileType.FloorTile)
                    continue;

                // ReSharper disable once PossibleLossOfFraction
                var position = startPosition + math.float3(i % columnsNumber, i / columnsNumber, 0);

                var (parent, blockConfig) = blocks[tileType /* & ~LevelTileType.PowerUpItem*/];
                gameFactory.InstantiatePrefab(blockConfig.Prefab, position, parent.transform);
            }
        }
    }
}
