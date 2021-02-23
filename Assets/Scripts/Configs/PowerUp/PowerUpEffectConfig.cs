using Entity;
using UnityEngine;

namespace Configs.PowerUp
{
    public abstract class PowerUpEffectConfig : ScriptableObject
    {
        public abstract void ApplyTo(IPlayer player);
    }
}
