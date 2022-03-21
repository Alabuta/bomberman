using System;
using System.Linq;
using App;
using Configs;
using Configs.Game;
using Configs.Level;
using Data;
using Entity.Enemies;
using Entity.Hero;
using Game;
using Infrastructure.Factory;
using Infrastructure.Services.Input;
using Infrastructure.Services.PersistentProgress;
using Level;
using Math.FixedPointMath;
using UI;
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

            _sceneLoader.Load(levelStage.LevelConfig.SceneName, () => OnLoaded(levelStage));
        }

        public void Exit()
        {
        }

        private void OnLoaded(LevelStage levelStage)
        {
            InitWorld(levelStage);

            InformProgressReaders();

            CreateGameStatsPanel(levelStage.GameModeConfig);

            _gameStateMachine.Enter<GameLoopState>();
        }

        private void CreateGameStatsPanel(GameModeConfig gameModeConfig)
        {
            var gameObject = _gameFactory.InstantiatePrefab(gameModeConfig.GameStatsViewPrefab, float3.zero);
            Game.GameStatsView = gameObject.GetComponent<GameStatsView>();
            Assert.IsNotNull(Game.GameStatsView);

            // :TODO: extend draw logic for variable players count
            var player = Game.LevelManager.Players.Values.FirstOrDefault();
            Assert.IsNotNull(player);

            gameObject.GetComponent<GameStatsView>().Construct(player.Hero);
        }

        private void InformProgressReaders()
        {
            foreach (var progressReader in _gameFactory.ProgressReaders)
                progressReader.LoadProgress(_progressService.Progress);
        }

        private void InitWorld(LevelStage levelStage)
        {
            var gameModeConfig = levelStage.GameModeConfig;
            var levelStageConfig = levelStage.LevelStageConfig;

            Random.InitState(levelStageConfig.RandomSeed);

            Game.LevelManager = new GameLevelManager(_gameFactory, levelStage);// :TODO: move to DI
            Game.LevelManager.GenerateLevelStage(levelStage, _gameFactory);

            var levelGridModel = Game.LevelManager.LevelGridModel;

            switch (levelStageConfig)
            {
                case LevelStagePvEConfig config when gameModeConfig is GameModePvEConfig gameModePvE:
                    CreatePlayersAndSpawnHeroesPvE(gameModePvE, config, levelGridModel);
                    break;

                case LevelStagePvPConfig config when gameModeConfig is GameModePvPConfig gameModePvP:
                    CreatePlayersAndSpawnHeroesPvP(gameModePvP, config, levelGridModel);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(levelStageConfig));
            }

            CreateAndSpawnEnemies(levelStageConfig, levelGridModel, Game.LevelManager);

            var defaultPlayer = Game.LevelManager.Players.Values.FirstOrDefault();// :TODO: use DefaultPlayerTag
            if (defaultPlayer != null)
                SetupCamera(levelStage, levelGridModel, defaultPlayer);
        }

        private void CreatePlayersAndSpawnHeroesPvE(GameModePvEConfig gameMode, LevelStagePvEConfig baseConfig,
            GameLevelGridModel levelGridModel)
        {
            CreatePlayerAndSpawnHero(levelGridModel, gameMode.PlayerConfig, baseConfig.PlayerSpawnCorner);
        }

        private void CreatePlayersAndSpawnHeroesPvP(GameModePvPConfig gameMode, LevelStagePvPConfig baseConfig,
            GameLevelGridModel levelGridModel)
        {
            var playerConfigs = gameMode.PlayerConfigs;
            var spawnCorners = baseConfig.PlayersSpawnCorners;
            Assert.IsTrue(playerConfigs.Length <= spawnCorners.Length, "players count greater than the level spawn corners");

            var zip = spawnCorners.Zip(playerConfigs, (spawnCorner, playerConfig) => (spawnCorner, playerConfig));
            foreach (var (spawnCorner, playerConfig) in zip)
                CreatePlayerAndSpawnHero(levelGridModel, playerConfig, spawnCorner);
        }

        private void CreatePlayerAndSpawnHero(GameLevelGridModel levelGridModel, PlayerConfig playerConfig, int2 spawnCorner)
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

        private void CreateAndSpawnEnemies(LevelStageConfig levelStageConfig, GameLevelGridModel levelGridModel,
            GameLevelManager gameLevelManager)
        {
            var enemySpawnElements = levelStageConfig.Enemies;
            var enemyConfigs = enemySpawnElements
                .SelectMany(e => Enumerable.Range(0, e.Count).Select(_ => e.EnemyConfig))
                .ToArray();

            var playersCoordinates = gameLevelManager.Players.Values
                .Select(p => levelGridModel.ToTileCoordinate(p.Hero.WorldPosition))
                .ToArray();

            var floorTiles = levelGridModel.GetTilesByType(LevelTileType.FloorTile)
                .Where(t => !playersCoordinates.Contains(t.Coordinate))
                .ToList();
            Assert.IsTrue(enemyConfigs.Length <= floorTiles.Count, "enemies to spawn count greater than the floor tiles count");

            foreach (var enemyConfig in enemyConfigs)
            {
                // var position = levelGridModel.ToWorldPosition(math.int2(8, 5));
                var index = (int) math.round(Random.value * (floorTiles.Count - 1));
                var floorTile = floorTiles[index];
                var go = _gameFactory.SpawnEntity(enemyConfig, fix2.ToXY(floorTile.WorldPosition));
                Assert.IsNotNull(go);

                floorTiles.RemoveAt(index);

                var entityController = go.GetComponent<EnemyController>();
                Assert.IsNotNull(entityController);

                var enemy = _gameFactory.CreateEnemy(enemyConfig, entityController);
                Assert.IsNotNull(enemy);

                Game.LevelManager.AddEnemy(enemy);

                var behaviourAgents = _gameFactory.CreateBehaviourAgent(enemyConfig.BehaviourConfig, enemy);
                Game.LevelManager.AddBehaviourAgents(enemy, behaviourAgents);
            }
        }

        private static void SetupCamera(LevelStage levelStage, GameLevelGridModel levelGridModel, IPlayer player)
        {
            // Camera setup and follow
            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            var playerPosition = player.Hero.WorldPosition;
            var levelSize = levelGridModel.Size;

            var levelConfig = levelStage.LevelConfig;

            var cameraRect = math.float2(Screen.width * 2f / Screen.height, 1) * mainCamera.orthographicSize;

            var fieldRect = (levelSize - cameraRect) / 2f;
            var fieldMargins = (float4) levelConfig.ViewportPadding / levelConfig.OriginalPixelsPerUnits;

            var position = math.clamp((float2) playerPosition, fieldMargins.xy - fieldRect, fieldRect + fieldMargins.zw);
            mainCamera.transform.position = math.float3(position, -1);
        }
    }
}
