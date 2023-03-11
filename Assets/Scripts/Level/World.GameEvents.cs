using System.Collections.Generic;
using System.Linq;
using Configs.Effects;
using Configs.Entity;
using Game;
using Game.Components;
using Game.Components.Entities;
using Game.Components.Events;
using Game.Components.Tags;
using Leopotam.Ecs;
using Math.FixedPointMath;
using UnityEngine.Assertions;

namespace Level
{
    public partial class World
    {
        public void HeroHasDied(EcsEntity entity)
        {
            var (playerInputProvider, _) =
                _playerInputProviders.FirstOrDefault(pi => pi.Value.HeroEntity == entity);

            if (playerInputProvider != null)
                _playersInputHandlerSystem.UnsubscribePlayerInputProvider(playerInputProvider);
        }

        public void CreatePlayerBombPlantAction(IPlayer player)
        {
            var heroEntity = player.HeroEntity;
            Assert.IsTrue(heroEntity.Has<HeroTag>());
            Assert.IsTrue(heroEntity.Has<TransformComponent>());

            var heroComponent = heroEntity.Get<EntityComponent>();
            var heroConfig = heroComponent.Config as HeroConfig;
            Assert.IsNotNull(heroConfig);

            // :TODO: use player current parameters instead of static data
            var bombBlastDamage = (fix) heroConfig.BombBlastDamage;
            var bombBlastRadius = heroConfig.BombBlastRadius;
            var bombBlastDelay = player.HasRemoConBomb ? new fix(-1) : (fix) heroConfig.BombBlastDelay;

            ref var transformComponent = ref heroEntity.Get<TransformComponent>();

            var coordinate = LevelTiles.ToTileCoordinate(transformComponent.WorldPosition);
            var position = LevelTiles.ToWorldPosition(coordinate);

            var eventEntity = _ecsWorld.NewEntity();
            eventEntity.Replace(new OnBombPlantActionEventComponent(
                playerTag: player.PlayerTag,
                position: position,
                bombConfig: heroConfig.BombConfig,
                blastDelay: bombBlastDelay,
                bombBlastDamage: bombBlastDamage,
                bombBlastRadius: bombBlastRadius
            ));
        }

        public void CreatePlayerBombBlastAction(IPlayer player)
        {
            var eventEntity = _ecsWorld.NewEntity();
            eventEntity.Replace(new OnBombBlastActionEventComponent(playerTag: player.PlayerTag));
        }

        public void InstantiateBlastEffect(IReadOnlyList<int> blastRadiusInDirections, int blastRadius, fix2 position,
            BlastEffectConfig blastEffectConfig)
        {
            var go = _gameFactory.InstantiatePrefab(blastEffectConfig.Prefab, fix2.ToXY(position));
            Assert.IsNotNull(go);

            var effectController = go.GetComponent<BlastEffectController>();
            Assert.IsNotNull(effectController);

            effectController.Construct(blastRadius, blastRadiusInDirections);

            var effectAnimator = go.GetComponent<EffectAnimator>();
            Assert.IsNotNull(effectAnimator);

            effectAnimator.OnAnimationStateEnter += state =>
            {
                if (state == AnimatorState.Finish)
                    go.SetActive(false);
            };
        }

        // :TODO: add an item pick up event handler
        // :TODO: add an entity death event handler
    }
}
