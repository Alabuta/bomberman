using Entity;
using UnityEngine;

namespace Configs.Items
{
    [CreateAssetMenu(fileName = "SpeedUpItemConfig", menuName = "Configs/Items/Speed Up")]
    public sealed class SpeedUpItemConfig : PowerUpItemConfigBase
    {
        [SerializeField]
        private float SpeedIncreaseValue = 1;

        public override void ApplyTo(IPlayer player) => player.Speed += SpeedIncreaseValue;
    }
}
