namespace Game.Components.Events
{
    public readonly struct AttackEventComponent
    {
        public readonly int DamageValue;

        public AttackEventComponent(int damageValue)
        {
            DamageValue = damageValue;
        }
    }
}
