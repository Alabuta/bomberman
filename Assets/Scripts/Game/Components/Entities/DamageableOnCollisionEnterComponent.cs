using Math.FixedPointMath;

namespace Game.Components.Entities
{
    public readonly struct DamageableOnCollisionEnterComponent
    {
        public readonly fix HurtRadius;

        public DamageableOnCollisionEnterComponent(fix hurtRadius)
        {
            HurtRadius = hurtRadius;
        }
    }
}
