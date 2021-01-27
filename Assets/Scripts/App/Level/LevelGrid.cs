using System.Linq;
using Configs.Level;
using Unity.Mathematics;
using UnityEngine;
using Random = System.Random;

namespace App.Level
{
    public sealed class LevelGrid
    {
        private readonly LevelConfig _levelConfig;

        public int ColumnsNumber => _levelConfig.ColumnsNumber;
        public int RowsNumber => _levelConfig.RowsNumber;

        public int this[int2 position] => _grid[position.x + position.y * ColumnsNumber];
        public int this[int index] => _grid[index];

        private readonly int[] _grid;

        public LevelGrid(LevelConfig levelConfig)
        {
            _levelConfig = levelConfig;

            _grid = Enumerable.Repeat(0, RowsNumber * ColumnsNumber).ToArray();

            Generate();
        }

        private void Generate()
        {
            var spawnCells = _levelConfig.PlayersSpawnCells;
            var playersNumber = spawnCells.Length;

            var playersReservedCellsNumber = playersNumber * 3 * 0; // Players are spawned in the grid corners

            var totalCellsNumber = _levelConfig.ColumnsNumber * _levelConfig.RowsNumber;
            var hardBlocksNumber = (_levelConfig.ColumnsNumber - 1) * (_levelConfig.RowsNumber - 1) / 4;
            var emptyCellsNumber = totalCellsNumber - hardBlocksNumber - playersReservedCellsNumber;

            var softBlocksNumber = emptyCellsNumber * _levelConfig.SoftBlocksCoverage / 100;

            var rg = new Random();

            /*int[] reps = {emptyCellsNumber, hardBlocksNumber};

            _grid = Enumerable.Range(0, 2)
                .SelectMany(i => Enumerable.Repeat(i, reps[i]))
                .OrderBy(t => rg.Next()).ToArray();*/

            for (var rowIndex = 0; rowIndex < RowsNumber; ++rowIndex)
            {
                for (var columnIndex = 0; columnIndex < ColumnsNumber; ++columnIndex)
                {
                    var index = rowIndex * ColumnsNumber + columnIndex;

                    if (columnIndex % 2 == 1 && rowIndex % 2 == 1) {
                        _grid[index] = 1;
                    }

                    else {
                        _grid[index] = rg.Next(0, 3) == 2 ? 2 : 0;
                    }
                }
            }
        }
    }
}
