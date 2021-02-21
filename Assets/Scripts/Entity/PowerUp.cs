using Configs.PowerUp;
using UnityEngine;

namespace Entity
{
    public class PowerUp : MonoBehaviour
    {
        public PowerUpEffectConfig[] effects;

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                // effect.ApplyTo(other.gameObject);
            }
        }
    }
}
