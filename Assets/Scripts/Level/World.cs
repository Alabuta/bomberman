﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Configs.Game;
using Configs.Level;
using Configs.Singletons;
using Data;
using Game;
using Game.Behaviours;
using Game.Components;
using Game.Components.Behaviours;
using Game.Systems;
using Game.Systems.Behaviours;
using Infrastructure.Factory;
using Infrastructure.Services.Input;
using Input;
using Leopotam.Ecs;
using Math;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Level
{
    public partial class World
    {
        private readonly IGameFactory _gameFactory;

        private readonly Dictionary<IPlayerInput, IPlayer> _playerInputs = new();

        private readonly Dictionary<PlayerTagConfig, IPlayer> _players = new();

        private readonly HashSet<EcsEntity> _enemies = new();

        private readonly Dictionary<IEntity, List<IBehaviourAgent>> _behaviourAgents = new();

        private readonly double _stageTimer;

        public RandomGenerator RandomGenerator { get; }

        public LevelModel LevelModel { get; private set; }

        public IReadOnlyDictionary<PlayerTagConfig, IPlayer> Players => _players;

        public double StageTimer =>
            math.max(0, _stageTimer - (double) (_simulationCurrentTime - _simulationStartTime));

        public World(ApplicationConfig applicationConfig, IGameFactory gameFactory, LevelStage levelStage)
        {
            TickRate = applicationConfig.TickRate;
            _fixedDeltaTime = fix.one / (fix) TickRate;

            _gameFactory = gameFactory;
            _stageTimer = levelStage.LevelStageConfig.LevelStageTimer;

            RandomGenerator = new RandomGenerator(levelStage.RandomSeed);

            _ecsWorld = new EcsWorld();
            _ecsSystems = new EcsSystems(_ecsWorld);
            _ecsFixedSystems = new EcsSystems(_ecsWorld);

#if UNITY_EDITOR
            Leopotam.Ecs.UnityIntegration.EcsWorldObserver.Create(_ecsWorld);
            Leopotam.Ecs.UnityIntegration.EcsSystemsObserver.Create(_ecsSystems);
            Leopotam.Ecs.UnityIntegration.EcsSystemsObserver.Create(_ecsFixedSystems);
#endif

            _ecsSystems
                .Add(new WorldViewUpdateSystem())
                .Inject(this)
                .Init();

            _ecsFixedSystems
                .OneFrame<DamageComponent>()
                .Add(new MovementBehaviourSystem())
                .Add(new CollisionsResolverSystem())
                .Add(new HealthSystem())
                .Inject(this)
                .Init();
        }

        public async Task InitWorld(IInputService inputService, LevelStage levelStage)
        {
            var gameModeConfig = levelStage.GameModeConfig;
            var levelStageConfig = levelStage.LevelStageConfig;

            GenerateLevelStage(levelStage);

            switch (levelStageConfig)
            {
                case LevelStagePvEConfig config when gameModeConfig is GameModePvEConfig gameModePvE:
                    await CreatePlayersAndSpawnHeroesPvE(gameModePvE, config, inputService);
                    break;

                case LevelStagePvPConfig config when gameModeConfig is GameModePvPConfig gameModePvP:
                    CreatePlayersAndSpawnHeroesPvP(gameModePvP, config, inputService);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(levelStageConfig));
            }

            await CreateAndSpawnEnemies(levelStageConfig);
        }

        public IPlayer GetPlayer(PlayerTagConfig playerTagConfig)
        {
            return _players.TryGetValue(playerTagConfig, out var player) ? player : null; // :TODO: refactor
        }

        public void Destroy()
        {
            _ecsFixedSystems.Destroy();
            _ecsSystems.Destroy();
            _ecsWorld.Destroy();
        }

        public EcsEntity NewEntity()
        {
            return _ecsWorld.NewEntity();
        }

        private void AddPlayer(PlayerTagConfig playerTagConfig, IPlayer player)
        {
            _players.Add(playerTagConfig, player); // :TODO: refactor
        }

        private void AddEnemy(EcsEntity enemy)
        {
            _enemies.Add(enemy);

            // enemy.Health.HealthChangedEvent += () => OnEntityHealthChangedEvent(enemy); :TODO:
        }

        private void OnEntityHealthChangedEvent(IEntity entity)
        {
            _behaviourAgents.Remove(entity);
        }

        private void AddBehaviourAgent(IEntity entity, IBehaviourAgent behaviourAgent)
        {
            if (!_behaviourAgents.TryGetValue(entity, out var agents))
            {
                agents = new List<IBehaviourAgent>();
                _behaviourAgents.Add(entity, agents);
            }

            agents.Add(behaviourAgent);
        }

        private void AttachPlayerInput(IPlayer player, IPlayerInput playerInput)
        {
            _playerInputs.Add(playerInput, player);

            playerInput.OnInputActionEvent += OnPlayerInputAction; // :TODO: unsubscribe when player is dead
        }

        private IEnumerable<EcsEntity> GetEnemiesByCoordinate(int2 coordinate)
        {
            return _enemies
                .Where(e => math.all(LevelModel.ToTileCoordinate(e.Get<TransformComponent>().WorldPosition) ==
                                     coordinate)); // :TODO: refactor
        }
    }
}
