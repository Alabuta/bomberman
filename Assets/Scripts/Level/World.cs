using System;
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
    public partial class World
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

#if UNITY_EDITOR
            Leopotam.Ecs.UnityIntegration.EcsWorldObserver.Create(_ecsWorld);
            Leopotam.Ecs.UnityIntegration.EcsSystemsObserver.Create(_ecsSystems);
            Leopotam.Ecs.UnityIntegration.EcsSystemsObserver.Create(_ecsFixedSystems);
#endif

            var rectTreeSystem = new CollidersRectTreeSystem();

#if UNITY_EDITOR
            var collidersDrawerGo = new GameObject("CollidersBoundsDrawer");
            Assert.IsNotNull(collidersDrawerGo);
            var colliderBoundsDrawer = collidersDrawerGo.AddComponent<CollidersBoundsDrawer>();

            var rTreeDrawerGo = new GameObject("RTreeDrawer");
            Assert.IsNotNull(rTreeDrawerGo);
            var rTreeDrawer = rTreeDrawerGo.AddComponent<RTreeDrawer>();
            rTreeDrawer.SetRTree(rectTreeSystem);
#endif

            _ecsSystems
                .Add(new WorldViewUpdateSystem())
#if UNITY_EDITOR
                .Add(colliderBoundsDrawer)
#endif
                .Inject(this)
                .Init();

            var healthSystem = new HealthSystem();
            healthSystem.HealthChangedEvent += OnEntityHealthChangedEvent;

            _ecsFixedSystems
                .OneFrame<AttackEventComponent>()
                .OneFrame<OnCollisionEnterEventComponent>()
                .OneFrame<OnCollisionExitEventComponent>()
                .OneFrame<OnCollisionStayEventComponent>()
                .OneFrame<PrevFrameDataComponent>()
                .Add(new BeforeSimulationStepSystem())
                .Add(new EntitiesTreeSystem())
                .Add(rectTreeSystem)
                .Add(new MovementBehaviourSystem())
                .Add(new CollisionsDetectionSystem())
                .Add(new CollisionsResolverSystem())
                .Add(new CollisionEventsListenerSystem())
                .Add(new LevelEntitiesTreeSystem())
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

                    if (ecsEntity.Has<MovementComponent>())
                    {
                        ref var transformComponent = ref ecsEntity.Get<MovementComponent>();
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

            playerInput.OnInputActionEvent += OnPlayerInputAction;
        }

        private IEnumerable<EcsEntity> GetEnemiesByCoordinate(int2 coordinate)
        {
            // :TODO: refactor
            return _enemies
                .Where(e => math.all(LevelTiles.ToTileCoordinate(e.Get<TransformComponent>().WorldPosition) == coordinate));
        }
    }
}
