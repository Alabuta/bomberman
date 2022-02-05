using System;
using Configs.Items;
using Entity.Hero;
using Level;
using UnityEngine;
using UnityEngine.Assertions;

namespace Entity
{
    public interface IItem
    {
        event Action<Item> ItemEffectAppliedEvent;
    }

    [RequireComponent(typeof(Collider2D))]
    public sealed class Item : MonoBehaviour, IItem
    {
        [SerializeField]
        private ItemConfigBase ItemConfigBase;

        public event Action<Item> ItemEffectAppliedEvent;

        private readonly IGameLevelState _gameLevelState;

        public Item(IGameLevelState gameLevelState)
        {
            _gameLevelState = gameLevelState;
        }

        private void Awake()
        {
            Assert.IsNotNull(ItemConfigBase, "ItemConfigBase != null");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(ItemConfigBase.ApplyObjectTag))
                return;

            var playerController = other.gameObject.GetComponent<HeroController>();
            if (playerController != null)
                ItemConfigBase.ApplyTo(playerController);

            ItemEffectAppliedEvent?.Invoke(this);
            Destroy(gameObject);// :TODO: refactor - use OnDestroy event
        }
    }
}
