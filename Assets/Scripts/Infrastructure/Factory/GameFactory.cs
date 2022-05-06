using System;
using System.Collections.Generic;
using System.Linq;
using Configs;
using Configs.Behaviours;
using Configs.Entity;
using Configs.Items;
using Game;
using Game.Behaviours;
using Game.Enemies;
using Game.Hero;
using Game.Items;
using Infrastructure.AssetManagement;
using Infrastructure.Services.Input;
using Infrastructure.Services.PersistentProgress;
using Input;
using Items;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using PlayerInput = UnityEngine.InputSystem.PlayerInput;

namespace Infrastructure.Factory
{
    public class GameFactory : IGameFactory
    {
        private readonly InputDevice[] _inputDevices = { Keyboard.current };

        private readonly IAssetProvider _assetProvider;

        public List<ISavedProgressReader> ProgressReaders { get; } = new();
        public List<ISavedProgressWriter> ProgressWriters { get; } = new();

        public GameFactory(IAssetProvider assetProvider)
        {
            _assetProvider = assetProvider;
        }

        public IPlayerInput CreatePlayerInputHolder(PlayerConfig playerConfig, int playerIndex)
        {
            var playerInput =
                PlayerInput.Instantiate(playerConfig.PlayerInputHolder, playerIndex, InputService.ControlScheme, -1,
                    _inputDevices);
            return playerInput.GetComponent<IPlayerInput>();
        }

        public IPlayer CreatePlayer(PlayerConfig playerConfig)
        {
            return new Player(playerConfig);
        }

        public Hero CreateHero(HeroConfig heroConfig, HeroController entityController)
        {
            var hero = new Hero(heroConfig, entityController);

            // RegisterProgressReader(hero.HeroHealth); :TODO: remove?

            return hero;
        }

        public Enemy CreateEnemy(EnemyConfig enemyConfig, EnemyController entityController)
        {
            return new Enemy(enemyConfig, entityController);
        }

        public BombItem CreateItem(BombItemConfig bobItemConfig, ItemController controller)
        {
            return new BombItem(bobItemConfig, controller);
        }

        public IReadOnlyList<IBehaviourAgent> CreateBehaviourAgent(IEnumerable<BehaviourConfig> behaviourConfigs,
            IEntity entity)
        {
            return behaviourConfigs
                .Select(c => c.Make(entity))
                .ToArray();
        }

        public GameObject SpawnEntity(EntityConfig heroConfig, float3 position)
        {
            return InstantiatePrefab(heroConfig.Prefab, position);
        }

        public GameObject InstantiatePrefab(GameObject prefab, float3 position, Transform parent = null)
        {
            var gameObject = _assetProvider.Instantiate(prefab, position, parent);

            RegisterProgressWatchers(gameObject);

            return gameObject;
        }

        public async void InstantiatePrefabAsync(Action<GameObject> callback, AssetReferenceGameObject reference,
            float3 position, Transform parent = null)
        {
            var handle = Addressables.InstantiateAsync(reference, position, Quaternion.identity, parent);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
                return;

            callback?.Invoke(handle.Result);

            // Addressables.ReleaseInstance(handle); :TODO:
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
