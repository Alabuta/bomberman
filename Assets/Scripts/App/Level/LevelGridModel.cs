using System;
using System.Diagnostics;
using System.Linq;
using Configs.Level;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace App.Level
{
    [Flags]
    public enum GridTileType
    {
        FloorTile = 0,
        HardBlock = 1,
        SoftBlock = 2,
        PowerUpItem = 4
    }

    public sealed class LevelGridModel
    {
        [NotNull]
        private readonly GridTileType[] _grid;

        private readonly int2 _size;

        public int2 Size => _size;

        public int ColumnsNumber => _size.x;
        public int RowsNumber => _size.y;

        public GridTileType this[int index] => _grid[index];
        public GridTileType this[int2 coordinate] => _grid[GetFlattenCellCoordinate(coordinate)];

        public LevelGridModel(LevelStageConfig levelStageConfig)
        {
            _size = math.int2(levelStageConfig.ColumnsNumber, levelStageConfig.RowsNumber);

            var playersSpawnCorners = levelStageConfig.PlayersSpawnCorners;
            var softBlocksCoverage = levelStageConfig.SoftBlocksCoverage;

            var reservedCellsIndices = playersSpawnCorners
                .SelectMany(corner =>
                {
                    var coordinate = corner * (_size - 1);
                    var offset = math.select(-1, 1, corner == int2.zero);

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

            var powerUpItems = levelStageConfig.PowerUpItems.ToList(); // 3
            var softBlocksPerPowerUpItem = softBlocksNumber / powerUpItems.Count; // 5

            var cellTypeNumbers = math.int2(floorCellsNumber, softBlocksNumber);

            var items = Enumerable
                .Range(0, powerUpItems.Count)
                .Select(i => Random.Range(i * softBlocksPerPowerUpItem, (i + 1) * softBlocksPerPowerUpItem))
                .ToList();

            Debug.LogWarning($"count {items.Count}");

            _grid = Enumerable
                .Range(0, totalCellsNumber + reservedCellsNumber)
                .Select(i =>
                {
                    if (reservedCellsIndices.Contains(i))
                        return GridTileType.FloorTile;

                    var coordinate = math.int2(i % _size.x, i / _size.x);
                    if (math.all(coordinate % 2 == 1))
                        return GridTileType.HardBlock;

                    var range = (int2) (cellTypeNumbers == int2.zero);

                    var softBlockOdds = (int) (cellTypeNumbers.y * 100.0f / (cellTypeNumbers.x + cellTypeNumbers.y));

                    var typeIndex = Convert.ToInt32(Random.Range(0, 100) < softBlockOdds);
                    typeIndex = math.clamp(typeIndex, range.x, 2 - range.y);

                    var tileType = typeIndex == 0 ? GridTileType.FloorTile : GridTileType.SoftBlock;

                    if (tileType == GridTileType.SoftBlock && items.Contains(i))
                        Debug.LogWarning($"contains {i}");

                    if (tileType == GridTileType.SoftBlock && items.Contains(i))
                        tileType |= GridTileType.PowerUpItem;

                    --cellTypeNumbers[typeIndex];

                    return tileType;
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
