using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Configs.Level;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace Level
{
    [Flags]
    public enum GridTileType
    {
        FloorTile = 0,
        HardBlock = 1,
        SoftBlock = 2,
        PowerUpItem = 4
    }

    public sealed class GameLevelGridModel : IEnumerable<GridTileType>
    {
        [NotNull]
        private readonly GridTileType[] _grid;

        private readonly int2 _size;

        public int2 Size => _size;
        public int2 WorldSize => _size;

        public int ColumnsNumber => _size.x;
        public int RowsNumber => _size.y;

        public GridTileType this[int index] => _grid[index];
        public GridTileType this[int2 coordinate] => _grid[GetFlattenCellCoordinate(coordinate)];

        public GameLevelGridModel(LevelStageConfig levelStageConfig)
        {
            _size = math.int2(levelStageConfig.ColumnsNumber, levelStageConfig.RowsNumber);
            Assert.IsTrue(math.all(_size % 2 != int2.zero));

            var playersSpawnCorners = levelStageConfig.PlayersSpawnCorners;
            var softBlocksCoverage = levelStageConfig.SoftBlocksCoverage;

            var reservedCellsIndices = new HashSet<int>(
                playersSpawnCorners
                    .SelectMany(
                        corner =>
                        {
                            var coordinate = corner * (_size - 1);
                            var offset = math.select(-1, 1, corner == int2.zero);

                            return new[]
                            {
                                GetFlattenCellCoordinate(coordinate),
                                GetFlattenCellCoordinate(math.mad(math.int2(1, 0), offset, coordinate)),
                                GetFlattenCellCoordinate(math.mad(math.int2(0, 1), offset, coordinate))
                            };
                        }
                    )
            );

            var reservedCellsNumber = reservedCellsIndices.Count;

            var totalCellsNumber = _size.x * _size.y - reservedCellsNumber;

            var hardBlocksNumber = (_size.x - 1) * (_size.y - 1) / 4;
            var softBlocksNumber = (int) math.round((totalCellsNumber - hardBlocksNumber) * softBlocksCoverage / 100.0f);
            var floorCellsNumber = totalCellsNumber - softBlocksNumber - hardBlocksNumber;

            var cellTypeNumbers = math.int2(floorCellsNumber, softBlocksNumber);

            var powerUpItems = levelStageConfig.PowerUpItems;
            var softBlocksPerPowerUpItem = softBlocksNumber / powerUpItems.Length;

            var powerItemsIndices = new HashSet<int>(
                Enumerable
                    .Range(0, powerUpItems.Length)
                    .Select(i => Random.Range(i * softBlocksPerPowerUpItem, (i + 1) * softBlocksPerPowerUpItem))
            );

            _grid = Enumerable
                .Range(0, totalCellsNumber + reservedCellsNumber)
                .Select(
                    index =>
                    {
                        if (reservedCellsIndices.Contains(index))
                            return GridTileType.FloorTile;

                        var coordinate = math.int2(index % _size.x, index / _size.x);
                        if (math.all(coordinate % 2 == 1))
                            return GridTileType.HardBlock;

                        var range = (int2) (cellTypeNumbers == int2.zero);

                        var softBlockOdds = (int) (cellTypeNumbers.y * 100.0f / (cellTypeNumbers.x + cellTypeNumbers.y));

                        var typeIndex = Convert.ToInt32(Random.Range(0, 100) < softBlockOdds);
                        typeIndex = math.clamp(typeIndex, range.x, 2 - range.y);

                        var tileType = typeIndex == 0 ? GridTileType.FloorTile : GridTileType.SoftBlock;

                        if (tileType == GridTileType.SoftBlock && powerItemsIndices.Contains(cellTypeNumbers[typeIndex]))
                            tileType |= GridTileType.PowerUpItem;

                        --cellTypeNumbers[typeIndex];

                        return tileType;
                    }
                )
                .ToArray();
        }

        private int GetFlattenCellCoordinate(int2 coordinate)
        {
            var c = math.select(int2.zero, _size, coordinate < int2.zero) + coordinate;
            return math.mad(c.y, _size.x, c.x);
        }

        public float3 GetCellWorldPosition(int2 coordinate)
        {
            var idx = ClampCoordinate(coordinate);

            var gridPosition = (float2) (WorldSize - 1) / 2f;
            var position = ((coordinate * 2 - 1) * gridPosition).xyy;
            position.z = 0;

            return position;
        }

        public float3 GetCornerWorldPosition(int2 corner)
        {
            var position = ((corner * 2 - 1) * (float2) WorldSize / 2f).xyy;
            position.z = 0;

            return position;
        }

        private int2 ClampCoordinate(int2 coordinate)
        {
            var c = coordinate % _size;
            return math.select(c, c + _size, c < int2.zero);
        }

        public IEnumerator<GridTileType> GetEnumerator()
        {
            return _grid.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
