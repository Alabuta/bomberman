using Entity;
using UnityEngine;

namespace Configs.Items
{
    [CreateAssetMenu(fileName = "BombUpItemConfig", menuName = "Configs/Items/Bomb Up")]
    public class BombUpItemConfig : ItemConfigBase
    {
        [SerializeField]
        private int BombCapacityIncreaseValue = 1;

        public override void ApplyTo(IPlayer player) => player.BombCapacity += BombCapacityIncreaseValue;
    }
}
