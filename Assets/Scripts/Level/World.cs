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
using Game.Components.Entities;
using Game.Components.Events;
using Game.Components.Tags;
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

        private readonly double _stageTimer;

        public RandomGenerator RandomGenerator { get; }

        public LevelModel LevelModel { get; private set; }

        public IReadOnlyDictionary<PlayerTagConfig, IPlayer> Players => _players;

        public double StageTimer =>
            math.max(0, _stageTimer - (double) (_simulationCurrentTime - _simulationStartTime));

        public World(ApplicationConfig applicationConfig, IGameFactory gameFactory, LevelStage levelStage)
        {
            TickRate = applicationConfig.TickRate;
            FixedDeltaTime = fix.one / (fix) TickRate;

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

            var healthSystem = new HealthSystem();
            healthSystem.HealthChangedEvent += OnEntityHealthChangedEvent;

            _ecsFixedSystems
                .OneFrame<AttackEventComponent>()
                .OneFrame<OnCollisionEnterEventComponent>()
                .Add(new MovementBehaviourSystem())
                .Add(new CollisionsResolverSystem())
                .Add(new AttackBehaviourSystem())
                .Add(healthSystem)
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
        }

        private void OnEntityHealthChangedEvent(EcsEntity ecsEntity)
        {
            if (ecsEntity.Has<HealthComponent>())
            {
                ref var healthComponent = ref ecsEntity.Get<HealthComponent>();
                if (healthComponent.IsAlive()) // :TODO: refactor
                {
                    if (ecsEntity.Has<EntityComponent>())
                    {
                        ref var entityComponent = ref ecsEntity.Get<EntityComponent>();
                        entityComponent.Controller.Kill();
                    }

                    if (ecsEntity.Has<TransformComponent>())
                    {
                        ref var transformComponent = ref ecsEntity.Get<TransformComponent>();
                        transformComponent.Speed = fix.zero;
                    }

                    if (ecsEntity.Has<HeroTag>())
                    {
                        var (playerInput, _) = _playerInputs.FirstOrDefault(pi => pi.Value.HeroEntity == ecsEntity);

                        if (playerInput != null)
                            playerInput.OnInputActionEvent -= OnPlayerInputAction;
                    }

                    // DeathEvent?.Invoke(this); // :TODO:

                    ecsEntity.Replace(new DeadTag());
                }
                else
                {
                    if (ecsEntity.Has<EntityComponent>())
                    {
                        ref var entityComponent = ref ecsEntity.Get<EntityComponent>();
                        entityComponent.Controller.TakeDamage();
                    }

                    // DamageEvent?.Invoke(this, damage); // :TODO:
                }
            }
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
