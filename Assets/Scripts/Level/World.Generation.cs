using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Configs;
using Configs.Behaviours;
using Configs.Entity;
using Configs.Game;
using Configs.Level;
using Data;
using Game.Components;
using Game.Enemies;
using Game.Hero;
using Infrastructure.Factory;
using Infrastructure.Services.Input;
using Leopotam.Ecs;
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

                var task = CreateAndSpawnEnemy(enemyConfig, enemyConfig.BehaviourConfigs, floorTile.WorldPosition);
                var enemy = await task;

                AddEnemy(enemy);

                floorTiles.RemoveAt(index);
            }
        }

        private void CreatePlayersAndSpawnHeroesPvP(GameModePvPConfig gameMode, LevelStagePvPConfig levelStageConfig,
            IInputService inputService)
        {
            var playerConfigs = gameMode.PlayerConfigs;
            var spawnCorners = levelStageConfig.PlayersSpawnCorners;
            Assert.IsTrue(playerConfigs.Length <= spawnCorners.Length, "players count greater than the level spawn corners");

            var tasks = spawnCorners
                .Zip(playerConfigs,
                    (spawnCorner, playerConfig) => (LevelModel.GetCornerWorldPosition(spawnCorner), playerConfig))
                .Select(p => CreatePlayerAndSpawnHero(inputService, p.playerConfig, p.Item1))
                .ToArray();

            Task.WhenAll(tasks);
        }

        private async Task CreatePlayersAndSpawnHeroesPvE(GameModePvEConfig gameMode,
            LevelStagePvEConfig levelStageConfig, IInputService inputService)
        {
            var spawnCoordinate = LevelModel.GetCornerWorldPosition(levelStageConfig.PlayerSpawnCorner);
            await CreatePlayerAndSpawnHero(inputService, gameMode.PlayerConfig, spawnCoordinate);
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

        private async Task<EcsEntity> CreatePlayerAndSpawnHero(IInputService inputService, PlayerConfig playerConfig,
            fix2 position)
        {
            var heroConfig = playerConfig.HeroConfig;
            var task = _gameFactory.InstantiatePrefabAsync(heroConfig.Prefab, fix2.ToXY(position));

            var player = _gameFactory.CreatePlayer(playerConfig);
            Assert.IsNotNull(player);

            var playerInput = inputService.RegisterPlayerInput(player);
            AttachPlayerInput(player, playerInput);

            var entity = _ecsWorld.NewEntity();

            entity.Replace(new TransformComponent
            {
                WorldPosition = position,
                Direction = heroConfig.StartDirection,
                Speed = fix.zero
            });

            entity.Replace(new HealthComponent
            {
                CurrentHealth = heroConfig.Health,
                MaxHealth = heroConfig.Health
            });

            var go = await task;
            Assert.IsNotNull(go);

            var heroController = go.GetComponent<HeroController>();
            Assert.IsNotNull(heroController);

            entity.Replace(new HeroComponent
            {
                Config = heroConfig,
                Controller = heroController,

                HitRadius = (fix) heroConfig.HitRadius,
                HurtRadius = (fix) heroConfig.HurtRadius,

                InitialSpeed = (fix) heroConfig.Speed,
                SpeedMultiplier = fix.one,

                InteractionLayerMask = heroConfig.Collider.InteractionLayerMask
            });

            player.AttachHero(entity);
            AddPlayer(playerConfig.PlayerTagConfig, player);

            return entity;
        }

        private async Task<EcsEntity> CreateAndSpawnEnemy(EnemyConfig config, IEnumerable<BehaviourConfig> behaviourConfigs,
            fix2 position)
        {
            var entity = _ecsWorld.NewEntity();

            var task = _gameFactory.InstantiatePrefabAsync(config.Prefab, fix2.ToXY(position));
            var go = await task;
            Assert.IsNotNull(go);

            var enemyController = go.GetComponent<EnemyController>();
            Assert.IsNotNull(enemyController);

            entity.Replace(new TransformComponent
            {
                WorldPosition = position,
                Direction = config.StartDirection,
                Speed = fix.zero
            });

            entity.Replace(new HealthComponent
            {
                CurrentHealth = config.Health,
                MaxHealth = config.Health
            });

            _gameFactory.AddBehaviourComponents(behaviourConfigs, entity);

            entity.Replace(new EnemyComponent
            {
                Config = config,
                Controller = enemyController,

                HitRadius = (fix) config.HitRadius,
                HurtRadius = (fix) config.HurtRadius,

                InitialSpeed = (fix) config.Speed,
                SpeedMultiplier = fix.one,

                InteractionLayerMask = config.Collider.InteractionLayerMask
            });

            return entity;
        }
    }
}
