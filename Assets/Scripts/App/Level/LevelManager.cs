using System.Collections.Generic;
using System.Linq;
using Configs.Level;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace App.Level
{
    public interface ILevelManager
    {
        void GenerateLevel(LevelConfig levelConfig);
    }

    public class LevelManager : ILevelManager
    {
        private LevelGridModel _levelGridModel;

        public LevelState CurrentLevelState { get; }


        public void GenerateLevel(LevelConfig levelConfig)
        {
            /*var levelMode = */GenerateLevelModel(levelConfig);

            InstantiateGameObjects(levelConfig, _levelGridModel);
            SetupCamera(levelConfig, _levelGridModel);

            /*if (!(ApplicationHolder.Instance.Container.Get<ISomeInterface>(out var someManager)))
                throw System.NotSupportedException;

            PrefabsManager.Instantiate();

            EnemiesManager.PopulateLevel(levelConfig, levelMode);
            PlayersManager.PopulateLevel(levelConfig, levelMode);

            someManager.Xdsdasd*/
        }

        private void GenerateLevelModel(LevelConfig levelConfig)
        {
            _levelGridModel = new LevelGridModel(levelConfig);
        }

        private void InstantiateGameObjects(LevelConfig levelConfig, LevelGridModel levelGridModel)
        {
            var scene = SceneManager.GetActiveScene();

            var rootGameObjects = scene.GetRootGameObjects();
            // Assert.IsTrue(rootGameObjects.Any(), "Scene doesn't have game objects");

            var sceneRoot = rootGameObjects.First().transform.parent;

            var columnsNumber = levelConfig.ColumnsNumber;
            var rowsNumber = levelConfig.RowsNumber;

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
                var blockType = levelGridModel[index];

                if (blockType == GridTileType.FloorTile)
                    continue;

                // ReSharper disable once PossibleLossOfFraction
                var position = startPosition + math.float3(index % columnsNumber, index / columnsNumber, 0);

                var (parent, prefab) = blocks[blockType];
                Object.Instantiate(prefab, position, Quaternion.identity, parent.transform);
            }

            var walls = Object.Instantiate(levelConfig.Walls, Vector3.zero, Quaternion.identity);
            var sprite = walls.GetComponent<SpriteRenderer>();
            sprite.size += new Vector2(columnsNumber, rowsNumber);
        }

        private void SetupCamera(LevelConfig levelConfig, LevelGridModel levelGridModel)
        {
            var mainCamera = Camera.main;
            if (mainCamera)
            {
                var cameraRect = math.float2(Screen.width * 2.0f / Screen.height, 1) * mainCamera.orthographicSize;

                var fieldRect = (levelGridModel.Size - cameraRect) / 2.0f;
                var fieldMargins = (float4) levelConfig.ViewportPadding / levelConfig.OriginalPixelsPerUnits;

                var firstPlayerCorner = levelConfig.PlayersSpawnCorners.FirstOrDefault();

                var camePosition = (firstPlayerCorner - (float2) 0.5f) * levelGridModel.Size;
                camePosition = math.clamp(camePosition, fieldMargins.xy - fieldRect, fieldRect + fieldMargins.zw);

                mainCamera.transform.position = math.float3(camePosition, -1);
            }
        }
    }
}
