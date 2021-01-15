using UnityEngine;

namespace Configs.Levels
{
    [CreateAssetMenu(fileName = "LevelRuinsConfig", menuName = "Configs/Level Configs/Level Ruins Config", order = 2)]
    public class LevelRuins : ScriptableObject
    {
        [Header("General Configs")]
        public float Width;
        public float Height;

        public int CellsNumberInRows;
        public int CellsNumberInColumns;

        [Header("Cells' Assets Prefabs")]
        public GameObject WallCellPrefab;
        public GameObject FloorCellPrefab;
        public GameObject ConcreteWallCellPrefab;

        public GameObject LevelBackgroundPrefab;
    }
}
