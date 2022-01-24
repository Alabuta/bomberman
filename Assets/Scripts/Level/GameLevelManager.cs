using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Configs;
using Configs.Game;
using Configs.Level;
using Core;
using Entity;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Level
{
    public interface ILevelManager
    {
        event Action<IEntity> EntitySpawnedEvent;

        void GenerateLevel(GameModePvEConfig applicationConfigGameModePvE, LevelConfig levelConfig);
    }

    public class GameLevelManager : ILevelManager
    {
        public event Action<IEntity> EntitySpawnedEvent;

        private int[] _hiddenItemsIndices;

        private IHero[] _players;

        public GameLevelState CurrentGameLevelState { get; }

        private LevelStageConfig _levelStageConfig;
        private GameLevelGridModel _gameLevelGridModel;

        public void GenerateLevel(GameModePvEConfig gameModePvE, LevelConfig levelConfig)
        {
            // Assert.IsTrue(ApplicationHolder.Instance.TryGet<ISceneManager>(out var sceneManager));

            _levelStageConfig = levelConfig.LevelStages.First();

            _gameLevelGridModel = new GameLevelGridModel(_levelStageConfig);

            _hiddenItemsIndices = _gameLevelGridModel
                .Select((_, i) => i)
                .Where(i => (_gameLevelGridModel[i] & GridTileType.PowerUpItem) != 0)
                .ToArray();

            SetupCamera(levelConfig, _levelStageConfig, _gameLevelGridModel);

            SpawnGameObjects(levelConfig, _gameLevelGridModel);

            SetupWalls(levelConfig, _gameLevelGridModel);

            SpawnPlayers(gameModePvE.Players, _levelStageConfig, _gameLevelGridModel);

            /*PrefabsManager.Instantiate();

            EnemiesManager.PopulateLevel(levelConfig, levelMode);
            PlayersManager.PopulateLevel(levelConfig, levelMode);*/
        }

        private void SpawnPlayers(IReadOnlyCollection<PlayerConfig> playerConfigs, LevelStageConfig levelStageConfig,
            GameLevelGridModel levelGridModel)
        {
            var spawnCorners = levelStageConfig.PlayersSpawnCorners;

            Assert.IsTrue(playerConfigs.Count <= spawnCorners.Length, "players count greater than spawn corners");

            _players = spawnCorners.Zip(playerConfigs, (spawnCorner, playerConfig) =>
                {
                    var position = ((spawnCorner * 2 - 1) * (float2) (levelGridModel.Size - 1) / 2.0f).xyy *
                                   math.float3(1, 1, 0);

                    var playerGameObject = Object.Instantiate(playerConfig.HeroConfig.Prefab, position, Quaternion.identity);

                    var playerController = playerGameObject.GetComponent<HeroController>();
                    Assert.IsNotNull(playerController, "'PlayerController' component is null");

                    playerController.BombPlantedEvent += BombPlantEventHandler;

                    EntitySpawnedEvent?.Invoke(playerController);

                    return (IHero) playerController;
                })
                .ToArray();

            /*_players = spawnCorners
                .Select(spawnCorner =>
                {
                    var position = ((spawnCorner * 2 - 1) * (float2) (levelGridModel.Size - 1) / 2.0f).xyy *
                                   math.float3(1, 1, 0);

                    var playerGameObject =
                        Object.Instantiate(playerConfigs.BombermanConfig.Prefab, position, Quaternion.identity);
                    var playerController = playerGameObject.GetComponent<HeroController>();
                    Assert.IsNotNull(playerController, "'PlayerController' component is null");

                    playerController.BombPlantedEvent += BombPlantEventHandler;

                    EntitySpawnedEvent?.Invoke(playerController);

                    return (IHero) playerController;
                })
                .ToArray();*/
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
                { GridTileType.HardBlock, (hardBlocksGroup, levelConfig.HardBlock.Prefab) },
                { GridTileType.SoftBlock, (softBlocksGroup, levelConfig.SoftBlock.Prefab) }
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

        private static void SetupCamera(LevelConfig levelConfig, LevelStageConfig levelStageConfig,
            GameLevelGridModel gameLevelGridModel)
        {
            var mainCamera = Camera.main;
            if (!mainCamera)
                return;

            var cameraRect = math.float2(Screen.width * 2.0f / Screen.height, 1) * mainCamera.orthographicSize;

            var fieldRect = (gameLevelGridModel.Size - cameraRect) / 2.0f;
            var fieldMargins = (float4) levelConfig.ViewportPadding / levelConfig.OriginalPixelsPerUnits;

            var firstPlayerCorner = levelStageConfig.PlayersSpawnCorners.FirstOrDefault();

            var camePosition = (firstPlayerCorner - (float2) 0.5f) * gameLevelGridModel.Size;
            camePosition = math.clamp(camePosition, fieldMargins.xy - fieldRect, fieldRect + fieldMargins.zw);

            mainCamera.transform.position = math.float3(camePosition, -1);
        }

        private static IEnumerator ExecuteAfterTime(float time, Action callback)
        {
            yield return new WaitForSeconds(time);

            callback?.Invoke();
        }
    }
}
