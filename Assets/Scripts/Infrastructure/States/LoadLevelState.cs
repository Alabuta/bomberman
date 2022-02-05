using System.Linq;
using App;
using Configs.Game;
using Configs.Singletons;
using Data;
using Infrastructure.Factory;
using Infrastructure.Services.Input;
using Infrastructure.Services.PersistentProgress;
using Infrastructure.States;
using Level;
using Unity.Mathematics;
using UnityEngine;

namespace Infrastructure
{
    public class LoadLevelState : IPayloadedState<LevelStage>
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly SceneLoader _sceneLoader;
        private readonly IGameFactory _gameFactory;
        private readonly IPersistentProgressService _progressService;
        private readonly IInputService _inputService;

        public LoadLevelState(
            GameStateMachine gameStateMachine,
            SceneLoader sceneLoader,
            IGameFactory gameFactory,
            IPersistentProgressService progressService,
            IInputService inputService)
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

            foreach (var playerConfig in gameMode.PlayerConfigs)
            {
                var playerInput = _inputService.RegisterPlayerInput(playerConfig);
                var player = _gameFactory.CreatePlayer(playerConfig, playerInput);

                Game.LevelManager.AddPlayer(playerConfig, player);
            }

            Game.LevelManager.GenerateLevelStage(gameMode, levelStage, _gameFactory);

            // Camera setup and follow
            var levelStageConfig = gameMode.LevelConfigs[levelStage.LevelIndex].LevelStages[levelStage.LevelStageIndex];
            var defaultPlayerCorner = levelStageConfig.PlayersSpawnCorners.FirstOrDefault();
            SetupCamera(gameMode, levelStage, Game.LevelManager.LevelGridModel.Size, defaultPlayerCorner);
        }

        private static void SetupCamera(GameModeBaseConfig gameMode, LevelStage levelStage, int2 levelSize,
            int2 defaultPlayerCorner)
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            var levelConfig = gameMode.LevelConfigs[levelStage.LevelIndex];

            var cameraRect = math.float2(Screen.width * 2f / Screen.height, 1) * mainCamera.orthographicSize;

            var fieldRect = (levelSize - cameraRect) / 2f;
            var fieldMargins = (float4) levelConfig.ViewportPadding / levelConfig.OriginalPixelsPerUnits;

            var position = (defaultPlayerCorner - (float2) .5f) * levelSize;
            position = math.clamp(position, fieldMargins.xy - fieldRect, fieldRect + fieldMargins.zw);

            mainCamera.transform.position = math.float3(position, -1);
        }
    }
}
