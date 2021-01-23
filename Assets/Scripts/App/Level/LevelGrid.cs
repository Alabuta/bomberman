namespace App.Level
{
    public sealed class LevelGrid
    {
        public int CellsNumberInRows => Grid.GetLength(0);
        public int CellsNumberInColumns => Grid.GetLength(1);

        public readonly int[,] Grid;

        public LevelGrid(int cellsNumberInRows, int cellsNumberInColumns)
        {
            Grid = new int[cellsNumberInRows, cellsNumberInColumns];
        }
    }
}
