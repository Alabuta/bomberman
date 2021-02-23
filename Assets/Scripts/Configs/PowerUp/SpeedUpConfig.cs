using Entity;
using UnityEngine;

namespace Configs.PowerUp
{
    [CreateAssetMenu(fileName = "SpeedUpConfig", menuName = "Configs/Power Up Items/SpeedIncreaseValue Up Config")]
    public sealed class SpeedUpConfig : PowerUpConfigBase
    {
        [SerializeField]
        private float SpeedIncreaseValue = 1;

        public override void ApplyTo(IPlayer player) => player.Speed += SpeedIncreaseValue;
    }
}
