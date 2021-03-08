using System;
using App.Level;
using Configs.Items;
using UnityEngine;

namespace GameEntities
{
    public interface IItem
    {
        event Action<Item> ItemEffectAppliedEvent;
    }

    [RequireComponent(typeof(Collider2D))]
    public sealed class Item : MonoBehaviour, IItem
    {
        public event Action<Item> ItemEffectAppliedEvent;

        private readonly IGameLevelState _gameLevelState;
        private readonly ItemConfigBase _itemConfigBase;

        public Item(IGameLevelState gameLevelState, ItemConfigBase itemConfigBase)
        {
            _gameLevelState = gameLevelState;
            _itemConfigBase = itemConfigBase;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(_itemConfigBase.EntityTag))
                return;

            var playerController = other.gameObject.GetComponent<PlayerController>();
            _itemConfigBase.ApplyTo(playerController);

            ItemEffectAppliedEvent?.Invoke(this);
            Destroy(gameObject); // :TODO: refactor - use OnDestroy event
        }
    }
}
