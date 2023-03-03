using System.Collections.Generic;
using System.Threading.Tasks;
using Configs.Game;
using Game;
using Infrastructure.Services;
using Infrastructure.Services.PersistentProgress;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Infrastructure.Factory
{
    public interface IGameFactory : IService
    {
        Task<IList<T>> LoadAssetsAsync<T>(IEnumerable<AssetReference> references);

        Task<T> LoadAssetAsync<T>(AssetReference reference);

        [CanBeNull]
        IPlayer CreatePlayer(PlayerConfig playerConfig);

        GameObject InstantiatePrefab(GameObject prefab, float3 position, Transform parent = null);

        Task<GameObject> InstantiatePrefabAsync(
            AssetReferenceGameObject reference,
            float3 position,
            Transform parent = null);

        void CleanUp();

        List<ISavedProgressReader> ProgressReaders { get; }

        List<ISavedProgressWriter> ProgressWriters { get; }
    }
}
