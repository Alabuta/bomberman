using System.Collections.Generic;
using System.Threading.Tasks;
using Configs.Game;
using Game;
using Infrastructure.AssetManagement;
using Infrastructure.Services.PersistentProgress;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Infrastructure.Factory
{
    public class GameFactory : IGameFactory
    {
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

        public GameObject InstantiatePrefab(GameObject prefab, float3 position, Transform parent = null)
        {
            var gameObject = _assetProvider.Instantiate(prefab, position, parent);

            RegisterProgressWatchers(gameObject);

            return gameObject;
        }

        public async Task<GameObject> InstantiatePrefabAsync(AssetReferenceGameObject reference,
            float3 position,
            Transform parent = null)
        {
            var handle = Addressables.InstantiateAsync(reference, position, Quaternion.identity, parent);
            Assert.IsTrue(handle.IsValid(),
                $"invalid async operation handle {reference.SubObjectName}: {handle.Status} {handle.OperationException}");

            await handle.Task;

            Assert.IsTrue(handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null,
                $"failed to instantiate asset {reference.SubObjectName}");

            var gameObject = handle.Result;
            RegisterProgressWatchers(gameObject);

            return handle.Result;

            // Addressables.ReleaseInstance(handle); // :TODO:
            // Addressables.ReleaseAsset for final bundle unload
        }

        public async Task<T> LoadAssetAsync<T>(AssetReference reference)
        {
            var handle = Addressables.LoadAssetAsync<T>(reference);
            Assert.IsTrue(handle.IsValid(),
                $"invalid async operation handle {reference.SubObjectName}: {handle.Status} {handle.OperationException}");

            await handle.Task;

            Assert.IsTrue(handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null,
                $"failed to instantiate asset {reference.SubObjectName}");

            return handle.Result;
        }

        public async Task<IList<T>> LoadAssetsAsync<T>(IEnumerable<AssetReference> references)
        {
            var handle = Addressables.LoadAssetsAsync<T>(references, null, Addressables.MergeMode.Union);
            Assert.IsTrue(handle.IsValid(),
                $"failed to load assets {references}: {handle.Status} {handle.OperationException}");

            await handle.Task;

            Assert.IsTrue(handle.Status == AsyncOperationStatus.Succeeded, $"can't load assets {references}");

            return handle.Result;

            // Addressables.Release(handle); // :TODO:
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
