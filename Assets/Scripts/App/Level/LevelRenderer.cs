using System;
using System.Collections.Generic;
using System.Linq;
using Configs.Level;
using UnityEngine;
using Random = UnityEngine.Random;

namespace App.Level
{
    public class LevelRenderer : MonoBehaviour
    {
        public LevelConfig LevelConfig;

        public GameObject Tile;

        private int _vertical;
        private int _horizontal;

        public void Start()
        {
            if (Camera.main)
            {
                _vertical = (int)Camera.main.orthographicSize;
                _horizontal = _vertical * (Screen.width / Screen.height);
            }

            var level = new Level(10, 10);

            /*var rand = new System.Random();
        var ids = Enumerable.Range(0, 50).
            .Select(i => new Tuple<int, int>(rand.Next(50), i))
            .OrderBy(i => i.Item1)
            .Select(i => i.Item2);*/
            var ids = new[] {1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 0, 0, 0, 0, 3, 3, 3, 3};


            level.Populate(ids);

            for (var i = 0; i < level.Grid.GetLength(0); i ++) {
                for (var j = 0; j < level.Grid.GetLength(1); j++) {
                    if ( level.Grid[i, j] != 0 )
                    {
                        SpawnObject(i, j, level.Grid[i, j]);
                    }
                }
            }
        }

        private void SpawnObject(int x, int y, int id)
        {
            var tilePrefab = LevelConfig.BreakableTiles[0].Prefab;

            var tile = Instantiate(tilePrefab, new Vector3(x - _horizontal, y - _vertical), Quaternion.identity);
            tile.GetComponent<SpriteRenderer>().color = id == 1 ? Color.cyan : Color.yellow;
        }
    }
}
