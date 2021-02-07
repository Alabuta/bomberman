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
                var orthographicSize = Camera.main.orthographicSize;
                var cameraSize = math.float2(
                    orthographicSize * Screen.width / Screen.height,
                    orthographicSize
                );

                // _cameraBounds.xz = (LevelGrid.Size.x + LevelConfig.WallsSize) / 2.0f;
                _cameraBounds.xz = math.float2(-1, 1) * (LevelGrid.Size.x + LevelConfig.WallsSize) / 2.0f;

                var levelGridSize = math.float2(LevelGrid.Size) / 2.0f;

                var firstPlayerCorner = LevelConfig.PlayersSpawnCorners.FirstOrDefault();
                var direction = math.select(1, -1, firstPlayerCorner == int2.zero);

                var cameraPosition = math.float3(LevelGrid.Size + LevelConfig.WallsSize, 0);

                var cameraTransform = Camera.main.transform;
                // cameraTransform.position = (cameraPosition + LevelConfig.CameraPositionOffset) * math.float3(direction, 1);

                Debug.LogWarning($"{cameraSize} {levelGridSize} {cameraPosition}");
            }

            var hardBlocksGroup = new GameObject("HardBlocks");
            var softBlocksGroup = new GameObject("SoftBlocks");

            // :TODO: refactor
            var blocks = new Dictionary<GridTileType, (Transform, GameObject)>
            {
                {GridTileType.HardBlock, (hardBlocksGroup.transform, LevelConfig.HardBlock.Prefab)},
                {GridTileType.SoftBlock, (softBlocksGroup.transform, LevelConfig.SoftBlock.Prefab)}
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
                Instantiate(prefab, position, Quaternion.identity, parent);
            }

            var walls = Instantiate(LevelConfig.Walls, Vector3.zero, Quaternion.identity);
            var sprite = walls.GetComponent<SpriteRenderer>();
            sprite.size += new Vector2(columnsNumber, rowsNumber);
        }
    }
}
