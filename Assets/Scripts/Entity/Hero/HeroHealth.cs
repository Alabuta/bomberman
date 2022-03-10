using System;
using Data;
using Infrastructure.Services.PersistentProgress;
using Unity.Mathematics;

namespace Entity.Hero
{
    public class HeroHealth : ISavedProgressWriter
    {
        private HealthState _heroHealthState;

        public Action OnHealthChangedEvent;

        public int Current
        {
            get => _heroHealthState.CurrentHp;
            set
            {
                if (_heroHealthState.CurrentHp == value)
                    return;

                _heroHealthState.CurrentHp = value;

                OnHealthChangedEvent?.Invoke();
            }
        }

        public int Max
        {
            get => _heroHealthState.MaxHp;
            set => _heroHealthState.MaxHp = value;
        }

        public void LoadProgress(PlayerProgress progress)
        {
            _heroHealthState = progress.HeroHealthState;
        }

        public void UpdateProgress(PlayerProgress progress)
        {
            progress.HeroHealthState.CurrentHp = Current;
            progress.HeroHealthState.MaxHp = Max;
        }

        public void ApplyDamage(int damage)
        {
            Current = math.max(0, Current - damage);
            // :TODO: Damage apply animation
        }
    }
}
