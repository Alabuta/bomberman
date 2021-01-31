using System;
using System.Linq;
using Configs.Level;
using JetBrains.Annotations;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace App.Level
{
    public enum GridTileType
    {
        FloorTile,
        SoftBlock,
        HardBlock
    }

    public sealed class LevelGrid
    {
        [NotNull]
        private readonly GridTileType[] _grid;

        private readonly int2 _size;

        public int ColumnsNumber => _size.x;
        public int RowsNumber => _size.y;

        public GridTileType this[int index] => _grid[index];
        public GridTileType this[int2 coordinate] => _grid[GetFlattenCellCoordinate(coordinate)];

        public LevelGrid(LevelConfig levelConfig)
        {
            _size = math.int2(levelConfig.ColumnsNumber, levelConfig.RowsNumber);

            var playersSpawnCorners = levelConfig.PlayersSpawnCorners;
            var softBlocksCoverage = levelConfig.SoftBlocksCoverage;

            var reservedCellsIndices = playersSpawnCorners
                .SelectMany(corner =>
                {
                    var coordinate = corner * (_size - 1);
                    var offset = math.select(math.int2(-1), math.int2(1), corner == int2.zero);

                    return new[]
                    {
                        GetFlattenCellCoordinate(coordinate),
                        GetFlattenCellCoordinate(math.mad(math.int2(1, 0), offset, coordinate)),
                        GetFlattenCellCoordinate(math.mad(math.int2(0, 1), offset, coordinate))
                    };
                })
                .ToArray();

            var reservedCellsNumber = reservedCellsIndices.Length;

            var totalCellsNumber = _size.x * _size.y - reservedCellsNumber;

            var hardBlocksNumber = (_size.x - 1) * (_size.y - 1) / 4;
            var softBlocksNumber = (int) math.round((totalCellsNumber - hardBlocksNumber) * softBlocksCoverage / 100.0f);
            var floorCellsNumber = totalCellsNumber - softBlocksNumber - hardBlocksNumber;

            var cellTypeNumbers = math.int2(floorCellsNumber, softBlocksNumber);

            _grid = Enumerable
                .Repeat(GridTileType.HardBlock, totalCellsNumber + reservedCellsNumber)
                .Select((type, i) =>
                {
                    if (reservedCellsIndices.Contains(i))
                        return GridTileType.FloorTile;

                    var coordinate = math.int2(i % _size.x, i / _size.x);
                    if (math.all(coordinate % 2 == 1))
                        return type;

                    var range = (int2) (cellTypeNumbers == int2.zero);

                    var softBlockOdds = (int) (cellTypeNumbers.y * 100.0f / (cellTypeNumbers.x + cellTypeNumbers.y));

                    var typeIndex = Convert.ToInt32(Random.Range(0, 100) < softBlockOdds);
                    typeIndex = math.clamp(typeIndex, range.x, 2 - range.y);

                    --cellTypeNumbers[typeIndex];

                    return typeIndex == 0 ? GridTileType.FloorTile : GridTileType.SoftBlock;
                })
                .ToArray();
        }

        private int GetFlattenCellCoordinate(int2 coordinate)
        {
            var c = math.select(int2.zero, _size, coordinate < int2.zero) + coordinate;
            return math.mad(c.y, _size.x, c.x);
        }
    }
}
