using Configs.Entity;
using Infrastructure.AssetManagement;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace Infrastructure.Factory
{
    public class GameFactory : IGameFactory
    {
        private readonly IAssetProvider _assetProvider;

        public GameFactory(IAssetProvider assetProvider)
        {
            _assetProvider = assetProvider;
        }

        [CanBeNull]
        public GameObject SpawnEntity(EntityConfig heroConfig, float3 position)
        {
            return _assetProvider.Instantiate(heroConfig.Prefab, position);
        }
    }
}
