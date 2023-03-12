using Math.FixedPointMath;

namespace Game.Components.Events
{
    public readonly struct DamageApplyEventComponent
    {
        public readonly fix DamageValue;

        public DamageApplyEventComponent(fix damageValue)
        {
            DamageValue = damageValue;
        }
    }
}
