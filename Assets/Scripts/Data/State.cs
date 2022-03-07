using System;

namespace Data
{
    [Serializable]
    public class State
    {
        public int CurrentHp;
        public int MaxHp;

        public void ResetHp() => CurrentHp = MaxHp;
    }
}
