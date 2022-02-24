using System.Linq;
using App;
using Configs.Game;
using Configs.Level;
using Configs.Singletons;
using Data;
using Entity.Behaviours;
using Entity.Enemies;
using Entity.Hero;
using Game;
using Infrastructure.Factory;
using Infrastructure.Services.Input;
using Infrastructure.Services.PersistentProgress;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace Infrastructure.States
{
    public class LoadLevelState : IPayloadedState<LevelStage>
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly SceneLoader _sceneLoader;
        private readonly IGameFactory _gameFactory;
        private readonly IInputService _inputService;
        private readonly IPersistentProgressService _progressService;

        public LoadLevelState(
            GameStateMachine gameStateMachine,
            SceneLoader sceneLoader,
            IGameFactory gameFactory,
            IInputService inputService,
            IPersistentProgressService progressService)
        {
            _gameStateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;
            _gameFactory = gameFactory;
            _progressService = progressService;
            _inputService = inputService;
        }

        public void Enter(LevelStage levelStage)
        {
            // :TODO: show loading progress

            _gameFactory.CleanUp();

            var applicationConfig = ApplicationConfig.Instance;

            var gameMode = applicationConfig.GameModePvE;
            var levelConfig = gameMode.LevelConfigs[levelStage.LevelIndex];

            _sceneLoader.Load(levelConfig.SceneName, () => OnLoaded(levelStage));
        }

        public void Exit()
        {
        }

        private void OnLoaded(LevelStage levelStage)
        {
            InitWorld(levelStage);
            InformProgressReaders();

            _gameStateMachine.Enter<GameLoopState>();
        }

        private void InformProgressReaders()
        {
            foreach (var progressReader in _gameFactory.ProgressReaders)
                progressReader.LoadProgress(_progressService.Progress);
        }

        private void InitWorld(LevelStage levelStage)
        {
            var applicationConfig = ApplicationConfig.Instance;
            var gameMode = applicationConfig.GameModePvE;

            var levelStageConfig = GetLevelStageConfig(gameMode, levelStage);

            Random.InitState(levelStageConfig.RandomSeed);

            Game.LevelManager = new GameLevelManager();
            Game.LevelManager.GenerateLevelStage(gameMode, levelStage);

            var levelGridModel = Game.LevelManager.LevelGridModel;

            CreateAndSpawnPlayers(gameMode, levelStageConfig, levelGridModel);

            CreateAndSpawnEnemies(levelStageConfig, levelGridModel);

            var defaultPlayerTag = applicationConfig.DefaultPlayerTag;
            var defaultPlayer = Game.LevelManager.GetPlayer(defaultPlayerTag);
            SetupCamera(levelStage, gameMode, levelGridModel, defaultPlayer);
        }

        private void CreateAndSpawnPlayers(GameModePvEConfig gameMode, LevelStageConfig levelStageConfig,
            GameLevelGridModel levelGridModel)
        {
            var playerConfigs = gameMode.PlayerConfigs;
            var spawnCorners = levelStageConfig.PlayersSpawnCorners;
            Assert.IsTrue(playerConfigs.Length <= spawnCorners.Length, "players count greater than the level spawn corners");

            var zip = spawnCorners.Zip(playerConfigs, (spawnCorner, playerConfig) => (spawnCorner, playerConfig));
            foreach (var (spawnCorner, playerConfig) in zip)
            {
                var player = _gameFactory.CreatePlayer(playerConfig);
                Assert.IsNotNull(player);

                var playerInput = _inputService.RegisterPlayerInput(playerConfig);
                player.AttachPlayerInput(playerInput);

                var position = levelGridModel.GetCornerWorldPosition(spawnCorner);

                var go = _gameFactory.SpawnEntity(playerConfig.HeroConfig, fix2.ToXY(position));
                Assert.IsNotNull(go);

                var heroController = go.GetComponent<HeroController>();
                Assert.IsNotNull(heroController);

                var hero = _gameFactory.CreateHero(playerConfig.HeroConfig, heroController);
                player.AttachHero(hero);

                Game.LevelManager.AddPlayer(playerConfig.PlayerTagConfig, player);
            }
        }

        private void CreateAndSpawnEnemies(LevelStageConfig levelStageConfig, GameLevelGridModel levelGridModel)
        {
            var enemySpawnElements = levelStageConfig.Enemies;
            foreach (var enemySpawnElement in enemySpawnElements)
            {
                var position = levelGridModel.ToWorldPosition(math.int2(2, 0));
                var go = _gameFactory.SpawnEntity(enemySpawnElement.EnemyConfig, fix2.ToXY(position));
                Assert.IsNotNull(go);

                var entityController = go.GetComponent<EnemyController>();
                Assert.IsNotNull(entityController);

                var enemy = _gameFactory.CreateEnemy(enemySpawnElement.EnemyConfig, entityController);
                Assert.IsNotNull(enemy);

                Game.LevelManager.AddEnemy(enemySpawnElement.EnemyConfig, enemy);

                var behaviourAgent = new MovementBehaviourAgent(enemySpawnElement.EnemyConfig.BehaviourConfig, enemy);
                Game.LevelManager.AddBehaviourAgent(enemy, behaviourAgent);
            }
        }

        private static void SetupCamera(LevelStage levelStage, GameModeBaseConfig gameMode, GameLevelGridModel levelGridModel,
            IPlayer defaultPlayer)
        {
            // Camera setup and follow
            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            var playerPosition = defaultPlayer.Hero.WorldPosition;
            var levelSize = levelGridModel.Size;

            var levelConfig = gameMode.LevelConfigs[levelStage.LevelIndex];

            var cameraRect = math.float2(Screen.width * 2f / Screen.height, 1) * mainCamera.orthographicSize;

            var fieldRect = (levelSize - cameraRect) / 2f;
            var fieldMargins = (float4) levelConfig.ViewportPadding / levelConfig.OriginalPixelsPerUnits;

            var position = math.clamp((float2) playerPosition, fieldMargins.xy - fieldRect, fieldRect + fieldMargins.zw);
            mainCamera.transform.position = math.float3(position, -1);
        }

        private static LevelStageConfig GetLevelStageConfig(GameModeBaseConfig gameMode, LevelStage levelStage)
        {
            return gameMode.LevelConfigs[levelStage.LevelIndex].LevelStages[levelStage.LevelStageIndex];
        }
    }
}
