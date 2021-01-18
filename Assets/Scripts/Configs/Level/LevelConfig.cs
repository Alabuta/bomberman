using Configs.Enemy;
using UnityEngine;

namespace Configs.Level
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Configs/Level/Level Config", order = 1)]
    public sealed class LevelConfig : ScriptableObject
    {
        [Header("General Configs")]
        public string Name;

        public int CellsNumberInRows;
        public int CellsNumberInColumns;

        [Header("Enemy Configs")]
        public EnemyConfig[] Enemies;

        [Header("Tile Configs")]
        public GameObject LevelBackgroundPrefab;

        public TileConfig FordableTile;
        public TileConfig UnfordableTile;

        public TileConfig PortalTile;

        public TileConfig[] LevelTiles;

        /*
         * [Header("PowerUp Configs")]
         * public PowerUpConfig[] PowerUps;
         *
         * [Header("Pit and Hazard Configs")]
         * ...
         */
    }
}
