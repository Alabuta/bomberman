using Configs.Effects;
using Math.FixedPointMath;
using UnityEngine;

namespace Game.Components.Entities
{
    public readonly struct BombComponent
    {
        public readonly ulong BlastWorldTick;
        public readonly fix BlastDamage;
        public readonly int BlastRadius;
        public readonly BlastEffectConfig BlastEffect;

        public BombComponent(ulong blastWorldTick, fix blastDamage, int blastRadius, BlastEffectConfig blastEffect)
        {
            BlastWorldTick = blastWorldTick;
            BlastDamage = blastDamage;
            BlastRadius = blastRadius;
            BlastEffect = blastEffect;
        }
    }
}
