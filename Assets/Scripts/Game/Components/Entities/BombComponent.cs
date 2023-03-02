using Math.FixedPointMath;

namespace Game.Components.Entities
{
    public readonly struct BombComponent
    {
        public readonly ulong BlastWorldTick;
        public readonly fix BlastDamage;
        public readonly int BlastRadius;

        public BombComponent(ulong blastWorldTick, fix blastDamage, int blastRadius)
        {
            BlastWorldTick = blastWorldTick;
            BlastDamage = blastDamage;
            BlastRadius = blastRadius;
        }
    }
}
