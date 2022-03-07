using System;
using Data;
using Infrastructure.Services.PersistentProgress;
using Unity.Mathematics;

namespace Entity.Hero
{
    public class HeroHealth : ISavedProgressWriter
    {
        private State _heroState;

        public Action OnHealthChangedEvent;

        public int Current
        {
            get => _heroState.CurrentHp;
            set
            {
                if (_heroState.CurrentHp == value)
                    return;

                _heroState.CurrentHp = value;

                OnHealthChangedEvent?.Invoke();
            }
        }

        public int Max
        {
            get => _heroState.MaxHp;
            set => _heroState.MaxHp = value;
        }

        public void LoadProgress(PlayerProgress progress)
        {
            _heroState = progress.HeroState;
        }

        public void UpdateProgress(PlayerProgress progress)
        {
            progress.HeroState.CurrentHp = Current;
            progress.HeroState.MaxHp = Max;
        }

        public void ApplyDamage(int damage)
        {
            Current = math.max(0, Current - damage);
            // :TODO: Damage apply animation
        }
    }
}
