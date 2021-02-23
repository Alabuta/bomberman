using Entity;
using UnityEngine;

namespace Configs.PowerUp
{
    [CreateAssetMenu(fileName = "BombUpConfig", menuName = "Configs/Power Up Items/Bomb Up Config")]
    public class BombUpConfig : PowerUpConfigBase
    {
        [SerializeField]
        private int BombCapacityIncreaseValue = 1;

        public override void ApplyTo(IPlayer player) => player.BombCapacity += BombCapacityIncreaseValue;
    }
}
