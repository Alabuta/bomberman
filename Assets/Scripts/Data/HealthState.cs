using System;

namespace Data
{
    [Serializable]
    public class HealthState
    {
        public int CurrentHealth;
        public int MaxHealth;

        public void ResetHealth() =>
            CurrentHealth = MaxHealth;
    }
}
