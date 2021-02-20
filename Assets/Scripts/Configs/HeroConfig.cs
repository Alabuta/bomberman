using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "HeroConfig", menuName = "Configs/Hero Config")]
    public sealed class HeroConfig : ScriptableObject
    {
        [Header("Movement Parameters"), Range(0f, 10f)]
        public float Speed;

        [Header("Hero Health Parameters"), Range(0, 5)]
        public int MaxHealtsPoints;
    }
}
