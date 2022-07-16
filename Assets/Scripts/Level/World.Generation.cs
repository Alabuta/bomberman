using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Configs.Entity;
using Configs.Game;
using Configs.Level;
using Data;
using Game;
using Game.Components;
using Game.Components.Entities;
using Game.Components.Tags;
using Game.Enemies;
using Game.Hero;
using Game.Items;
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

            LevelTiles = new LevelTiles(this, levelConfig, levelStageConfig);

            var spawnBlocksTask = SpawnBlocks(levelConfig, LevelTiles, _gameFactory);
            var spawnWallsTask = SpawnWalls(_gameFactory, levelConfig, LevelTiles);

            Task.WhenAll(spawnBlocksTask, spawnWallsTask);
        }

        private async Task SpawnWalls(IGameFactory gameFactory, LevelConfig levelConfig, LevelTiles levelTiles)
        {
            var loadTask = gameFactory.InstantiatePrefabAsync(levelConfig.Walls, Vector3.zero);

            var columnsNumber = levelTiles.ColumnsNumber;
            var rowsNumber = levelTiles.RowsNumber;

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

        private async Task SpawnBlocks(LevelConfig levelConfig, LevelTiles levelTiles,
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

            var columnsNumber = levelTiles.ColumnsNumber;
            var rowsNumber = levelTiles.RowsNumber;

            var startPosition = (math.float3(1) - math.float3(columnsNumber, rowsNumber, 0)) / 2;

            var assets = await loadTask;

            for (var i = 0; i < columnsNumber * rowsNumber; ++i)
            {
                var tile = levelTiles[i];
                var tileType = tile.Get<LevelTileComponent>().Type;
                if (tileType == LevelTileType.FloorTile)
                    continue;

                // ReSharper disable once PossibleLossOfFraction
                var position = startPosition + math.float3(i % columnsNumber, i / columnsNumber, 0);
                var coordinate = levelTiles.ToTileCoordinate(new fix2((fix) position.x, (fix) position.y));

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
                .Select(p => LevelTiles.ToTileCoordinate(p.HeroEntity.Get<TransformComponent>().WorldPosition))
                .ToArray();

            var floorTiles = LevelTiles
                .GetTilesByType(LevelTileType.FloorTile)
                .Where(t =>
                {
                    var coordinate = LevelTiles.ToTileCoordinate(t.Get<TransformComponent>().WorldPosition);
                    return !playersCoordinates.Contains(coordinate);
                })
                .ToList();

            Assert.IsTrue(enemyConfigs.Length <= floorTiles.Count, "enemies to spawn count greater than the floor tiles count");

            foreach (var (enemyConfigIndex, enemyConfig) in enemyConfigs.Select((c, i) => (i, c)))
            {
                var index = RandomGenerator.Range(0, floorTiles.Count, levelStageConfig.Index, enemyConfigIndex);
                var floorTile = floorTiles[index];
                var levelTileComponent = floorTile.Get<TransformComponent>();

                var task = CreateAndSpawnEnemy(enemyConfig, levelTileComponent.WorldPosition);
                var enemy = await task;

                AddEnemy(enemy);

                floorTiles.RemoveAt(index);
            }
        }

        private async Task<EcsEntity> CreateAndSpawnEnemy(EnemyConfig enemyConfig, fix2 position)
        {
            var task = _gameFactory.InstantiatePrefabAsync(enemyConfig.Prefab, fix2.ToXY(position));

            var entity = _ecsWorld.NewEntity();

            entity.Replace(new EnemyTag());

            entity.Replace(new TransformComponent
            {
                WorldPosition = position,
                Direction = enemyConfig.StartDirection,
                IsStatic = false
            });

            entity.Replace(new MovementComponent
            {
                Speed = fix.zero
            });

            entity.Replace(new HealthComponent
            {
                CurrentHealth = enemyConfig.HealthParameters.Health,
                MaxHealth = enemyConfig.HealthParameters.Health
            });

            entity.Replace(new DamageableComponent
            {
                HurtRadius = (fix) enemyConfig.DamageParameters.HurtRadius
            });

            entity.Replace(new LayerMaskComponent
            {
                Value = enemyConfig.LayerMask
            });

            entity.AddCollider(enemyConfig.Collider);
            entity.AddBehaviourComponents(enemyConfig, enemyConfig.BehaviourConfigs);

            var go = await task;
            Assert.IsNotNull(go);

            var enemyController = go.GetComponent<EnemyController>();
            Assert.IsNotNull(enemyController);

            entity.Replace(new EntityComponent
            {
                Config = enemyConfig,
                Controller = enemyController,

                InitialSpeed = (fix) enemyConfig.MovementParameters.Speed,
                SpeedMultiplier = fix.one
            });

            return entity;
        }

        private void CreatePlayersAndSpawnHeroesPvP(GameModePvPConfig gameMode, LevelStagePvPConfig levelStageConfig,
            IInputService inputService)
        {
            var playerConfigs = gameMode.PlayerConfigs;
            var spawnCorners = levelStageConfig.PlayersSpawnCorners;
            Assert.IsTrue(playerConfigs.Length <= spawnCorners.Length, "players count greater than the level spawn corners");

            var tasks = spawnCorners
                .Zip(playerConfigs,
                    (spawnCorner, playerConfig) => (LevelTiles.GetCornerWorldPosition(spawnCorner), playerConfig))
                .Select(p => CreatePlayerAndSpawnHero(inputService, p.playerConfig, p.Item1))
                .ToArray();

            Task.WhenAll(tasks);
        }

        private async Task CreatePlayersAndSpawnHeroesPvE(GameModePvEConfig gameMode,
            LevelStagePvEConfig levelStageConfig, IInputService inputService)
        {
            var spawnCoordinate = LevelTiles.GetCornerWorldPosition(levelStageConfig.PlayerSpawnCorner);
            await CreatePlayerAndSpawnHero(inputService, gameMode.PlayerConfig, spawnCoordinate);
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

            entity.Replace(new HeroTag());
            entity.Replace(new PositionControlledByResolverTag());

            entity.Replace(new TransformComponent
            {
                WorldPosition = position,
                Direction = heroConfig.StartDirection,
                IsStatic = false
            });

            entity.Replace(new MovementComponent
            {
                Speed = fix.zero
            });

            entity.Replace(new HealthComponent
            {
                CurrentHealth = heroConfig.HealthParameters.Health,
                MaxHealth = heroConfig.HealthParameters.Health
            });
            // RegisterProgressReader(hero.HeroHealth); :TODO:

            entity.Replace(new DamageableComponent
            {
                HurtRadius = (fix) heroConfig.DamageParameters.HurtRadius
            });

            entity.Replace(new LayerMaskComponent
            {
                Value = heroConfig.LayerMask
            });

            entity.AddCollider(heroConfig.Collider);

            var go = await task;
            Assert.IsNotNull(go);

            var heroController = go.GetComponent<HeroController>();
            Assert.IsNotNull(heroController);

            entity.Replace(new EntityComponent
            {
                Config = heroConfig,
                Controller = heroController,

                InitialSpeed = (fix) heroConfig.MovementParameters.Speed,
                SpeedMultiplier = fix.one
            });

            player.AttachHero(entity);
            AddPlayer(playerConfig.PlayerTagConfig, player);

            return entity;
        }

        private async Task<EcsEntity> CreateAndSpawnBomb(BombConfig bombConfig, fix2 position)
        {
            var task = _gameFactory.InstantiatePrefabAsync(bombConfig.Prefab, fix2.ToXY(position));

            var entity = _ecsWorld.NewEntity();

            entity.Replace(new BombTag());
            entity.Replace(new IsKinematicTag());

            entity.Replace(new TransformComponent
            {
                WorldPosition = position,
                Direction = bombConfig.StartDirection,
                IsStatic = true
            });

            entity.AddCollider(bombConfig.Collider);

            entity.Replace(new DamageableComponent
            {
                HurtRadius = (fix) bombConfig.DamageParameters.HurtRadius
            });

            entity.Replace(new LayerMaskComponent
            {
                Value = bombConfig.LayerMask
            });

            var go = await task;
            Assert.IsNotNull(go);

            var itemController = go.GetComponent<ItemController>();
            Assert.IsNotNull(itemController);

            entity.Replace(new EntityComponent
            {
                Config = bombConfig,
                Controller = itemController,

                InitialSpeed = fix.zero,
                SpeedMultiplier = fix.zero
            });

            return entity;
        }
    }
}
