using UnityEngine;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "DefaultBomb", menuName = "Configs/Entity/Default Bomb")]
    public class DefaultBombConfig : BombConfig
    {
        public int LifetimeSec = 3;
    }
}
