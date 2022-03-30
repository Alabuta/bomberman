using System.Collections.Generic;
using Configs.Game;
using Data;
using Entity;
using Entity.Behaviours;
using Entity.Enemies;
using Game;
using Infrastructure.Factory;
using Infrastructure.Services.Input;
using Input;
using Unity.Mathematics;

namespace Level
{
    public partial class World
    {
        public LevelModel LevelModel { get; private set; }

        public IReadOnlyDictionary<PlayerTagConfig, IPlayer> Players => _players;

        public double LevelStageTimer =>
            math.max(0, _levelStageTimer - (double) (_simulationCurrentTime - _simulationStartTime));

        private readonly IGameFactory _gameFactory;

        private readonly Dictionary<IPlayerInput, IPlayer> _playerInputs = new();

        private readonly Dictionary<PlayerTagConfig, IPlayer> _players = new();
        private readonly HashSet<Enemy> _enemies = new();

        private readonly Dictionary<IEntity, List<IBehaviourAgent>> _behaviourAgents = new();

        private readonly double _levelStageTimer;


        public World(IGameFactory gameFactory, LevelStage levelStage, IInputService inputService)
        {
            _gameFactory = gameFactory;
            _levelStageTimer = levelStage.LevelStageConfig.LevelStageTimer;
        }

        public void AddPlayer(PlayerTagConfig playerTagConfig, IPlayer player)
        {
            _players.Add(playerTagConfig, player);// :TODO: refactor
        }

        public IPlayer GetPlayer(PlayerTagConfig playerTagConfig)
        {
            return _players.TryGetValue(playerTagConfig, out var player) ? player : null;// :TODO: refactor
        }

        public void AddEnemy(Enemy enemy)
        {
            _enemies.Add(enemy);
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

            playerInput.OnInputActionEvent += OnPlayerInputAction;// :TODO: unsubscribe when player is dead
        }
    }
}
