using System;
using System.Linq;
using System.Threading.Tasks;
using Configs.Level;
using Data;
using Infrastructure.Factory;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Level
{
    public partial class World
    {
        public void GenerateLevelStage(IGameFactory gameFactory, LevelStage levelStage) // :TODO: get IGameFactory from DI
        {
            var levelConfig = levelStage.LevelConfig;
            var levelStageConfig = levelStage.LevelStageConfig;

            LevelModel = new LevelModel(levelConfig, levelStageConfig);

            var spawnBlocksTask = SpawnBlocks(levelConfig, LevelModel, gameFactory);
            var spawnWallsTask = SpawnWalls(gameFactory, levelConfig, LevelModel);

            Task.WhenAll(spawnBlocksTask, spawnWallsTask);
        }

        private static async Task SpawnWalls(IGameFactory gameFactory, LevelConfig levelConfig, LevelModel levelModel)
        {
            var loadTask = gameFactory.InstantiatePrefabAsync(levelConfig.Walls, Vector3.zero);

            var columnsNumber = levelModel.ColumnsNumber;
            var rowsNumber = levelModel.RowsNumber;

            var offsetsAndSize = new[]
            {
                (math.float2(+columnsNumber / 2f + 1, 0), math.float2(2, rowsNumber)),
                (math.float2(-columnsNumber / 2f - 1, 0), math.float2(2, rowsNumber)),
                (math.float2(0, +rowsNumber / 2f + 1), math.float2(columnsNumber, 2)),
                (math.float2(0, -rowsNumber / 2f - 1), math.float2(columnsNumber, 2))
            };

            var walls = await loadTask;

            var sprite = walls.GetComponent<SpriteRenderer>();
            sprite.size += new Vector2(columnsNumber, rowsNumber);

            /*foreach (var (offset, size) in offsetsAndSize) // :TODO: replace to own colliders
            {
                var collider = walls.AddComponent<BoxCollider2D>();
                collider.offset = offset;
                collider.size = size;
            }*/
        }

        private static async Task SpawnBlocks(LevelConfig levelConfig, LevelModel levelModel,
            IGameFactory gameFactory)
        {
            // :TODO: refactor
            (LevelTileType type, AssetReferenceGameObject prefab)[] blocks =
            {
                (LevelTileType.HardBlock, levelConfig.HardBlockConfig.Prefab),
                (LevelTileType.SoftBlock, levelConfig.SoftBlockConfig.Prefab)
            };

            var loadTask = gameFactory.LoadAssetsAsync<GameObject>(blocks.Select(pair => pair.prefab));

            var blocksGroup = new GameObject("Blocks");

            var columnsNumber = levelModel.ColumnsNumber;
            var rowsNumber = levelModel.RowsNumber;

            var startPosition = (math.float3(1) - math.float3(columnsNumber, rowsNumber, 0)) / 2;

            var assets = await loadTask;

            for (var i = 0; i < columnsNumber * rowsNumber; ++i)
            {
                var tile = levelModel[i];
                var tileType = tile.Type;
                if (tileType == LevelTileType.FloorTile)
                    continue;

                // ReSharper disable once PossibleLossOfFraction
                var position = startPosition + math.float3(i % columnsNumber, i / columnsNumber, 0);

                var blockIndex = Array.FindIndex(blocks, p => p.type == tileType);
                var blockPrefab = assets[blockIndex];
                gameFactory.InstantiatePrefab(blockPrefab, position, blocksGroup.transform);
            }
        }
    }
}
