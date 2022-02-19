﻿using System;
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
    public sealed class GameLevelGridModel : IEnumerable<ILevelTileView>
    {
        [NotNull]
        private readonly LevelTile[] _grid;

        private readonly int2 _size;
        private readonly float2 _tileSizeWorldUnits;

        public int2 Size => _size;
        public float2 WorldSize => _size * _tileSizeWorldUnits;

        public int ColumnsNumber => _size.x;
        public int RowsNumber => _size.y;

        public ILevelTileView this[int index] => _grid[index];
        public ILevelTileView this[int2 coordinate] => _grid[GetFlattenTileCoordinate(coordinate)];

        public GameLevelGridModel(LevelConfig levelConfig, LevelStageConfig levelStageConfig)
        {
            _tileSizeWorldUnits = levelConfig.TileSizeWorldUnits;

            _size = math.int2(levelStageConfig.ColumnsNumber, levelStageConfig.RowsNumber);
            Assert.IsTrue(math.all(_size % 2 != int2.zero));

            var playersSpawnCorners = levelStageConfig.PlayersSpawnCorners;
            var softBlocksCoverage = levelStageConfig.SoftBlocksCoverage;

            var spawnTilesIndices = GetPlayerSpawnTilesIndices(playersSpawnCorners);
            var spawnTilesIndicesCount = spawnTilesIndices.Count;

            var totalTilesCount = _size.x * _size.y - spawnTilesIndicesCount;

            var hardBlocksCount = (_size.x - 1) * (_size.y - 1) / 4;
            var softBlocksCount = (int) math.round((totalTilesCount - hardBlocksCount) * softBlocksCoverage / 100.0f);
            var floorTilesCount = totalTilesCount - softBlocksCount - hardBlocksCount;

            /*var powerUpItems = levelStageConfig.PowerUpItems;
            var softBlocksPerPowerUpItem = softBlocksNumber / powerUpItems.Length;

            var powerItemsIndices = new HashSet<int>(
                Enumerable
                    .Range(0, powerUpItems.Length)
                    .Select(i => Random.Range(i * softBlocksPerPowerUpItem, (i + 1) * softBlocksPerPowerUpItem))
            );*/

            _grid = GenerateLevelGrid(spawnTilesIndices, totalTilesCount, floorTilesCount, softBlocksCount);
        }

        private HashSet<int> GetPlayerSpawnTilesIndices(IEnumerable<int2> playersSpawnCorners)
        {
            return new HashSet<int>(
                playersSpawnCorners
                    .SelectMany(
                        corner =>
                        {
                            var coordinate = corner * (_size - 1);
                            var offset = math.select(-1, 1, corner == int2.zero);

                            return new[]
                            {
                                GetFlattenTileCoordinate(coordinate),
                                GetFlattenTileCoordinate(math.mad(math.int2(1, 0), offset, coordinate)),
                                GetFlattenTileCoordinate(math.mad(math.int2(0, 1), offset, coordinate))
                            };
                        }
                    )
            );
        }

        private LevelTile[] GenerateLevelGrid(ICollection<int> spawnTilesIndices, int totalTilesCount,
            int floorTilesCount, int softBlocksCount)
        {
            var tileTypeCount = math.int2(floorTilesCount, softBlocksCount);
            var spawnTilesIndicesCount = spawnTilesIndices.Count;

            return Enumerable
                .Range(0, totalTilesCount + spawnTilesIndicesCount)
                .Select(
                    index =>
                    {
                        var coordinate = math.int2(index % _size.x, index / _size.x);
                        var worldPosition = ToWorldPosition(coordinate);

                        if (spawnTilesIndices.Contains(index))
                            return new LevelTile(LevelTileType.FloorTile, coordinate, worldPosition);

                        if (IsHardBlockTileCoordinate(coordinate))
                            return new LevelTile(LevelTileType.HardBlock, coordinate, worldPosition);

                        var range = (int2) (tileTypeCount == int2.zero);

                        var softBlockOdds = (int) (tileTypeCount.y * 100.0f / (tileTypeCount.x + tileTypeCount.y));

                        var typeIndex = Convert.ToInt32(Random.Range(0, 100) < softBlockOdds);
                        typeIndex = math.clamp(typeIndex, range.x, 2 - range.y);

                        var tileType = typeIndex == 0 ? LevelTileType.FloorTile : LevelTileType.SoftBlock;

                        /*if (tileType == LevelTileType.SoftBlock && powerItemsIndices.Contains(tileTypeNumbers[typeIndex]))
                            tileType |= LevelTileType.PowerUpItem;*/

                        --tileTypeCount[typeIndex];

                        return new LevelTile(tileType, coordinate, worldPosition);
                    }
                )
                .ToArray();
        }

        public float3 GetCornerWorldPosition(int2 corner)
        {
            var position = ((corner * 2 - 1) * (WorldSize - 1) / 2f).xyy;
            position.z = 0;

            return position;
        }

        public int2 ToTileCoordinate(float3 position)
        {
            return (int2) (position.xy / _tileSizeWorldUnits + (_size - 1) / math.float2(2));
        }

        public float3 ToWorldPosition(int2 coordinate)
        {
            var position = ((coordinate - (_size - 1) / 2) * _tileSizeWorldUnits).xyy;
            position.z = 0;
            return position;
        }

        private static bool IsHardBlockTileCoordinate(int2 coordinate) =>
            math.all(coordinate % 2 == 1);

        private int GetFlattenTileCoordinate(int2 coordinate)
        {
            var c = math.select(int2.zero, _size, coordinate < int2.zero) + coordinate;
            return math.mad(c.y, _size.x, c.x);
        }

        private int2 ClampCoordinate(int2 coordinate)
        {
            var c = coordinate % _size;
            return math.select(c, c + _size, c < int2.zero);
        }

        public IEnumerator<ILevelTileView> GetEnumerator()
        {
            return _grid.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
