using System;
using Data;
using Unity.Mathematics;

namespace Entity.Hero
{
    public class HeroHealth/* : ISavedProgressWriter*/
    {
        private readonly HealthState _heroHealthState;

        public Action<int> HealthChangedEvent;

        public HeroHealth(int health)
        {
            _heroHealthState = new HealthState
            {
                CurrentHealth = health,
                MaxHealth = health
            };
        }

        public int Current
        {
            get => _heroHealthState.CurrentHealth;
            set
            {
                if (_heroHealthState.CurrentHealth == value)
                    return;

                _heroHealthState.CurrentHealth = value;

                HealthChangedEvent?.Invoke(value);
            }
        }

        public int Max
        {
            get => _heroHealthState.MaxHealth;
            set => _heroHealthState.MaxHealth = value;
        }

        /*public void LoadProgress(PlayerProgress progress)
        {
            _heroHealthState = progress.HeroHealthState;

            OnHealthChangedEvent?.Invoke();
        }

        public void UpdateProgress(PlayerProgress progress)
        {
            progress.HeroHealthState.CurrentHealth = Current;
            progress.HeroHealthState.MaxHealth = Max;
        }*/

        public void ApplyDamage(int damage)
        {
            Current = math.max(0, Current - damage);
            // :TODO: Damage apply animation
        }
    }
}
