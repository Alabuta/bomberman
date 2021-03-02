using Entity;
using UnityEngine;

namespace Configs.Items
{
    public abstract class PowerUpItemConfigBase : ScriptableObject
    {
        public string EntityTag;

        public GameObject Prefab;

        public abstract void ApplyTo(IPlayer player);
    }
}
