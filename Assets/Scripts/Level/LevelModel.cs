using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Configs.Level;
using Configs.Level.Tile;
using Items;
using JetBrains.Annotations;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace Level
{
    public sealed class LevelModel : IEnumerable<ILevelTileView>
    {
        private static readonly int2[] NeighborTilesOffsets =
        {
            new(1, 1),
            new(1, -1),
            new(-1, -1),
            new(-1, 1),

            new(0, 1),
            new(1, 0),
            new(0, -1),
            new(-1, 0)
        };

        [NotNull]
        private readonly LevelTile[] _tiles;

        private readonly int2 _size;
        private readonly fix _tileSizeWorldUnits;

        public int2 Size => _size;
        public fix2 WorldSize => (fix2) _size * _tileSizeWorldUnits;

        public int ColumnsNumber => _size.x;
        public int RowsNumber => _size.y;

        public fix TileSize => _tileSizeWorldUnits;
        public fix TileInnerRadius => _tileSizeWorldUnits / new fix(2);
        public fix TileOuterRadius => TileInnerRadius * fix.sqrt(new fix(2));

        public ILevelTileView this[int index] => _tiles[index];
        public ILevelTileView this[int2 coordinate] => _tiles[GetFlattenTileCoordinate(coordinate)];

        public LevelModel(LevelConfig levelConfig, LevelStageConfig levelStageConfig)
        {
            _tileSizeWorldUnits = (fix) levelConfig.TileSizeWorldUnits;

            _size = math.int2(levelStageConfig.ColumnsNumber, levelStageConfig.RowsNumber);
            Assert.IsTrue(math.all(_size % 2 != int2.zero));

            var softBlocksCoverage = levelStageConfig.SoftBlocksCoverage;

            var playersSpawnCorners = GetPlayersSpawnCorners(levelStageConfig);
            var spawnTilesIndices = GetPlayerSpawnTilesIndices(playersSpawnCorners);
            var spawnTilesIndicesCount = spawnTilesIndices.Count;

            var totalTilesCount = _size.x * _size.y - spawnTilesIndicesCount;
            var enemiesCount = levelStageConfig.Enemies.Sum(e => e.Count);

            var hardBlocksCount = (_size.x - 1) * (_size.y - 1) / 4;
            var softBlocksCount =
                (int) math.round((totalTilesCount - hardBlocksCount - enemiesCount) * softBlocksCoverage / 100f);
            var floorTilesCount = totalTilesCount - softBlocksCount - hardBlocksCount;

            /*var powerUpItems = levelStageConfig.PowerUpItems;
            var softBlocksPerPowerUpItem = softBlocksNumber / powerUpItems.Length;

            var powerItemsIndices = new HashSet<int>(
                Enumerable
                    .Range(0, powerUpItems.Length)
                    .Select(i => Random.Range(i * softBlocksPerPowerUpItem, (i + 1) * softBlocksPerPowerUpItem))
            );*/

            _tiles = GenerateLevelGrid(levelConfig, spawnTilesIndices, totalTilesCount, floorTilesCount, softBlocksCount);
        }

        private static IEnumerable<int2> GetPlayersSpawnCorners(LevelStageConfig levelStageConfig)
        {
            return levelStageConfig switch
            {
                LevelStagePvEConfig config => new[] { config.PlayerSpawnCorner },
                LevelStagePvPConfig config => config.PlayersSpawnCorners,
                _ => throw new ArgumentOutOfRangeException(nameof(levelStageConfig))
            };
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

        private LevelTile[] GenerateLevelGrid(LevelConfig levelConfig, ICollection<int> spawnTilesIndices, int totalTilesCount,
            int floorTilesCount, int softBlocksCount)
        {
            var tileTypeCount = math.int2(floorTilesCount, softBlocksCount);
            var spawnTilesIndicesCount = spawnTilesIndices.Count;

            var hardBlockConfig = levelConfig.HardBlockConfig;
            var softBlockConfig = levelConfig.SoftBlockConfig;

            return Enumerable
                .Range(0, totalTilesCount + spawnTilesIndicesCount)
                .Select(
                    index =>
                    {
                        var coordinate = math.int2(index % _size.x, index / _size.x);
                        var worldPosition = ToWorldPosition(coordinate);

                        if (spawnTilesIndices.Contains(index))
                            return new LevelTile(LevelTileType.FloorTile, coordinate, worldPosition);
                        // return new LevelTile(LevelTileType.FloorTile, coordinate, worldPosition);

                        if (IsHardBlockTileCoordinate(coordinate))
                        {
                            var levelTile = new LevelTile(LevelTileType.HardBlock, coordinate, worldPosition);
                            levelTile.SetLoad(CreateBlock(hardBlockConfig));
                            return levelTile;
                        }
                        // return new LevelTile(LevelTileType.HardBlock, coordinate, worldPosition);

                        var range = (int2) (tileTypeCount == int2.zero);

                        var softBlockOdds = (int) (tileTypeCount.y * 100.0f / (tileTypeCount.x + tileTypeCount.y));

                        var typeIndex = Convert.ToInt32(Random.Range(0, 100) < softBlockOdds);
                        typeIndex = math.clamp(typeIndex, range.x, 2 - range.y);

                        var tileType = typeIndex == 0 ? LevelTileType.FloorTile : LevelTileType.SoftBlock;
                        var tile = new LevelTile(tileType, coordinate, worldPosition);

                        if (tileType == LevelTileType.SoftBlock)
                            tile.SetLoad(CreateBlock(softBlockConfig));

                        --tileTypeCount[typeIndex];

                        return tile;
                    }
                )
                .ToArray();
        }

        private static TileLoad CreateBlock(BlockConfig blockConfig)
        {
            return blockConfig switch
            {
                HardBlockConfig config => new HardBlock(config),
                SoftBlockConfig config => new SoftBlock(config),
                _ => null
            };
        }

        public fix2 GetCornerWorldPosition(int2 corner)
        {
            var position = (fix2) (corner * 2 - 1) * (WorldSize - fix2.one) / new fix2(2);
            return position;
        }

        public int2 ToTileCoordinate(fix2 position)
        {
            var coordinate = position / _tileSizeWorldUnits + (fix2) (_size - 1) / new fix2(2);

            return ClampCoordinate((int2) fix2.round(coordinate));
        }

        public fix2 ToWorldPosition(int2 coordinate)
        {
            var position = (fix2) (coordinate - (_size - 1) / 2) * _tileSizeWorldUnits;
            return position;
        }

        private static bool IsHardBlockTileCoordinate(int2 coordinate) =>
            math.all(coordinate % 2 == 1);

        private int GetFlattenTileCoordinate(int2 coordinate)
        {
            var c = math.select(int2.zero, _size, coordinate < int2.zero) + coordinate;
            return math.mad(c.y, _size.x, c.x);
        }

        /*private int2 ClampCoordinate(int2 coordinate)
        {
            var c = coordinate % _size;
            return math.select(c, c + _size, c < int2.zero);
        }*/

        public int2 ClampCoordinate(int2 coordinate)
        {
            return math.clamp(coordinate, int2.zero, _size);
        }

        public bool IsCoordinateInField(int2 coordinate)
        {
            return math.all(coordinate >= 0) && math.all(coordinate < _size);
        }

        public IEnumerable<ILevelTileView> GetNeighborTiles(int2 coordinate)
        {
            return NeighborTilesOffsets
                .Select(o => coordinate + o)
                .Where(IsCoordinateInField)
                .Select(c => this[c]);
        }

        public IEnumerable<LevelTile> GetTilesByType(LevelTileType type)
        {
            return _tiles.Where(t => t.Type == type);
        }

        public IEnumerator<ILevelTileView> GetEnumerator()
        {
            return _tiles.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddItem(BombItem item, int2 coordinate)
        {
            _tiles[GetFlattenTileCoordinate(coordinate)].AddItem(item);
        }

        public void RemoveItem(int2 coordinate)
        {
            _tiles[GetFlattenTileCoordinate(coordinate)].RemoveItem();
        }
    }
}
