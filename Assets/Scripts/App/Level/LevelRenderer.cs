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

        private LevelGrid LevelGrid { get; set; }

        public void Start()
        {
            LevelGrid = new LevelGrid(LevelConfig);

            var columnsNumber = LevelGrid.ColumnsNumber;
            var rowsNumber = LevelGrid.RowsNumber;

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
