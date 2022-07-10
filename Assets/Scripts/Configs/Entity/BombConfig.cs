using Unity.Mathematics;
using UnityEngine;

namespace Configs.Entity
{
    public abstract class BombConfig : EntityConfig
    {
        [Space]
        public DamageParameters DamageParameters;

        [Space]
        public int2[] BombBlastDirections =
        {
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1)
        };
    }
}
