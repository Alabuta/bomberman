using Configs.Level;
using UnityEngine;

namespace Configs.Enemy
{
    public enum EnemyAILevel
    {
        Dumb,
        Average,
        Dangerous
    }

    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Configs/Enemy/Enemy Config", order = 4)]
    public sealed class EnemyConfig : ScriptableObject
    {
        [Header("General Configs")]
        public string Name;
        public float MaxHealth;
        public float MovementSpeed;

        public EnemyAILevel AILevel;

        [Header("Tiles")]
        public TileConfig[] FordableTiles;

        [Header("Assets Configs")]
        public GameObject Prefab;
    }
}
