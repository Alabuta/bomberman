using System;
using Configs.Entity;
using Game;
using Game.Hero;
using Infrastructure.Factory;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace UI
{
    public class GameStatsView : MonoBehaviour
    {
        private const string TimeFormat = "m':'ss";

        [Header("General")]
        [SerializeField]
        private TextMeshProUGUI LevelStageTimeText;

        [Header("Hero")]
        [SerializeField]
        private Image HeroIcon;

        [SerializeField]
        private TextMeshProUGUI HeroHealthText;

        private Hero _hero;

        public void Construct(IGameFactory gameFactory, Hero hero) // :TODO: get IGameFactory from DI
        {
            UpdateLevelStageTimer(0);

            var heroConfig = (HeroConfig) hero.Config;
            _hero = hero;

            OnAssetsLoad(gameFactory, heroConfig.Icon);
            SetHeroHealth();

            hero.DeathEvent += OnHeroDeathEvent;
            _hero.Health.HealthChangedEvent += SetHeroHealth;
        }

        public void UpdateLevelStageTimer(double timer)
        {
            if (LevelStageTimeText != null)
                LevelStageTimeText.SetText(TimeSpan.FromSeconds(timer).ToString(TimeFormat));
        }

        private void OnDestroy()
        {
            if (_hero != null)
                _hero.Health.HealthChangedEvent -= SetHeroHealth;
        }

        private async void OnAssetsLoad(IGameFactory gameFactory, AssetReferenceSprite spriteReference)
        {
            var sprite = await gameFactory.LoadAssetAsync<Sprite>(spriteReference);
            SetHeroIcon(sprite);
        }

        private void SetHeroIcon(Sprite sprite)
        {
            if (HeroIcon != null)
                HeroIcon.sprite = sprite;
        }

        private void SetHeroHealth()
        {
            if (HeroHealthText != null)
                HeroHealthText.SetText(_hero.Health.Current.ToString());
        }

        private void OnHeroDeathEvent(IEntity hero)
        {
            if (_hero != hero)
                return;

            _hero.Health.HealthChangedEvent -= SetHeroHealth;
            _hero = null;
        }
    }
}
