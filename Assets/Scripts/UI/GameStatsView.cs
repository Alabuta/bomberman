using System;
using Configs.Entity;
using Game;
using Game.Hero;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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

            Load<Sprite>(entityConfig.Icon, SetHeroIcon);

            // SetHeroIcon(entityConfig.Icon);
            SetHeroHealth();

            _health.HealthChangedEvent += SetHeroHealth;
        }

        public void UpdateLevelStageTimer(double timer)
        {
            if (LevelStageTimeText != null)
                LevelStageTimeText.SetText(TimeSpan.FromSeconds(timer).ToString("m':'ss"));
        }

        private static async void Load<T>(AssetReference reference, Action<T> callback)
        {
            var handle = reference.LoadAssetAsync<T>();
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
                return;

            callback?.Invoke(handle.Result);

            // Addressables.Release(handle); :TODO:
        }

        private void OnDestroy()
        {
            _health.HealthChangedEvent -= SetHeroHealth;
        }

        private void SetHeroIcon(Sprite sprite)
        {
            if (HeroIcon != null)
                HeroIcon.sprite = sprite;
        }

        private void SetHeroHealth()
        {
            if (HeroHealthText != null)
                HeroHealthText.SetText(_health.Current.ToString());
        }
    }
}
