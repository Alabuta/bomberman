using Configs.Entity;
using Unity.Mathematics;
using UnityEngine;

namespace Infrastructure.Factory
{
    public interface IGameFactory
    {
        GameObject SpawnEntity(EntityConfig heroConfig, float3 position);
    }
}
