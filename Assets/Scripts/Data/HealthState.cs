using System;

namespace Data
{
    [Serializable]
    public class HealthState
    {
        public int CurrentHp;
        public int MaxHp;

        public void ResetHp() => CurrentHp = MaxHp;
    }
}
