using System.Collections.Generic;
using System.Linq;
using Configs.Entity;
using Configs.Game;
using Configs.Level;
using Data;
using Entity;
using Entity.Behaviours;
using Entity.Enemies;
using Game;
using Unity.Mathematics;
using UnityEngine;

namespace Level
{
    public class GameLevelManager
    {
        public GameLevelGridModel LevelGridModel { get; private set; }

        private readonly Dictionary<PlayerTagConfig, IPlayer> _players = new();
        private readonly Dictionary<EnemyConfig, Enemy> _enemies = new();
        private readonly Dictionary<IEntity, List<BehaviourAgent>> _behaviours = new();
        private int _xxxx;
        private double _timeRemainder;
        private ulong _tick;

        public void GenerateLevelStage(GameModeBaseConfig gameMode, LevelStage levelStage)
        {
            var levelConfig = gameMode.LevelConfigs[levelStage.LevelIndex];
            var levelStageConfig = levelConfig.LevelStages.First();

            LevelGridModel = new GameLevelGridModel(levelConfig, levelStageConfig);

            /*_hiddenItemsIndices = LevelGridModel
                .Select((_, i) => i)
                .Where(i => (LevelGridModel[i] & GridTileType.PowerUpItem) != 0)
                .ToArray();*/

            SpawnGameObjects(levelConfig, LevelGridModel);

            SetupWalls(levelConfig, LevelGridModel);
        }

        public void AddPlayer(PlayerTagConfig playerTagConfig, IPlayer player)
        {
            _players.Add(playerTagConfig, player);// :TODO: refactor
        }

        public IPlayer GetPlayer(PlayerTagConfig playerTagConfig)
        {
            return _players.TryGetValue(playerTagConfig, out var player) ? player : null;// :TODO: refactor
        }

        public void AddEnemy(EnemyConfig enemyConfig, Enemy enemy)
        {
            _enemies.Add(enemyConfig, enemy);
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

        private static void SpawnGameObjects(LevelConfig levelConfig, GameLevelGridModel gameLevelGridModel)
        {
            var columnsNumber = gameLevelGridModel.ColumnsNumber;
            var rowsNumber = gameLevelGridModel.RowsNumber;

            var hardBlocksGroup = new GameObject("HardBlocks");
            var softBlocksGroup = new GameObject("SoftBlocks");

            // :TODO: refactor
            var blocks = new Dictionary<LevelTileType, (GameObject, GameObject)>
            {
                { LevelTileType.HardBlock, (hardBlocksGroup, levelConfig.HardBlockConfig.Prefab) },
                { LevelTileType.SoftBlock, (softBlocksGroup, levelConfig.SoftBlockConfig.Prefab) }
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

                var (parent, prefab) = blocks[tileType/* & ~LevelTileType.PowerUpItem*/];
                Object.Instantiate(prefab, position, Quaternion.identity, parent.transform);
            }
        }

        public void StartSimulation()
        {
            _timeRemainder = 0;
            _tick = 0;
        }

        public void UpdateSimulation()
        {
            const int ticksPerSecond = 60;

            var gameContext = new GameContext(LevelGridModel);

            var deltaTime = Time.deltaTime + _timeRemainder;

            var targetTick = _tick + (ulong) (ticksPerSecond * deltaTime);
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

            _timeRemainder = math.max(0, deltaTime - tickCounts / (double) ticksPerSecond);
        }

        public void AddBehaviourAgent(IEntity entity, BehaviourAgent behaviourAgent)
        {
            if (!_behaviours.TryGetValue(entity, out var agents))
            {
                agents = new List<BehaviourAgent>();
                _behaviours.Add(entity, agents);
            }

            agents.Add(behaviourAgent);
        }
    }
}
