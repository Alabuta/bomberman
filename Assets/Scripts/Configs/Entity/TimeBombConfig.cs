using UnityEngine;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "TimeBomb", menuName = "Configs/Entity/Time Bomb")]
    public class TimeBombConfig : BombConfig
    {
        public int LifetimeSec = 3;
    }
}
