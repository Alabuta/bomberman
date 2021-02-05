using System.Collections.Generic;
using Configs.Level;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace App.Level
{
    public class LevelRenderer : MonoBehaviour
    {
        [SerializeField]
        private LevelConfig LevelConfig;

        public LevelGrid LevelGrid { get; private set; }

        public void Start()
        {
            var columnsNumber = LevelConfig.ColumnsNumber;
            var rowsNumber = LevelConfig.RowsNumber;

            LevelGrid = new LevelGrid(LevelConfig);

            var hardBlocksGroup = GameObject.Find("HardBlocks");
            var softBlocksGroup = GameObject.Find("SoftBlocks");

            // :TODO: refactor
            var blocks = new Dictionary<GridTileType, (Transform, GameObject)>
            {
                {GridTileType.HardBlock, (hardBlocksGroup.transform, LevelConfig.HardBlock.Prefab)},
                {GridTileType.SoftBlock, (softBlocksGroup.transform, LevelConfig.SoftBlock.Prefab)}
            };

            var startPosition = (Vector3.one - new Vector3(columnsNumber, rowsNumber)) / 2;

            for (var index = 0; index < columnsNumber * rowsNumber; ++index)
            {
                var blockType = LevelGrid[index];

                if (blockType == GridTileType.FloorTile)
                    continue;

                // ReSharper disable once PossibleLossOfFraction
                var position = startPosition + new Vector3(index % columnsNumber, index / columnsNumber);

                var (parent, prefab) = blocks[blockType];
                Instantiate(prefab, position, Quaternion.identity, parent);
            }

            var walls = Instantiate(LevelConfig.Walls, Vector3.zero, Quaternion.identity);
            var sprite = walls.GetComponent<SpriteRenderer>();
            sprite.size += new Vector2(columnsNumber, rowsNumber);
        }
    }
}
