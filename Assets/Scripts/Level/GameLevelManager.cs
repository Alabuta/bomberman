using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Configs;
using Configs.Game;
using Configs.Level;
using Core;
using Data;
using Entity;
using Entity.Hero;
using Game;
using Infrastructure.Factory;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Level
{
    public interface ILevelManager
    {
        event Action<IEntity> EntitySpawnedEvent;
        GameLevelGridModel LevelGridModel { get; }

        void GenerateLevelStage(GameModeBaseConfig gameMode, LevelStage levelConfig,
            IGameFactory gameFactory);

        void AddPlayer(PlayerConfig playerConfig, IPlayer player);
    }

    public class GameLevelManager : ILevelManager
    {
        public event Action<IEntity> EntitySpawnedEvent;


        public GameLevelState CurrentGameLevelState { get; }

        private LevelStageConfig _levelStageConfig;
        public GameLevelGridModel LevelGridModel { get; private set; }

        private int[] _hiddenItemsIndices;

        private IHero[] _heroes;

        private readonly Dictionary<PlayerConfig, IPlayer> _players = new();

        public void GenerateLevelStage(GameModeBaseConfig gameMode, LevelStage levelStage, IGameFactory gameFactory)
        {
            var levelConfig = gameMode.LevelConfigs[levelStage.LevelIndex];
            _levelStageConfig = levelConfig.LevelStages.First();

            LevelGridModel = new GameLevelGridModel(_levelStageConfig);

            _hiddenItemsIndices = LevelGridModel
                .Select((_, i) => i)
                .Where(i => (LevelGridModel[i] & GridTileType.PowerUpItem) != 0)
                .ToArray();

            SpawnGameObjects(levelConfig, LevelGridModel);

            SetupWalls(levelConfig, LevelGridModel);

            var heroes = SpawnHeroesPrefabs(gameMode.PlayerConfigs, _levelStageConfig.PlayersSpawnCorners, LevelGridModel,
                gameFactory);
            _heroes = heroes
                .Select(go => go.GetComponent<HeroController>())
                .Select(c =>
                {
                    c.BombPlantedEvent += BombPlantEventHandler;
                    EntitySpawnedEvent?.Invoke(c);

                    return (IHero) c;
                })
                .ToArray();

            /*PrefabsManager.Instantiate();

            EnemiesManager.PopulateLevel(levelConfig, levelMode);
            PlayersManager.PopulateLevel(levelConfig, levelMode);*/
        }

        public void AddPlayer(PlayerConfig playerConfig, IPlayer player)
        {
            _players.Add(playerConfig, player);// :TODO: refactor
        }

        private static IEnumerable<GameObject> SpawnHeroesPrefabs(
            IReadOnlyCollection<PlayerConfig> playerConfigs,
            IReadOnlyCollection<int2> spawnCorners,
            GameLevelGridModel levelGridModel,
            IGameFactory gameFactory)
        {
            Assert.IsTrue(playerConfigs.Count <= spawnCorners.Count, "players count greater than the level spawn corners");

            return spawnCorners
                .Zip(playerConfigs, (spawnCorner, playerConfig) =>
                {
                    var position = levelGridModel.GetCornerWorldPosition(spawnCorner);
                    return gameFactory.SpawnEntity(playerConfig.HeroConfig, position);
                })
                .ToArray();
        }

        private void BombPlantEventHandler(object sender, BombPlantEventData data)
        {
            Assert.IsTrue(sender is IHero, $"expected {typeof(IHero)} sender type instead of {sender.GetType()}");

            var position = math.float3(math.round(data.WorldPosition).xy, 0);
            var prefab = _levelStageConfig.BombConfig.Prefab;
            var bomb = Object.Instantiate(prefab, position, Quaternion.identity);

            StartCoroutine.Start(ExecuteAfterTime(_levelStageConfig.BombConfig.LifetimeSec, () => { bomb.SetActive(false); }));
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
                (math.float2(+columnsNumber / 2.0f + 1, 0), math.float2(2, rowsNumber)),
                (math.float2(-columnsNumber / 2.0f - 1, 0), math.float2(2, rowsNumber)),
                (math.float2(0, +rowsNumber / 2.0f + 1), math.float2(columnsNumber, 2)),
                (math.float2(0, -rowsNumber / 2.0f - 1), math.float2(columnsNumber, 2))
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

            for (var index = 0; index < columnsNumber * rowsNumber; ++index)
            {
                var blockType = gameLevelGridModel[index];

                if (blockType == GridTileType.FloorTile)
                    continue;

                // ReSharper disable once PossibleLossOfFraction
                var position = startPosition + math.float3(index % columnsNumber, index / columnsNumber, 0);

                var (parent, prefab) = blocks[blockType & ~GridTileType.PowerUpItem];
                Object.Instantiate(prefab, position, Quaternion.identity, parent.transform);
            }
        }


        private static IEnumerator ExecuteAfterTime(float time, Action callback)
        {
            yield return new WaitForSeconds(time);

            callback?.Invoke();
        }
    }
}
