using Configs.PowerUp;
using UnityEngine;

namespace Entity
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PowerUp : MonoBehaviour
    {
        [SerializeField]
        private PowerUpConfigBase Effect;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(Effect.EntityTag))
                return;

            var playerController = other.gameObject.GetComponent<PlayerController>();
            Effect.ApplyTo(playerController);
        }
    }
}
