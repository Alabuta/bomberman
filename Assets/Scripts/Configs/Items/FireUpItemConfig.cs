using Entity.Hero;
using UnityEngine;

namespace Configs.Items
{
    [CreateAssetMenu(fileName = "FireUpItem", menuName = "Configs/Items/Fire Up Item")]
    public class FireUpItemConfig : ItemConfigBase
    {
        [SerializeField]
        private int BlastRadiusIncreaseValue = 1;

        public override void ApplyTo(IHero hero) => hero.BlastRadius += BlastRadiusIncreaseValue;
    }
}
