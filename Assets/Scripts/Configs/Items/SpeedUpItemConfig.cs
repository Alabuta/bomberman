using Entity.Hero;
using UnityEngine;

namespace Configs.Items
{
    [CreateAssetMenu(fileName = "SpeedUpItemConfig", menuName = "Configs/Items/Speed Up Item")]
    public sealed class SpeedUpItemConfig : ItemConfigBase
    {
        [SerializeField]
        private float MultiplierValue = 1;

        public override void ApplyTo(HeroController hero)
        {
            // hero.SpeedMultiplier = MultiplierValue;
        }
    }
}
