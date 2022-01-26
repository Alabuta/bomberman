using App;
using Configs.Entity;
using Configs.Level;
using Configs.Singletons;
using Entity;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Infrastructure
{
    public class LoadLevelState : IPayloadedState<LevelConfig>
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly SceneLoader _sceneLoader;

        public LoadLevelState(GameStateMachine gameStateMachine, SceneLoader sceneLoader)
        {
            _gameStateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;
        }

        public void Enter(LevelConfig levelConfig)
        {
            _sceneLoader.Load(levelConfig.SceneName, () => OnSceneLoaded(levelConfig));
        }

        public void Exit()
        {
        }

        private void OnSceneLoaded(LevelConfig levelConfig)
        {
            // var tile = Instantiate("PillarTile 1");

            var applicationConfig = ApplicationConfig.Instance;
            Game.LevelManager.GenerateLevel(applicationConfig.GameModePvE, levelConfig);

            // Camera follow
        }

        /*private void SpawnPlayers(IReadOnlyCollection<PlayerConfig> playerConfigs, IReadOnlyCollection<int2> spawnCorners,
            GameLevelGridModel levelGridModel)
        {
            Assert.IsTrue(playerConfigs.Count <= spawnCorners.Count, "players count greater than the level spawn corners");

            var players = spawnCorners
                .Zip(playerConfigs, (spawnCorner, playerConfig) =>
                {
                    var position = levelGridModel.GetCellPosition(spawnCorner);
                    return SpawnHero(playerConfig.HeroConfig, position);
                })
                .ToArray();
        }*/

        private static IHero SpawnHero(HeroConfig heroConfig, float3 position)
        {
            var heroGameObject = Object.Instantiate(heroConfig.Prefab, position, Quaternion.identity);

            var heroController = heroGameObject.GetComponent<HeroController>();
            Assert.IsNotNull(heroController, "'HeroController' component is null");

            /*heroController.BombPlantedEvent += BombPlantEventHandler;
            EntitySpawnedEvent?.Invoke(heroController);*/

            return heroController;
        }

        private static GameObject Instantiate(string path)
        {
            var prefab = Resources.Load<GameObject>(path);
            return Object.Instantiate(prefab);
        }
    }
}
