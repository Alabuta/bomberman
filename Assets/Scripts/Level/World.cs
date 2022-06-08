using System.Collections.Generic;
using Configs.Game;
using Configs.Singletons;
using Data;
using Game;
using Game.Behaviours;
using Game.Enemies;
using Infrastructure.Factory;
using Input;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Level
{
    public partial class World
    {
        private readonly IGameFactory _gameFactory;

        private readonly Dictionary<IPlayerInput, IPlayer> _playerInputs = new();

        private readonly Dictionary<PlayerTagConfig, IPlayer> _players = new();

        private readonly HashSet<Enemy> _enemies = new();

        private readonly Dictionary<IEntity, List<IBehaviourAgent>> _behaviourAgents = new();

        private readonly double _stageTimer;

        public LevelModel LevelModel { get; private set; }

        public IReadOnlyDictionary<PlayerTagConfig, IPlayer> Players => _players;

        public double StageTimer =>
            math.max(0, _stageTimer - (double) (_simulationCurrentTime - _simulationStartTime));

        public World(ApplicationConfig applicationConfig, IGameFactory gameFactory, LevelStage levelStage)
        {
            _tickRate = applicationConfig.TickRate;
            _fixedDeltaTime = fix.one / (fix) _tickRate;

            _gameFactory = gameFactory;
            _stageTimer = levelStage.LevelStageConfig.LevelStageTimer;
        }

        public void AddPlayer(PlayerTagConfig playerTagConfig, IPlayer player)
        {
            _players.Add(playerTagConfig, player); // :TODO: refactor
        }

        public IPlayer GetPlayer(PlayerTagConfig playerTagConfig)
        {
            return _players.TryGetValue(playerTagConfig, out var player) ? player : null; // :TODO: refactor
        }

        public void AddEnemy(Enemy enemy)
        {
            _enemies.Add(enemy);

            enemy.Health.HealthChangedEvent += () => OnEntityHealthChangedEvent(enemy);
        }

        private void OnEntityHealthChangedEvent(IEntity entity)
        {
            _behaviourAgents.Remove(entity);
        }

        public void AddBehaviourAgent(IEntity entity, IBehaviourAgent behaviourAgent)
        {
            if (!_behaviourAgents.TryGetValue(entity, out var agents))
            {
                agents = new List<IBehaviourAgent>();
                _behaviourAgents.Add(entity, agents);
            }

            agents.Add(behaviourAgent);
        }

        public void AttachPlayerInput(IPlayer player, IPlayerInput playerInput)
        {
            _playerInputs.Add(playerInput, player);

            playerInput.OnInputActionEvent += OnPlayerInputAction; // :TODO: unsubscribe when player is dead
        }
    }
}
