using System;
using Data;
using Unity.Mathematics;

namespace Entity
{
    public class Health
    {
        private readonly HealthState _healthState;

        public Action HealthChangedEvent;
        public Action<int> HealthDamagedEvent;

        public Health(int health)
        {
            _healthState = new HealthState
            {
                CurrentHealth = health,
                MaxHealth = health
            };
        }

        public int Current
        {
            get => _healthState.CurrentHealth;
            private set
            {
                if (_healthState.CurrentHealth == value)
                    return;

                _healthState.CurrentHealth = value;

                HealthChangedEvent?.Invoke();
            }
        }

        public int Max
        {
            get => _healthState.MaxHealth;
            set => _healthState.MaxHealth = value;
        }

        public void ApplyDamage(int damage)
        {
            Current = math.max(0, Current - damage);

            HealthDamagedEvent?.Invoke(damage);
        }
    }
}
