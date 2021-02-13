using System.Collections.Generic;
using System.Linq;
using Configs.Level;
using Unity.Mathematics;
using UnityEngine;

namespace App.Level
{
    public class LevelRenderer : MonoBehaviour
    {
        private float4 _cameraBounds;

        [SerializeField]
        private LevelConfig LevelConfig;

        private LevelGrid LevelGrid { get; set; }

        public void Start()
        {
            LevelGrid = new LevelGrid(LevelConfig);

            var columnsNumber = LevelGrid.ColumnsNumber;
            var rowsNumber = LevelGrid.RowsNumber;

            var walls = Instantiate(LevelConfig.Walls, Vector3.zero, Quaternion.identity);
            var sprite = walls.GetComponent<SpriteRenderer>();
            sprite.size += new Vector2(columnsNumber, rowsNumber);

            var mainCamera = Camera.main;
            if (mainCamera)
            {
                var cameraPlayerFocusArea = math.float2(0);

                var cameraRect = math.float2(Screen.width * 2.0f / Screen.height, 1) * mainCamera.orthographicSize;
                var ppu = mainCamera.pixelHeight / mainCamera.orthographicSize * 0.5f;
                var viewportPadding = (float4) LevelConfig.CameraViewportPadding / ppu;

                // cameraRect += math.float2(viewportPadding);

                var area = (LevelGrid.Size + math.float2(3, -2) - cameraRect) / 2.0f;

                var firstPlayerCorner = LevelConfig.PlayersSpawnCorners.FirstOrDefault();
                var position = (firstPlayerCorner - math.float2(0.5f)) * LevelGrid.Size;
                // var offsetDirection = math.select(1, -1, firstPlayerCorner == int2.zero);
                position = math.clamp(position, -area - math.float2(0, -1.625f), area - math.float2(0, 0.125f));

                /*var cameraPosition = math.max(math.float2(LevelGrid.Size) / 2.0f - cameraRect, 0) * offsetDirection;

                Debug.LogWarning(
                    $"ppu {ppu} cameraRect {cameraRect} pixelRect {math.float2(mainCamera.pixelWidth, mainCamera.pixelHeight)} viewportPadding {viewportPadding}");*/

                var cameraTransform = mainCamera.transform;
                cameraTransform.position = math.float3(position, -1);

                Debug.LogWarning(
                    $"ppu {ppu} cameraRect {cameraRect} position {position}");
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
                var blockType = LevelGrid[index];

                if (blockType == GridTileType.FloorTile)
                    continue;

                // ReSharper disable once PossibleLossOfFraction
                var position = startPosition + math.float3(index % columnsNumber, index / columnsNumber, 0);

                var (parent, prefab) = blocks[blockType];
                Instantiate(prefab, position, Quaternion.identity, parent.transform);
            }
        }
    }
}
