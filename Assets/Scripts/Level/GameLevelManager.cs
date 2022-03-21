using System;
using System.Collections.Generic;
using System.Linq;
using Configs.Game;
using Configs.Level;
using Configs.Level.Tile;
using Data;
using Entity;
using Entity.Behaviours;
using Entity.Enemies;
using Game;
using Infrastructure.Factory;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Level
{
    public class GameLevelManager
    {
        private const int TickRate = 60;

        public GameLevelGridModel LevelGridModel { get; private set; }

        public double LevelStageTimer => math.max(0, _levelStageTimer - _simulationCurrentTime + _simulationStartTime);

        private readonly IGameFactory _gameFactory;

        private readonly Dictionary<PlayerTagConfig, IPlayer> _players = new();
        private readonly HashSet<Enemy> _enemies = new();

        private readonly Dictionary<IEntity, List<IBehaviourAgent>> _behaviours = new();

        private readonly double _levelStageTimer;
        private double _simulationStartTime;
        private double _simulationCurrentTime;

        private double _timeRemainder;
        private ulong _tick;

        public IReadOnlyDictionary<PlayerTagConfig, IPlayer> Players => _players;

        public GameLevelManager(IGameFactory gameFactory, LevelStage levelStage)
        {
            _gameFactory = gameFactory;
            _levelStageTimer = levelStage.LevelStageConfig.LevelStageTimer;
        }

        public void GenerateLevelStage(LevelStage levelStage, IGameFactory gameFactory)
        {
            var levelConfig = levelStage.LevelConfig;
            var levelStageConfig = levelStage.LevelStageConfig;

            LevelGridModel = new GameLevelGridModel(levelConfig, levelStageConfig);

            /*_hiddenItemsIndices = LevelGridModel
                .Select((_, i) => i)
                .Where(i => (LevelGridModel[i] & GridTileType.PowerUpItem) != 0)
                .ToArray();*/

            SpawnGameObjects(levelConfig, LevelGridModel, gameFactory);

            SetupWalls(levelConfig, LevelGridModel);
        }

        public void AddPlayer(PlayerTagConfig playerTagConfig, IPlayer player)
        {
            _players.Add(playerTagConfig, player);// :TODO: refactor

            player.BombPlantEvent += OnPlayerBombPlant;
            player.BombBlastEvent += OnPlayerBombBlast;
        }

        public IPlayer GetPlayer(PlayerTagConfig playerTagConfig)
        {
            return _players.TryGetValue(playerTagConfig, out var player) ? player : null;// :TODO: refactor
        }

        public void AddEnemy(Enemy enemy)
        {
            _enemies.Add(enemy);
        }

        private static void SetupWalls(LevelConfig levelConfig, GameLevelGridModel gameLevelGridModel)
        {
            var columnsNumber = gameLevelGridModel.ColumnsNumber;
            var rowsNumber = gameLevelGridModel.RowsNumber;

            var walls = Object.Instantiate(levelConfig.Walls, Vector3.zero, Quaternion.identity);

            var sprite = walls.GetComponent<SpriteRenderer>();
            sprite.size += new Vector2(columnsNumber, rowsNumber);

            var offsetsAndSize = new[]
            {
                (math.float2(+columnsNumber / 2f + 1, 0), math.float2(2, rowsNumber)),
                (math.float2(-columnsNumber / 2f - 1, 0), math.float2(2, rowsNumber)),
                (math.float2(0, +rowsNumber / 2f + 1), math.float2(columnsNumber, 2)),
                (math.float2(0, -rowsNumber / 2f - 1), math.float2(columnsNumber, 2))
            };

            foreach (var (offset, size) in offsetsAndSize)
            {
                var collider = walls.AddComponent<BoxCollider2D>();
                collider.offset = offset;
                collider.size = size;
            }
        }

        private static void SpawnGameObjects(LevelConfig levelConfig, GameLevelGridModel gameLevelGridModel,
            IGameFactory gameFactory)
        {
            var columnsNumber = gameLevelGridModel.ColumnsNumber;
            var rowsNumber = gameLevelGridModel.RowsNumber;

            var hardBlocksGroup = new GameObject("HardBlocks");
            var softBlocksGroup = new GameObject("SoftBlocks");

            // :TODO: refactor
            var blocks = new Dictionary<LevelTileType, (GameObject, BlockConfig)>
            {
                { LevelTileType.HardBlock, (hardBlocksGroup, levelConfig.HardBlockConfig) },
                { LevelTileType.SoftBlock, (softBlocksGroup, levelConfig.SoftBlockConfig) }
            };

            var startPosition = (math.float3(1) - math.float3(columnsNumber, rowsNumber, 0)) / 2;

            for (var i = 0; i < columnsNumber * rowsNumber; ++i)
            {
                var tile = gameLevelGridModel[i];
                var tileType = tile.Type;
                if (tileType == LevelTileType.FloorTile)
                    continue;

                // ReSharper disable once PossibleLossOfFraction
                var position = startPosition + math.float3(i % columnsNumber, i / columnsNumber, 0);

                var (parent, blockConfig) = blocks[tileType/* & ~LevelTileType.PowerUpItem*/];
                gameFactory.InstantiatePrefab(blockConfig.Prefab, position, parent.transform);
            }
        }

        public void StartSimulation()
        {
            _timeRemainder = 0;
            _tick = 0;

            _simulationStartTime = Time.timeAsDouble;
        }

        public void UpdateSimulation()
        {
            var heroes = Players.Values.Select(p => p.Hero).ToArray();
            var gameContext = new GameContext(LevelGridModel, heroes);

            var deltaTime = Time.deltaTime + _timeRemainder;

            var targetTick = _tick + (ulong) (TickRate * deltaTime);
            var tickCounts = targetTick - _tick;
            while (_tick < targetTick)
            {
                foreach (var (entity, behaviourAgents) in _behaviours)
                {
                    foreach (var behaviourAgent in behaviourAgents)
                        behaviourAgent.Update(gameContext, entity);
                }

                ++_tick;
            }

            _timeRemainder = math.max(0, deltaTime - tickCounts / (double) TickRate);

            _simulationCurrentTime += deltaTime - _timeRemainder;
        }

        public void AddBehaviourAgents(IEntity entity, IEnumerable<IBehaviourAgent> behaviourAgents)
        {
            foreach (var behaviourAgent in behaviourAgents)
            {
                if (!_behaviours.TryGetValue(entity, out var agents))
                {
                    agents = new List<IBehaviourAgent>();
                    _behaviours.Add(entity, agents);
                }

                agents.Add(behaviourAgent);
            }
        }

        private void OnPlayerBombPlant(IPlayer player, fix2 worldPosition)
        {
            var bombConfig = player.Hero.BombConfig;
            var bombCoordinate = LevelGridModel.ToTileCoordinate(worldPosition, true);
            var position = LevelGridModel.ToWorldPosition(bombCoordinate);

            _gameFactory.InstantiatePrefab(bombConfig.Prefab, fix2.ToXY(position));
        }

        private void OnPlayerBombBlast(IPlayer player)
        {
            throw new NotImplementedException();
        }
    }
}
