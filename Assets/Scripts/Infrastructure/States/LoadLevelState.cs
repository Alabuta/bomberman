using System;
using System.Linq;
using System.Threading.Tasks;
using App;
using Configs;
using Configs.Game;
using Configs.Level;
using Configs.Singletons;
using Data;
using Game;
using Game.Enemies;
using Game.Hero;
using Infrastructure.Factory;
using Infrastructure.Services.Input;
using Infrastructure.Services.PersistentProgress;
using Level;
using Math.FixedPointMath;
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
            IPersistentProgressService progressService, LoadingScreenController loadingScreenController)
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
            await CreateGameStatsPanel(levelStage.GameModeConfig);

            InformProgressReaders();

            _loadingScreenController.Hide(() =>
            {
#pragma warning disable CS4014
                _gameStateMachine.Enter<GameLoopState>();
#pragma warning restore CS4014
            });
        }

        public void Exit()
        {
        }

        private async Task CreateWorld(LevelStage levelStage)
        {
            var applicationConfig = ApplicationConfig.Instance;

            var gameModeConfig = levelStage.GameModeConfig;
            var levelStageConfig = levelStage.LevelStageConfig;

            Game.World = new World(applicationConfig, _gameFactory, levelStage); // :TODO: move to DI
            Game.World.GenerateLevelStage(Game.World, _gameFactory, levelStage);

            var levelGridModel = Game.World.LevelModel;

            switch (levelStageConfig)
            {
                case LevelStagePvEConfig config when gameModeConfig is GameModePvEConfig gameModePvE:
                    await CreatePlayersAndSpawnHeroesPvE(gameModePvE, config, levelGridModel);
                    break;

                case LevelStagePvPConfig config when gameModeConfig is GameModePvPConfig gameModePvP:
                    CreatePlayersAndSpawnHeroesPvP(gameModePvP, config, levelGridModel);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(levelStageConfig));
            }

            await CreateAndSpawnEnemies(levelStageConfig, levelGridModel, Game.World);

            var defaultPlayer = Game.World.Players.Values.FirstOrDefault(); // :TODO: use DefaultPlayerTag
            if (defaultPlayer != null)
                SetupCamera(levelStage, levelGridModel, defaultPlayer);
        }

        private async Task CreateGameStatsPanel(GameModeConfig gameModeConfig)
        {
            var instantiateTask = _gameFactory.InstantiatePrefabAsync(gameModeConfig.GameStatsViewPrefab, float3.zero);

            // :TODO: extend draw logic for variable players count
            var player = Game.World.Players.Values.FirstOrDefault();
            Assert.IsNotNull(player);

            var gameStatsObject = await instantiateTask;

            var gameStatsView = gameStatsObject.GetComponent<GameStatsView>();
            Assert.IsNotNull(gameStatsView);

            await gameStatsView.Construct(_gameFactory, Game.World.StageTimer, player.Hero);

            Game.GameStatsView = gameStatsView;
        }

        private void InformProgressReaders()
        {
            foreach (var progressReader in _gameFactory.ProgressReaders)
                progressReader.LoadProgress(_progressService.Progress);
        }

        private async Task CreatePlayersAndSpawnHeroesPvE(GameModePvEConfig gameMode, LevelStagePvEConfig baseConfig,
            LevelModel levelModel)
        {
            await CreatePlayerAndSpawnHero(levelModel, gameMode.PlayerConfig, baseConfig.PlayerSpawnCorner);
        }

        private void CreatePlayersAndSpawnHeroesPvP(GameModePvPConfig gameMode, LevelStagePvPConfig baseConfig,
            LevelModel levelModel)
        {
            var playerConfigs = gameMode.PlayerConfigs;
            var spawnCorners = baseConfig.PlayersSpawnCorners;
            Assert.IsTrue(playerConfigs.Length <= spawnCorners.Length, "players count greater than the level spawn corners");

            var tasks = spawnCorners
                .Zip(playerConfigs, (spawnCorner, playerConfig) => (spawnCorner, playerConfig))
                .Select(p => CreatePlayerAndSpawnHero(levelModel, p.playerConfig, p.spawnCorner))
                .ToArray();

            Task.WhenAll(tasks);
        }

        private async Task CreatePlayerAndSpawnHero(LevelModel levelModel, PlayerConfig playerConfig, int2 spawnCorner)
        {
            var player = _gameFactory.CreatePlayer(playerConfig);
            Assert.IsNotNull(player);

            var playerInput = _inputService.RegisterPlayerInput(player);
            Game.World.AttachPlayerInput(player, playerInput);

            var spawnCoordinate = levelModel.GetCornerWorldPosition(spawnCorner);
            var task = _gameFactory.InstantiatePrefabAsync(playerConfig.HeroConfig.Prefab, fix2.ToXY(spawnCoordinate));
            var go = await task;
            Assert.IsNotNull(go);

            var heroController = go.GetComponent<HeroController>();
            Assert.IsNotNull(heroController);

            var hero = _gameFactory.CreateHero(playerConfig.HeroConfig, heroController, Game.World.NewEntity());
            player.AttachHero(hero);

            Game.World.AddPlayer(playerConfig.PlayerTagConfig, player);
        }

        private async Task CreateAndSpawnEnemies(LevelStageConfig levelStageConfig, LevelModel levelModel,
            World world)
        {
            var enemySpawnElements = levelStageConfig.Enemies;
            var enemyConfigs = enemySpawnElements
                .SelectMany(e => Enumerable.Range(0, e.Count).Select(_ => e.EnemyConfig))
                .ToArray();

            var playersCoordinates = world.Players.Values
                .Select(p => levelModel.ToTileCoordinate(p.Hero.WorldPosition))
                .ToArray();

            var floorTiles = levelModel.GetTilesByType(LevelTileType.FloorTile)
                .Where(t => !playersCoordinates.Contains(t.Coordinate))
                .ToList();

            Assert.IsTrue(enemyConfigs.Length <= floorTiles.Count, "enemies to spawn count greater than the floor tiles count");

            foreach (var enemyConfig in enemyConfigs)
            {
                var index = world.RandomGenerator.Range(0, floorTiles.Count, levelStageConfig.Index);
                var floorTile = floorTiles[index];
                var task = _gameFactory.InstantiatePrefabAsync(enemyConfig.Prefab, fix2.ToXY(floorTile.WorldPosition));
                var go = await task;
                Assert.IsNotNull(go);

                floorTiles.RemoveAt(index);

                var entityController = go.GetComponent<EnemyController>();
                Assert.IsNotNull(entityController);

                var enemy = _gameFactory.CreateEnemy(enemyConfig, entityController, world.NewEntity());
                Assert.IsNotNull(enemy);

                world.AddEnemy(enemy);

                var behaviourAgents = _gameFactory.CreateBehaviourAgent(enemyConfig.BehaviourConfig, enemy);
                foreach (var behaviourAgent in behaviourAgents)
                    world.AddBehaviourAgent(enemy, behaviourAgent);

                _gameFactory.AddBehaviourComponents(enemyConfig.BehaviourConfig, enemy, enemy.Id);
            }
        }

        private static void SetupCamera(LevelStage levelStage, LevelModel levelModel, IPlayer player)
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            var playerPosition = player.Hero.WorldPosition;
            var levelSize = levelModel.Size;

            var levelConfig = levelStage.LevelConfig;

            var cameraRect = math.float2(Screen.width * 2f / Screen.height, 1) * mainCamera.orthographicSize;

            var fieldRect = (levelSize - cameraRect) / 2f;
            var fieldMargins = (float4) levelConfig.ViewportPadding / levelConfig.OriginalPixelsPerUnits;

            var position = math.clamp((float2) playerPosition, fieldMargins.xy - fieldRect, fieldRect + fieldMargins.zw);
            mainCamera.transform.position = math.float3(position, -1);
        }
    }
}
