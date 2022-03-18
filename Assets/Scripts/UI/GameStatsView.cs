using Configs.Entity;
using Entity.Hero;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class GameStatsView : MonoBehaviour
    {
        [SerializeField]
        private Image HeroIcon;

        [SerializeField]
        private TextMeshProUGUI HeroHealthText;

        private HeroHealth _heroHealth;

        public void Construct(Hero hero)
        {
            var entityConfig = (HeroConfig) hero.EntityConfig;

            _heroHealth = hero.HeroHealth;

            SetHeroIcon(entityConfig.Icon);
            SetHeroHealth(_heroHealth.Current);

            _heroHealth.HealthChangedEvent += SetHeroHealth;
        }

        private void OnDestroy()
        {
            _heroHealth.HealthChangedEvent -= SetHeroHealth;
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
