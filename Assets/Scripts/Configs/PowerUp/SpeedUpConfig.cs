using Entity;
using UnityEngine;

namespace Configs.PowerUp
{
    [CreateAssetMenu(fileName = "SpeedUpConfig", menuName = "Configs/Level/Speed Up Config")]
    public class SpeedUpConfig : PowerUpEffectConfig
    {
        public float Speed;

        public override void ApplyTo(IPlayer player) => player.Speed = Speed;
    }
}
