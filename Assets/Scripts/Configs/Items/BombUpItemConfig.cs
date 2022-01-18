using Entity;
using UnityEngine;

namespace Configs.Items
{
    [CreateAssetMenu(fileName = "BombUpItemConfig", menuName = "Configs/Items/Bomb Up")]
    public class BombUpItemConfig : ItemConfigBase
    {
        [SerializeField]
        private int BombCapacityIncreaseValue = 1;

        public override void ApplyTo(IHero hero) => hero.BombCapacity += BombCapacityIncreaseValue;
    }
}
