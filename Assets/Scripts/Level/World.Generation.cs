using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Configs;
using Configs.Game;
using Configs.Level;
using Data;
using Game.Enemies;
using Game.Hero;
using Infrastructure.Factory;
using Infrastructure.Services.Input;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;

namespace Level
{
    public partial class World
    {
        private readonly Dictionary<int2, GameObject> _blocks = new();

        private void GenerateLevelStage(LevelStage levelStage) // :TODO: get IGameFactory from DI
        {
            var levelConfig = levelStage.LevelConfig;
            var levelStageConfig = levelStage.LevelStageConfig;

            LevelModel = new LevelModel(this, levelConfig, levelStageConfig);

            var spawnBlocksTask = SpawnBlocks(levelConfig, LevelModel, _gameFactory);
            var spawnWallsTask = SpawnWalls(_gameFactory, levelConfig, LevelModel);

            Task.WhenAll(spawnBlocksTask, spawnWallsTask);
        }

        private static async Task SpawnWalls(IGameFactory gameFactory, LevelConfig levelConfig, LevelModel levelModel)
        {
            var loadTask = gameFactory.InstantiatePrefabAsync(levelConfig.Walls, Vector3.zero);

            var columnsNumber = levelModel.ColumnsNumber;
            var rowsNumber = levelModel.RowsNumber;

            /*var offsetsAndSize = new[]
            {
                (math.float2(+columnsNumber / 2f + 1, 0), math.float2(2, rowsNumber)),
                (math.float2(-columnsNumber / 2f - 1, 0), math.float2(2, rowsNumber)),
                (math.float2(0, +rowsNumber / 2f + 1), math.float2(columnsNumber, 2)),
                (math.float2(0, -rowsNumber / 2f - 1), math.float2(columnsNumber, 2))
            };*/

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

        private async Task SpawnBlocks(LevelConfig levelConfig, LevelModel levelModel,
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
                var coordinate = levelModel.ToTileCoordinate(new fix2((fix) position.x, (fix) position.y));

                var blockIndex = Array.FindIndex(blocks, p => p.type == tileType);
                var blockPrefab = assets[blockIndex];
                var gameObject = gameFactory.InstantiatePrefab(blockPrefab, position, blocksGroup.transform);

                AddBlockPrefab(coordinate, gameObject);
            }
        }

        private void AddBlockPrefab(int2 coordinate, GameObject gameObject)
        {
            _blocks.TryAdd(coordinate, gameObject);
        }

        private void DestroyBlockPrefab(int2 coordinate)
        {
            if (_blocks.TryGetValue(coordinate, out var gameObject))
                gameObject.SetActive(false);
        }

        private async Task CreateAndSpawnEnemies(LevelStageConfig levelStageConfig)
        {
            var enemySpawnElements = levelStageConfig.Enemies;
            var enemyConfigs = enemySpawnElements
                .SelectMany(e => Enumerable.Range(0, e.Count).Select(_ => e.EnemyConfig))
                .ToArray();

            var playersCoordinates = Players.Values
                .Select(p => LevelModel.ToTileCoordinate(p.Hero.WorldPosition))
                .ToArray();

            var floorTiles = LevelModel.GetTilesByType(LevelTileType.FloorTile)
                .Where(t => !playersCoordinates.Contains(t.Coordinate))
                .ToList();

            Assert.IsTrue(enemyConfigs.Length <= floorTiles.Count, "enemies to spawn count greater than the floor tiles count");

            foreach (var enemyConfig in enemyConfigs)
            {
                var index = RandomGenerator.Range(0, floorTiles.Count, levelStageConfig.Index);
                var floorTile = floorTiles[index];
                var task = _gameFactory.InstantiatePrefabAsync(enemyConfig.Prefab, fix2.ToXY(floorTile.WorldPosition));
                var go = await task;
                Assert.IsNotNull(go);

                floorTiles.RemoveAt(index);

                var entityController = go.GetComponent<EnemyController>();
                Assert.IsNotNull(entityController);

                var enemy = _gameFactory.CreateEnemy(enemyConfig, entityController, NewEntity());
                Assert.IsNotNull(enemy);

                AddEnemy(enemy);

                var behaviourAgents = _gameFactory.CreateBehaviourAgent(enemyConfig.BehaviourConfig, enemy);
                foreach (var behaviourAgent in behaviourAgents)
                    AddBehaviourAgent(enemy, behaviourAgent);

                _gameFactory.AddBehaviourComponents(enemyConfig.BehaviourConfig, enemy, enemy.Id);
            }
        }

        private void CreatePlayersAndSpawnHeroesPvP(GameModePvPConfig gameMode, LevelStagePvPConfig levelStageConfig,
            IInputService inputService)
        {
            var playerConfigs = gameMode.PlayerConfigs;
            var spawnCorners = levelStageConfig.PlayersSpawnCorners;
            Assert.IsTrue(playerConfigs.Length <= spawnCorners.Length, "players count greater than the level spawn corners");

            var tasks = spawnCorners
                .Zip(playerConfigs, (spawnCorner, playerConfig) => (spawnCorner, playerConfig))
                .Select(p => CreatePlayerAndSpawnHero(p.playerConfig, p.spawnCorner, inputService))
                .ToArray();

            Task.WhenAll(tasks);
        }

        private async Task CreatePlayersAndSpawnHeroesPvE(GameModePvEConfig gameMode,
            LevelStagePvEConfig levelStageConfig, IInputService inputService)
        {
            await CreatePlayerAndSpawnHero(gameMode.PlayerConfig, levelStageConfig.PlayerSpawnCorner, inputService);
        }

        private async Task CreatePlayerAndSpawnHero(PlayerConfig playerConfig, int2 spawnCorner, IInputService inputService)
        {
            var player = _gameFactory.CreatePlayer(playerConfig);
            Assert.IsNotNull(player);

            var playerInput = inputService.RegisterPlayerInput(player);
            AttachPlayerInput(player, playerInput);

            var spawnCoordinate = LevelModel.GetCornerWorldPosition(spawnCorner);
            var task = _gameFactory.InstantiatePrefabAsync(playerConfig.HeroConfig.Prefab, fix2.ToXY(spawnCoordinate));
            var go = await task;
            Assert.IsNotNull(go);

            var heroController = go.GetComponent<HeroController>();
            Assert.IsNotNull(heroController);

            var hero = _gameFactory.CreateHero(playerConfig.HeroConfig, heroController, NewEntity());
            player.AttachHero(hero);

            AddPlayer(playerConfig.PlayerTagConfig, player);
        }
    }
}
