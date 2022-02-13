using System.Collections.Generic;
using Configs;
using Configs.Entity;
using Entity.Enemies;
using Entity.Hero;
using Game;
using Infrastructure.AssetManagement;
using Infrastructure.Services.PersistentProgress;
using Input;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInput = UnityEngine.InputSystem.PlayerInput;

namespace Infrastructure.Factory
{
    public class GameFactory : IGameFactory
    {
        private const string ControlScheme = "Keyboard";
        private readonly InputDevice[] _inputDevices = { Keyboard.current };

        private readonly IAssetProvider _assetProvider;

        public List<ISavedProgressReader> ProgressReaders { get; } = new();
        public List<ISavedProgressWriter> ProgressWriters { get; } = new();

        public GameFactory(IAssetProvider assetProvider)
        {
            _assetProvider = assetProvider;
        }

        public IPlayer CreatePlayer(PlayerConfig playerConfig)
        {
            return new Player(playerConfig);
        }

        public IPlayerInput CreatePlayerInputHolder(PlayerConfig playerConfig, int playerIndex)
        {
            var playerInput =
                PlayerInput.Instantiate(playerConfig.PlayerInputHolder, playerIndex, ControlScheme, -1, _inputDevices);
            return playerInput.GetComponent<IPlayerInput>();
        }

        public Hero CreateHero(HeroConfig heroConfig, HeroController entityController)
        {
            return new Hero(heroConfig, entityController);
        }

        public Enemy CreateEnemy(EnemyConfig enemyConfig)
        {
            return null;// :TODO:
        }

        public GameObject SpawnEntity(EntityConfig heroConfig, float3 position)
        {
            var gameObject = _assetProvider.Instantiate(heroConfig.Prefab, position);

            RegisterProgressWatchers(gameObject);

            return gameObject;
        }

        private void RegisterProgressWatchers(GameObject gameObject)
        {
            foreach (var progressReader in gameObject.GetComponentsInChildren<ISavedProgressReader>())
                RegisterProgressReader(progressReader);
        }

        private void RegisterProgressReader(ISavedProgressReader progressReader)
        {
            if (progressReader is ISavedProgressWriter progressWriter)
                ProgressWriters.Add(progressWriter);

            ProgressReaders.Add(progressReader);
        }

        public void CleanUp()
        {
            ProgressReaders.Clear();
            ProgressWriters.Clear();
        }

        /*private void SpawnBomb(float2 worldPosition)
        {
            var position = math.float3(math.round(worldPosition), 0);
            var prefab = _levelStageConfig.BombConfig.Prefab;
            var bomb = Object.Instantiate(prefab, position, Quaternion.identity);

            StartCoroutine.Start(ExecuteAfterTime(_levelStageConfig.BombConfig.LifetimeSec, () => { bomb.SetActive(false); }));
        }

        private static IEnumerator ExecuteAfterTime(float time, Action callback)
        {
            yield return new WaitForSeconds(time);

            callback?.Invoke();
        }*/
    }
}
