using Math.FixedPointMath;

namespace Game.Components.Entities
{
    public readonly struct TimeBomb
    {
        public readonly ulong BlastWorldTick;
        public readonly fix BlastDamage;
        public readonly int BlastRadius;

        public TimeBomb(ulong blastWorldTick, fix blastDamage, int blastRadius)
        {
            BlastWorldTick = blastWorldTick;
            BlastDamage = blastDamage;
            BlastRadius = blastRadius;
        }
    }

    public readonly struct RemoConBomb
    {
        public readonly fix BlastDamage;
        public readonly int BlastRadius;

        public RemoConBomb(fix blastDamage, int blastRadius)
        {
            BlastDamage = blastDamage;
            BlastRadius = blastRadius;
        }
    }
}
