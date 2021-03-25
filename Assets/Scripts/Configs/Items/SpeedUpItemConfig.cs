using GameEntities;
using UnityEngine;

namespace Configs.Items
{
    [CreateAssetMenu(fileName = "SpeedUpItemConfig", menuName = "Configs/Items/Speed Up")]
    public sealed class SpeedUpItemConfig : ItemConfigBase
    {
        [SerializeField]
        private float SpeedIncreaseValue = 1;

        public override void ApplyTo(IPlayer player) => player.Speed += SpeedIncreaseValue;
    }
}
