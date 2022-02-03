using System.Collections.Generic;
using Configs.Entity;
using Infrastructure.AssetManagement;
using JetBrains.Annotations;
using Services.PersistentProgress;
using Unity.Mathematics;
using UnityEngine;

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

        [CanBeNull]
        public GameObject SpawnEntity(EntityConfig heroConfig, float3 position)
        {
            var gameObject = _assetProvider.Instantiate(heroConfig.Prefab, position);

            RegisterProgressWatchers(gameObject);

            return gameObject;
        }

        public void CleanUp()
        {
            ProgressReaders.Clear();
            ProgressWriters.Clear();
        }

        private void RegisterProgressWatchers(GameObject gameObject)
        {
            foreach (var progressReader in gameObject.GetComponentsInChildren<ISavedProgressReader>())
                Register(progressReader);
        }

        private void Register(ISavedProgressReader progressReader)
        {
            if (progressReader is ISavedProgressWriter progressWriter)
                ProgressWriters.Add(progressWriter);

            ProgressReaders.Add(progressReader);
        }
    }
}
