using Entity;
using UnityEngine;

namespace Configs.PowerUp
{
    public abstract class PowerUpConfigBase : ScriptableObject
    {
        public string EntityTag;

        public GameObject Prefab;

        public abstract void ApplyTo(IPlayer player);
    }
}
