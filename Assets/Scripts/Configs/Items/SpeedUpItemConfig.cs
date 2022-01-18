using Entity;
using UnityEngine;

namespace Configs.Items
{
    [CreateAssetMenu(fileName = "SpeedUpItemConfig", menuName = "Configs/Items/Speed Up")]
    public sealed class SpeedUpItemConfig : ItemConfigBase
    {
        [SerializeField]
        private float SpeedIncreaseValue = 1;

        public override void ApplyTo(IHero hero) => hero.Speed += SpeedIncreaseValue;
    }
}
