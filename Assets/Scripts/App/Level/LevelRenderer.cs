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

            if (Camera.main)
            {
                var cameraRect = math.float2((float) Screen.width / Screen.height, 1) * Camera.main.orthographicSize;
                cameraRect += LevelConfig.CameraViewportPadding;

                var firstPlayerCorner = LevelConfig.PlayersSpawnCorners.FirstOrDefault();
                var offsetDirection = math.select(1, -1, firstPlayerCorner == int2.zero);

                var cameraPosition = math.max(math.float2(LevelGrid.Size) / 2.0f - cameraRect, 0) * offsetDirection;

                var cameraTransform = Camera.main.transform;
                cameraTransform.position = math.float3(cameraPosition, -1);

                Debug.LogWarning($"cameraRect {cameraRect} cameraPosition {cameraPosition} offsetDirection {offsetDirection}");
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

            var walls = Instantiate(LevelConfig.Walls, Vector3.zero, Quaternion.identity);
            var sprite = walls.GetComponent<SpriteRenderer>();
            sprite.size += new Vector2(columnsNumber, rowsNumber);
        }
    }
}
