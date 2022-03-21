using Infrastructure.Services;
using Unity.Mathematics;
using UnityEngine;

namespace Infrastructure.AssetManagement
{
    public interface IAssetProvider : IService
    {
        GameObject Instantiate(GameObject prefab, float3 position, Transform parent = null);

        GameObject Instantiate(string path);
    }
}
