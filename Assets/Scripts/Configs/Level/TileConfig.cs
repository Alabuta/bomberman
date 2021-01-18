using UnityEngine;

namespace Configs.Level
{
    [CreateAssetMenu(fileName = "TileConfig", menuName = "Configs/Level/Tile Config", order = 2)]
    public sealed class TileConfig : ScriptableObject
    {
        public float MovementSpeedMultiplier;
        public float Strengh;

        public GameObject Prefab;
    }
}
