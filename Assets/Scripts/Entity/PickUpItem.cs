using System;
using Configs.Items;
using UnityEngine;

namespace Entity
{
    public sealed class PickUpItem : MonoBehaviour, IPickUpItem
    {
        [SerializeField]
        private ItemConfig ItemConfig;

        public event Action<PickUpItem> ItemEffectAppliedEvent;

        /*private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(ItemConfig.ApplyObjectTag))
                return;

            var playerController = other.gameObject.GetComponent<HeroController>();
            if (playerController != null)
                ItemConfig.ApplyTo(playerController);

            ItemEffectAppliedEvent?.Invoke(this);
            Destroy(gameObject);// :TODO: refactor - use OnDestroy event
        }*/
    }
}
