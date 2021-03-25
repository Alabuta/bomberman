using Entity;
using UnityEngine;

namespace Configs.Items
{
    public abstract class ItemConfigBase : ScriptableObject
    {
        public string ApplyObjectTag;

        public GameObject Prefab;

        public abstract void ApplyTo(IPlayer player);
    }
}
