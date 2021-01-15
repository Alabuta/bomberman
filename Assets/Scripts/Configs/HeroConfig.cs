using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "HeroConfig", menuName = "Configs/Hero Config", order = 1)]
    public sealed class HeroConfig : ScriptableObject
    {
        [Header("Hero Movement Configs"), Range(0f, 10f)]
        public float SpeedValue;

        [Range(1f, 3f)]
        public float SpeedBoostMultiplier;

        [Header("Hero Health Configs"), Range(0, 5)]
        public int MaxHealtsPoints;
    }
}
