using Entity;
using UnityEngine;

namespace Configs.Items
{
    [CreateAssetMenu(fileName = "FireUpItemConfig", menuName = "Configs/Items/Fire Up")]
    public class FireUpItemConfig : ItemConfigBase
    {
        [SerializeField]
        private int BlastRadiusIncreaseValue = 1;

        public override void ApplyTo(IHero hero) => hero.BlastRadius += BlastRadiusIncreaseValue;
    }
}
