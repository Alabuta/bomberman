﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Configs.Game;
using Configs.Level;
using Configs.Singletons;
using Data;
using Game;
using Game.Components;
using Game.Components.Events;
using Game.Systems;
using Game.Systems.Behaviours;
using Game.Systems.RTree;
using Gizmos;
using Infrastructure.Factory;
using Infrastructure.Services.Input;
using Input;
using Leopotam.Ecs;
using Math;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Level
{
    public partial class World : IDisposable
    {
        private readonly IGameFactory _gameFactory;

        private readonly Dictionary<IPlayerInput, IPlayer> _playerInputs = new();
        private readonly Dictionary<PlayerTagConfig, IPlayer> _players = new();

        private readonly HashSet<EcsEntity> _enemies = new();

        private readonly fix _stageTimer;

        public RandomGenerator RandomGenerator { get; }

        public LevelTiles LevelTiles { get; private set; }

        public IReadOnlyDictionary<PlayerTagConfig, IPlayer> Players => _players;

        public fix StageTimer =>
            fix.max(fix.zero, _stageTimer - _simulationCurrentTime + _simulationStartTime);

        public World(ApplicationConfig applicationConfig, IGameFactory gameFactory, LevelStage levelStage)
        {
            TickRate = applicationConfig.TickRate;
            FixedDeltaTime = fix.one / (fix) TickRate;

            _gameFactory = gameFactory;
            _stageTimer = (fix) levelStage.LevelStageConfig.LevelStageTimer;

            RandomGenerator = new RandomGenerator(levelStage.RandomSeed);

            _ecsWorld = new EcsWorld();
            _ecsSystems = new EcsSystems(_ecsWorld);
            _ecsFixedSystems = new EcsSystems(_ecsWorld);

            _entitiesAabbTree = new AabbRTree();

#if UNITY_EDITOR
            Leopotam.Ecs.UnityIntegration.EcsWorldObserver.Create(_ecsWorld);
            Leopotam.Ecs.UnityIntegration.EcsSystemsObserver.Create(_ecsSystems);
            Leopotam.Ecs.UnityIntegration.EcsSystemsObserver.Create(_ecsFixedSystems);

            var rTreeDrawerGameObject = new GameObject("RTreeDrawer");
            Assert.IsNotNull(rTreeDrawerGameObject);
            var rTreeDrawer = rTreeDrawerGameObject.AddComponent<AabbEntitiesTreeDrawer>();
            rTreeDrawer.SetRTree(_entitiesAabbTree);
#endif

            _ecsSystems
                .Add(new WorldViewUpdateSystem())
                .Inject(this)
                .Init();

            var healthSystem = new HealthSystem();
            healthSystem.HealthChangedEvent += OnEntityHealthChangedEvent;

            _ecsFixedSystems
                .OneFrame<AttackEventComponent>()
                .OneFrame<OnCollisionEnterEventComponent>()
                .OneFrame<OnCollisionExitEventComponent>()
                .OneFrame<OnCollisionStayEventComponent>()
                .OneFrame<OnBombBlastEventComponent>()
                .OneFrame<PrevFrameDataComponent>()
                .Add(new BeforeSimulationStepSystem())
                .Add(new MovementBehaviourSystem())
                .Add(new CollisionsDetectionSystem())
                .Add(new CollisionsResolverSystem())
                .Add(new BombsProcessSystem())
                .Add(new LevelEntitiesTreeSystem())
                .Add(new AttackBehaviourSystem())
                .Add(healthSystem)
                .Inject(this)
                .Inject(_entitiesAabbTree)
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
        }

        private void AttachPlayerInput(IPlayer player, IPlayerInput playerInput)
        {
            _playerInputs.Add(playerInput, player);

            playerInput.OnInputActionEvent += OnPlayerInputAction;
        }

        private IEnumerable<EcsEntity> GetEnemiesByCoordinate(int2 coordinate)
        {
            // :TODO: refactor
            return _enemies
                .Where(e => math.all(LevelTiles.ToTileCoordinate(e.Get<TransformComponent>().WorldPosition) == coordinate));
        }

        public void Dispose()
        {
            _entitiesAabbTree?.Dispose();

            _ecsFixedSystems.Destroy();
            _ecsSystems.Destroy();
            _ecsWorld.Destroy();
        }
    }
}
