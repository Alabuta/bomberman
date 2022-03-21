using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace Infrastructure.AssetManagement
{
    public class AssetProvider : IAssetProvider
    {
        [CanBeNull]
        public GameObject Instantiate(GameObject prefab, float3 position, Transform parent)
        {
            return Object.Instantiate(prefab, position, Quaternion.identity, parent);
        }

        [CanBeNull]
        public GameObject Instantiate(string path)
        {
            var prefab = Resources.Load<GameObject>(path);
            return Object.Instantiate(prefab);
        }
    }
}
