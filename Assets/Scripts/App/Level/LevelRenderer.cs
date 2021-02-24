using System.Collections.Generic;
using System.Linq;
using Configs.Level;
using Unity.Mathematics;
using UnityEngine;

namespace App.Level
{
    public class LevelRenderer : MonoBehaviour
    {
        [SerializeField]
        private LevelConfig LevelConfig;

        private LevelGridModel LevelGridModel { get; set; }

        public void Start2()
        {
            LevelGridModel = new LevelGridModel(LevelConfig);

            var columnsNumber = LevelGridModel.ColumnsNumber;
            var rowsNumber = LevelGridModel.RowsNumber;

            var mainCamera = Camera.main;
            if (mainCamera)
            {
                var cameraRect = math.float2(Screen.width * 2.0f / Screen.height, 1) * mainCamera.orthographicSize;

                var fieldRect = (LevelGridModel.Size - cameraRect) / 2.0f;
                var fieldMargins = (float4) LevelConfig.ViewportPadding / LevelConfig.OriginalPixelsPerUnits;

                var firstPlayerCorner = LevelConfig.PlayersSpawnCorners.FirstOrDefault();

                var camePosition = (firstPlayerCorner - (float2) 0.5f) * LevelGridModel.Size;
                camePosition = math.clamp(camePosition, fieldMargins.xy - fieldRect, fieldRect + fieldMargins.zw);

                mainCamera.transform.position = math.float3(camePosition, -1);
            }

            var hardBlocksGroup = new GameObject("HardBlocks");
            var softBlocksGroup = new GameObject("SoftBlocks");

            // :TODO: refactor
            var blocks = new Dictionary<GridTileType, (GameObject, GameObject)>
            {
                {GridTileType.HardBlock, (hardBlocksGroup, LevelConfig.HardBlock.Prefab)},
                {GridTileType.SoftBlock, (softBlocksGroup, LevelConfig.SoftBlock.Prefab)}
            };

            var startPosition = (math.float3(1) - math.float3(columnsNumber, rowsNumber, 0)) / 2;

            for (var index = 0; index < columnsNumber * rowsNumber; ++index)
            {
                var blockType = LevelGridModel[index];

                if (blockType == GridTileType.FloorTile)
                    continue;

                // ReSharper disable once PossibleLossOfFraction
                var position = startPosition + math.float3(index % columnsNumber, index / columnsNumber, 0);

                var (parent, prefab) = blocks[blockType];
                Instantiate(prefab, position, Quaternion.identity, parent.transform);
            }

            var walls = Instantiate(LevelConfig.Walls, Vector3.zero, Quaternion.identity);
            var sprite = walls.GetComponent<SpriteRenderer>();
            sprite.size += new Vector2(columnsNumber, rowsNumber);
        }
    }
}
