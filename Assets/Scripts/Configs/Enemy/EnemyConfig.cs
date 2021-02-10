using Configs.Level.Tile;
using UnityEngine;

namespace Configs.Enemy
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Configs/Enemy/Enemy Config")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [Header("General Configs")]
        public string Name;
        public float MaxHealth;
        public float MovementSpeed;

        [Header("Tiles")]
        public BlockConfig[] FordableTiles;

        [Header("Assets Configs")]
        public GameObject Prefab;
    }
}
