using System;
using System.Collections.Generic;
using System.Linq;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Game.Components.Events
{
    public readonly struct OnBombBlastEventComponent
    {
        public readonly fix2 Position;
        public readonly fix BombBlastDamage;
        public readonly int BombBlastRadius;
        public readonly int2[] BombBlastDirections;

        public OnBombBlastEventComponent(fix2 position,
            fix bombBlastDamage,
            int bombBlastRadius,
            IEnumerable<int2> bombBlastDirections)
        {
            Position = position;
            BombBlastDamage = bombBlastDamage;
            BombBlastRadius = bombBlastRadius;
            BombBlastDirections = bombBlastDirections as int2[] ?? bombBlastDirections.ToArray();
        }
    }
}
