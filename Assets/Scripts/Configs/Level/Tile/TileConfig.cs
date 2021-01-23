using UnityEngine;

namespace Configs.Level.Tile
{
    [CreateAssetMenu(fileName = "TileConfig", menuName = "Configs/Level/Tile Config", order = 2)]
    public class TileConfig : ScriptableObject
    {
        public float MovementSpeedMultiplier;
        public float Strengh;

        public GameObject Prefab;
    }
}
