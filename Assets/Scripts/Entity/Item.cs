using Configs.Items;
using UnityEngine;

namespace Entity
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Item : MonoBehaviour
    {
        [SerializeField]
        private ItemConfigBase Effect;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(Effect.EntityTag))
                return;

            var playerController = other.gameObject.GetComponent<PlayerController>();
            Effect.ApplyTo(playerController);

            // GameLevelState?.ItemHasBeenApplied(Effect);
            Destroy(gameObject); // :TODO: refactor - use OnDestroy event
        }
    }
}
