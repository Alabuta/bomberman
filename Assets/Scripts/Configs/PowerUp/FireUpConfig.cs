using Entity;
using UnityEngine;

namespace Configs.PowerUp
{
    [CreateAssetMenu(fileName = "FireUpConfig", menuName = "Configs/Power Up Items/Fire Up Config")]
    public class FireUpConfig : PowerUpConfigBase
    {
        [SerializeField]
        private int BlastRadiusIncreaseValue = 1;

        public override void ApplyTo(IPlayer player) => player.BlastRadius += BlastRadiusIncreaseValue;
    }
}
