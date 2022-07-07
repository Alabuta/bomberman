using System;
using System.Threading.Tasks;
using Configs.Entity;
using Game;
using Game.Components.Entities;
using Game.Components.Tags;
using Infrastructure.Factory;
using Leopotam.Ecs;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
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

        private EcsEntity _heroEntity;

        public async Task
            Construct(IGameFactory gameFactory, double timer, EcsEntity heroEntity) // :TODO: get IGameFactory from DI
        {
            UpdateLevelStageTimer(timer);

            Assert.IsTrue(heroEntity.Has<HeroTag>());
            var heroComponent = heroEntity.Get<EntityComponent>();

            var heroConfig = (HeroConfig) heroComponent.Config;
            _heroEntity = heroEntity;

            var spriteLoadTask = gameFactory.LoadAssetAsync<Sprite>(heroConfig.Icon);

            SetHeroHealth();

            /*hero.DeathEvent += OnHeroDeathEvent; :TODO: fix
            _heroEntity.Health.HealthChangedEvent += SetHeroHealth;*/

            SetHeroIcon(await spriteLoadTask);
        }

        public void UpdateLevelStageTimer(double timer)
        {
            if (LevelStageTimeText != null)
                LevelStageTimeText.SetText(TimeSpan.FromSeconds(timer).ToString(TimeFormat));
        }

        private void OnDestroy()
        {
            /*if (_heroEntity != null) :TODO: fix
                _heroEntity.Health.HealthChangedEvent -= SetHeroHealth;*/
        }

        private void SetHeroIcon(Sprite sprite)
        {
            if (HeroIcon != null)
                HeroIcon.sprite = sprite;
        }

        private void SetHeroHealth()
        {
            /*if (HeroHealthText != null) :TODO: fix
                HeroHealthText.SetText(_heroEntity.Health.Current.ToString());*/
        }

        private void OnHeroDeathEvent(IEntity hero)
        {
            /*if (_heroEntity != hero) :TODO: fix
                return;

            _heroEntity.Health.HealthChangedEvent -= SetHeroHealth;
            _heroEntity = null;*/
        }
    }
}
