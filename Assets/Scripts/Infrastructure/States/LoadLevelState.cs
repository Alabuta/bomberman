using System.Linq;
using App;
using Configs.Game;
using Configs.Level;
using Configs.Singletons;
using Data;
using Entity.Hero;
using Game;
using Infrastructure.Factory;
using Infrastructure.Services.Input;
using Infrastructure.Services.PersistentProgress;
using Level;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

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

            Game.LevelManager = new GameLevelManager();
            Game.LevelManager.GenerateLevelStage(gameMode, levelStage);

            var levelStageConfig = GetLevelStageConfig(gameMode, levelStage);

            var levelGridModel = Game.LevelManager.LevelGridModel;

            CreateAndSpawnPlayers(gameMode, levelStageConfig, levelGridModel);

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
                var playerInput = _inputService.RegisterPlayerInput(playerConfig);

                var player = _gameFactory.CreatePlayer(playerConfig, playerInput);
                Assert.IsNotNull(player);

                var position = levelGridModel.GetCornerWorldPosition(spawnCorner);

                var go = _gameFactory.SpawnEntity(playerConfig.HeroConfig, position);
                Assert.IsNotNull(go);

                var hero = (IHero) go.GetComponent<HeroController>();
                player.AttachHero(hero, playerInput);

                Game.LevelManager.AddPlayer(playerConfig.PlayerTagConfig, player);
            }
        }

        private static void SetupCamera(LevelStage levelStage, GameModeBaseConfig gameMode, GameLevelGridModel levelGridModel,
            IPlayer defaultPlayer)
        {
            // Camera setup and follow
            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            var playerPosition = defaultPlayer.Hero.WorldPosition.xy;
            var levelSize = levelGridModel.Size;

            var levelConfig = gameMode.LevelConfigs[levelStage.LevelIndex];

            var cameraRect = math.float2(Screen.width * 2f / Screen.height, 1) * mainCamera.orthographicSize;

            var fieldRect = (levelSize - cameraRect) / 2f;
            var fieldMargins = (float4) levelConfig.ViewportPadding / levelConfig.OriginalPixelsPerUnits;

            var position = math.clamp(playerPosition, fieldMargins.xy - fieldRect, fieldRect + fieldMargins.zw);

            mainCamera.transform.position = math.float3(position, -1);
        }

        private static LevelStageConfig GetLevelStageConfig(GameModeBaseConfig gameMode, LevelStage levelStage)
        {
            return gameMode.LevelConfigs[levelStage.LevelIndex].LevelStages[levelStage.LevelStageIndex];
        }
    }
}
