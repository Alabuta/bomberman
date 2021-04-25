using System;
using System.Collections.Generic;
using System.Linq;
using Configs.Game;
using Configs.Level;
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

        void GenerateLevel(GameModePvE applicationConfigGameModePvE, LevelConfig levelConfig);
    }

    public class GameLevelManager : ILevelManager
    {
        public event Action<IEntity> EntitySpawnedEvent;

        private int[] _hiddenItemsIndices;

        private IPlayer[] _players;

        public GameLevelState CurrentGameLevelState { get; }

        private LevelStageConfig _levelStageConfig;
        private GameLevelGridModel _gameLevelGridModel;

        public void GenerateLevel(GameModePvE gameModePvE, LevelConfig levelConfig)
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

            SpawnPlayers(gameModePvE, _levelStageConfig, _gameLevelGridModel);

            /*PrefabsManager.Instantiate();

            EnemiesManager.PopulateLevel(levelConfig, levelMode);
            PlayersManager.PopulateLevel(levelConfig, levelMode);*/
        }

        private void SpawnPlayers(GameModePvE gameModePvE, LevelStageConfig levelStageConfig, GameLevelGridModel levelGridModel)
        {
            _players = levelStageConfig.PlayersSpawnCorners
                .Select(spawnCorner =>
                {
                    var position = ((spawnCorner * 2 - 1) * (float2) (levelGridModel.Size - 1) / 2.0f).xyy * math.float3(1, 1, 0);

                    var playerGameObject = Object.Instantiate(gameModePvE.BombermanConfig.Prefab, position, Quaternion.identity);
                    var playerController = playerGameObject.GetComponent<PlayerController>();
                    Assert.IsNotNull(playerController, "'PlayerController' component is null");

                    playerController.BombPlantedEvent += BombPlantEventHandler;

                    EntitySpawnedEvent?.Invoke(playerController);

                    return (IPlayer) playerController;
                })
                .ToArray();
        }

        private void BombPlantEventHandler(object sender, BombPlantEventData data)
        {
            if (!(sender is IPlayer player))
                return;

            var playerController = (PlayerController) player;
            Debug.LogWarning(
                $"BombCapacity {player.BombCapacity} blastRadius {data.BlastRadius}, bombCoordinate {data.WorldPosition}"
            );

            // var cell = math.floor((data.WorldPosition / _gameLevelGridModel.Size + 1) / 2.0f * _gameLevelGridModel.Size);
            var cell = math.round(data.WorldPosition / _gameLevelGridModel.Size) * _gameLevelGridModel.Size;
            Debug.LogWarning(
                $"{data.WorldPosition / _gameLevelGridModel.Size} {(data.WorldPosition / _gameLevelGridModel.Size + 1) / 2.0f} {math.floor((data.WorldPosition / _gameLevelGridModel.Size + 1) / 2.0f)} cell {cell}"
            );

            var position = math.float3(cell.xy, 0);
            var prefab = _levelStageConfig.BombConfig.Prefab;
            Object.Instantiate(prefab, position, Quaternion.identity);
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
                {GridTileType.HardBlock, (hardBlocksGroup, levelConfig.HardBlock.Prefab)},
                {GridTileType.SoftBlock, (softBlocksGroup, levelConfig.SoftBlock.Prefab)}
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
    }
}
