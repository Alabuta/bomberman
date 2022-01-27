using Unity.Mathematics;
using UnityEngine;

namespace Infrastructure.AssetManagement
{
    public interface IAssetProvider
    {
        GameObject Instantiate(GameObject prefab, float3 position);

        GameObject Instantiate(string path);
    }
}
