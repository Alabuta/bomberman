using System;
using Configs.Items;
using Entity.Hero;
using UnityEngine;

namespace Entity
{
    public sealed class PickUpItem : MonoBehaviour, IPickUpItem
    {
        [SerializeField]
        private ItemConfigBase ItemConfigBase;

        public event Action<PickUpItem> ItemEffectAppliedEvent;

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

    public class Item : MonoBehaviour
    {
    }
}
