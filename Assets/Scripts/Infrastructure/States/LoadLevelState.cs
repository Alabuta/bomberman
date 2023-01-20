using System.Linq;
using System.Threading.Tasks;
using App;
using Configs.Game;
using Configs.Singletons;
using Data;
using Game;
using Game.Components;
using Infrastructure.Factory;
using Infrastructure.Services.Input;
using Infrastructure.Services.PersistentProgress;
using Leopotam.Ecs;
using Level;
using UI;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Infrastructure.States
{
    public class LoadLevelState : IPayloadedState<LevelStage>
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly IGameFactory _gameFactory;
        private readonly IInputService _inputService;
        private readonly IPersistentProgressService _progressService;
        private readonly LoadingScreenController _loadingScreenController;

        public LoadLevelState(GameStateMachine gameStateMachine,
            IGameFactory gameFactory,
            IInputService inputService,
            IPersistentProgressService progressService,
            LoadingScreenController loadingScreenController)
        {
            _gameStateMachine = gameStateMachine;
            _gameFactory = gameFactory;
            _progressService = progressService;
            _loadingScreenController = loadingScreenController;
            _inputService = inputService;
        }

        public async Task Enter(LevelStage levelStage)
        {
            _loadingScreenController.Show();

            _gameFactory.CleanUp();

            await SceneLoader.LoadSceneAsAddressable(levelStage.LevelConfig.SceneName);
            await OnLoaded(levelStage);
        }

        private async Task OnLoaded(LevelStage levelStage)
        {
            await CreateWorld(levelStage);
            await CreateGameStatsPanel(levelStage.GameModeConfig, Game.World);

            InformProgressReaders();

            _loadingScreenController.Hide(OnLoadingScreenHideCallback);
        }

        private void OnLoadingScreenHideCallback()
        {
#pragma warning disable CS4014
            _gameStateMachine.Enter<GameLoopState>();
#pragma warning restore CS4014
        }

        public void Exit()
        {
        }

        private async Task CreateWorld(LevelStage levelStage)
        {
            var applicationConfig = ApplicationConfig.Instance;

            Game.World?.Dispose();

            var gameWorld = new World(applicationConfig, _gameFactory, levelStage); // :TODO: move to DI
            await gameWorld.InitWorld(_inputService, levelStage);

            var defaultPlayer = gameWorld.Players.Values.FirstOrDefault(); // :TODO: use DefaultPlayerTag
            if (defaultPlayer != null)
                SetupCamera(levelStage, gameWorld.LevelTiles, defaultPlayer);

            gameWorld.UpdateWorldView();

            Game.World = gameWorld;
        }

        // :TODO: rename to CreateGameUI and support whole game UI set up
        private async Task CreateGameStatsPanel(GameModeConfig gameModeConfig, World world)
        {
            var instantiateTask = _gameFactory.InstantiatePrefabAsync(gameModeConfig.GameStatsViewPrefab, float3.zero);

            // :TODO: extend draw logic for variable players count
            var player = world.Players.Values.FirstOrDefault();
            Assert.IsNotNull(player);

            var gameStatsObject = await instantiateTask;

            var gameStatsView = gameStatsObject.GetComponent<GameStatsView>();
            Assert.IsNotNull(gameStatsView);

            await gameStatsView.Construct(_gameFactory, world.StageTimer, player.HeroEntity);

            Game.GameStatsView = gameStatsView;
        }

        private void InformProgressReaders()
        {
            foreach (var progressReader in _gameFactory.ProgressReaders)
                progressReader.LoadProgress(_progressService.Progress);
        }

        private static void SetupCamera(LevelStage levelStage, LevelTiles levelTiles, IPlayer player)
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            var heroEntity = player.HeroEntity;
            var playerPosition = heroEntity.Get<TransformComponent>().WorldPosition;
            var levelSize = levelTiles.Size;

            var levelConfig = levelStage.LevelConfig;

            var cameraRect = math.float2(Screen.width * 2f / Screen.height, 1) * mainCamera.orthographicSize;

            var fieldRect = (levelSize - cameraRect) / 2f;
            var fieldMargins = (float4) levelConfig.ViewportPadding / levelConfig.OriginalPixelsPerUnits;

            var position = math.clamp((float2) playerPosition, fieldMargins.xy - fieldRect, fieldRect + fieldMargins.zw);
            mainCamera.transform.position = math.float3(position, -1);
        }
    }
}
