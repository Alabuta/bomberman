using Entity;
using UnityEngine;

namespace Configs.Items
{
    [CreateAssetMenu(fileName = "BombUpItemConfig", menuName = "Configs/Items/Bomb Up")]
    public class BombUpItemConfig : WeaponItemConfig, IBombCapacityChangerItem
    {
        [SerializeField]
        private int BombCapacityIncreaseValue = 1;

        public override void ApplyTo(IHero hero) => hero.BombCapacity += BombCapacityIncreaseValue;

        public int AdditionalValue => BombCapacityIncreaseValue;
        public int MultiplierValue => 1;
    }
}
