using System;
using System.Collections.Generic;

namespace App.Level
{
    public sealed class Level
    {
        public int[,] Grid { get; }

        public Level(int cellsNumberInRow, int cellsNumberInColumn)
        {
            Grid = new int[cellsNumberInRow, cellsNumberInColumn];
        }

        public void Populate(IEnumerable<int> ids)
        {
            var positions = new List<(int, int)>();

            var cellsNumberInRow = Grid.GetLength(0);
            var cellsNumberInColumn = Grid.GetLength(1);

            for (var i = 0; i < cellsNumberInRow; i++)
            for (var j = 0; j < cellsNumberInColumn; j++)
                positions.Add((i, j));

            var rnd = new Random();

            foreach (var id in ids)
            {
                var index = rnd.Next(positions.Count);
                var (x, y) = positions[index];

                Grid[x, y] = id;
                positions.RemoveAt(index);
            }
        }
    }
}
