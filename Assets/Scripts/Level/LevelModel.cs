using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Configs.Game.Colliders;
using Configs.Level;
using Game;
using Game.Components;
using Game.Components.Entities;
using JetBrains.Annotations;
using Leopotam.Ecs;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Level
{
    public sealed class LevelModel : IEnumerable<EcsEntity>
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
        private readonly EcsEntity[] _tiles;

        private readonly int2 _size;
        private readonly fix _tileSizeWorldUnits;

        public int2 Size => _size;
        public fix2 WorldSize => (fix2) _size * _tileSizeWorldUnits;

        public int ColumnsNumber => _size.x;
        public int RowsNumber => _size.y;

        public fix TileSize => _tileSizeWorldUnits;
        public fix TileInnerRadius => _tileSizeWorldUnits / new fix(2);
        public fix TileOuterRadius => TileInnerRadius * fix.sqrt(new fix(2));

        public EcsEntity this[int index] => _tiles[index];
        public EcsEntity this[int2 coordinate] => _tiles[GetFlattenTileCoordinate(coordinate)];

        public LevelModel(World world, LevelConfig levelConfig, LevelStageConfig levelStageConfig)
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

            _tiles = GenerateLevelGrid(world, levelConfig, spawnTilesIndices, totalTilesCount, floorTilesCount,
                softBlocksCount);
        }

        public void ClearTile(int2 coordinate)
        {
            var index = GetFlattenTileCoordinate(coordinate);
            var worldPosition = ToWorldPosition(coordinate);

            var ecsEntity = _tiles[index];

            ecsEntity.Replace(new LevelTileComponent
            {
                Type = LevelTileType.FloorTile
            });

            ecsEntity.Replace(new TransformComponent
            {
                WorldPosition = worldPosition
            });
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

        private EcsEntity[] GenerateLevelGrid(World world, LevelConfig levelConfig, ICollection<int> spawnTilesIndices,
            int totalTilesCount, int floorTilesCount, int softBlocksCount)
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
                        var ecsEntity = world.NewEntity();

                        var coordinate = math.int2(index % _size.x, index / _size.x);
                        var worldPosition = ToWorldPosition(coordinate);

                        ecsEntity.Replace(new TransformComponent
                        {
                            WorldPosition = worldPosition
                        });

                        if (spawnTilesIndices.Contains(index))
                        {
                            ecsEntity.Replace(new LevelTileComponent
                            {
                                Type = LevelTileType.FloorTile
                            });

                            return ecsEntity;
                        }

                        if (IsHardBlockTileCoordinate(coordinate))
                        {
                            ecsEntity.Replace(new LevelTileComponent
                            {
                                Type = LevelTileType.HardBlock
                            });

                            var colliderComponentConfigs = hardBlockConfig.Components
                                .Where(c => c is ColliderComponentConfig)
                                .Cast<ColliderComponentConfig>();

                            ecsEntity.AddColliderComponents(colliderComponentConfigs);

                            return ecsEntity;
                        }

                        var range = (int2) (tileTypeCount == int2.zero);

                        var softBlockOdds = (float) tileTypeCount.y / (tileTypeCount.x + tileTypeCount.y);

                        var rndNumber = world.RandomGenerator.Range(0, 100, levelConfig.Index, index);
                        var typeIndex = Convert.ToInt32(rndNumber * 0.01f < softBlockOdds);
                        typeIndex = math.clamp(typeIndex, range.x, 2 - range.y);

                        var tileType = typeIndex == 0 ? LevelTileType.FloorTile : LevelTileType.SoftBlock;

                        ecsEntity.Replace(new LevelTileComponent
                        {
                            Type = tileType
                        });

                        if (tileType == LevelTileType.SoftBlock)
                        {
                            var colliderComponentConfigs = softBlockConfig.Components
                                .Where(c => c is ColliderComponentConfig)
                                .Cast<ColliderComponentConfig>();

                            ecsEntity.AddColliderComponents(colliderComponentConfigs);
                        }

                        --tileTypeCount[typeIndex];

                        return ecsEntity;
                    }
                )
                .ToArray();
        }

        /*private static ITileLoad CreateBlock(BlockConfig blockConfig)
        {
            return blockConfig switch
            {
                HardBlockConfig config => new HardBlock(config),
                SoftBlockConfig config => new SoftBlock(config),
                _ => null
            };
        }*/

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

        public IEnumerable<EcsEntity> GetNeighborTiles(int2 coordinate)
        {
            return NeighborTilesOffsets
                .Select(o => coordinate + o)
                .Where(IsCoordinateInField)
                .Select(c => this[c]);
        }

        public IEnumerable<EcsEntity> GetTilesByType(LevelTileType type)
        {
            var tilesByType = _tiles.Where(t => t.Get<LevelTileComponent>().Type == type).ToArray();
            return tilesByType; // :TODO: refactor
        }

        public IEnumerator<EcsEntity> GetEnumerator()
        {
            return _tiles.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /*public void AddItem(BombItem item, int2 coordinate)
        {
            _tiles[GetFlattenTileCoordinate(coordinate)].SetLoad(item); // :TODO: refactor
        }*/

        /*public void RemoveItem(int2 coordinate)
        {
            _tiles[GetFlattenTileCoordinate(coordinate)].RemoveLoad(); // :TODO: refactor
        }*/
    }
}
