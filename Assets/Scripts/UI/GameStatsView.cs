using System;
using Configs.Entity;
using Entity.Hero;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class GameStatsView : MonoBehaviour
    {
        [Header("General")]
        [SerializeField]
        private TextMeshProUGUI LevelStageTimeText;

        [Header("Hero")]
        [SerializeField]
        private Image HeroIcon;

        [SerializeField]
        private TextMeshProUGUI HeroHealthText;

        private Health _health;

        public void Construct(Hero hero)
        {
            UpdateLevelStageTimer(0);

            var entityConfig = (HeroConfig) hero.EntityConfig;

            _health = hero.Health;

            SetHeroIcon(entityConfig.Icon);
            SetHeroHealth(_health.Current);

            _health.HealthChangedEvent += SetHeroHealth;
        }

        private void OnDestroy()
        {
            _health.HealthChangedEvent -= SetHeroHealth;
        }

        public void UpdateLevelStageTimer(double timer)
        {
            if (LevelStageTimeText != null)
                LevelStageTimeText.SetText(TimeSpan.FromSeconds(timer).ToString("m':'ss"));
        }

        private void SetHeroIcon(Sprite sprite)
        {
            if (HeroIcon != null)
                HeroIcon.sprite = sprite;
        }

        private void SetHeroHealth(int health)
        {
            if (HeroHealthText != null)
                HeroHealthText.SetText(health.ToString());
        }
    }
}
