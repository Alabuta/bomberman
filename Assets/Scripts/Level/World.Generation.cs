using System;
using System.Linq;
using System.Threading.Tasks;
using Configs.Level;
using Configs.Level.Tile;
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

            var task = SpawnBlocks(levelConfig, LevelModel, gameFactory);
            task.Wait(TimeSpan.FromSeconds(1));

            /*var task = Task.Run(async () => await SpawnBlocks(levelConfig, LevelModel, gameFactory));
            task.Wait();*/

            SpawnWalls(gameFactory, levelConfig, LevelModel);
        }

        private static void SpawnWalls(IGameFactory gameFactory, LevelConfig levelConfig, LevelModel levelModel)
        {
            var columnsNumber = levelModel.ColumnsNumber;
            var rowsNumber = levelModel.RowsNumber;

            // var walls = gameFactory.InstantiatePrefabAsync(levelConfig.Walls, Vector3.zero);
            var walls = gameFactory.InstantiatePrefab(levelConfig.Walls, Vector3.zero);

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

        private static async Task SpawnBlocks(LevelConfig levelConfig, LevelModel levelModel,
            IGameFactory gameFactory)
        {
            // :TODO: refactor
            (LevelTileType type, AssetReferenceGameObject prefab)[] blocks =
            {
                (LevelTileType.HardBlock, levelConfig.HardBlockConfig.Prefab),
                (LevelTileType.SoftBlock, levelConfig.SoftBlockConfig.Prefab)
            };

            Debug.LogWarning("pre");
            var loadTasks = gameFactory.LoadAssetsAsync<GameObject>(blocks.Select(pair => pair.prefab));

            var blocksGroup = new GameObject("Blocks");

            var columnsNumber = levelModel.ColumnsNumber;
            var rowsNumber = levelModel.RowsNumber;

            var startPosition = (math.float3(1) - math.float3(columnsNumber, rowsNumber, 0)) / 2;

            var assets = await loadTasks;
            Debug.LogWarning($"post {assets.Count}");

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

        private static async Task SpawnBlocks(IGameFactory gameFactory, LevelConfig levelConfig, LevelModel levelModel)
        {
            var blocks = new (LevelTileType type, BlockConfig config)[]
            {
                (LevelTileType.HardBlock, levelConfig.HardBlockConfig),
                (LevelTileType.SoftBlock, levelConfig.SoftBlockConfig)
            };

            var loadTasks = gameFactory.LoadAssetsAsync<GameObject>(blocks.Select(pair => pair.config.Prefab));

            var columnsNumber = levelModel.ColumnsNumber;
            var rowsNumber = levelModel.RowsNumber;

            var blocksGroup = new GameObject("Blocks");

            var startPosition = (math.float3(1) - math.float3(columnsNumber, rowsNumber, 0)) / 2;

            var assets = await loadTasks;

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
