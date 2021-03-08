using GameEntities;
using UnityEngine;

namespace Configs.Items
{
    public abstract class ItemConfigBase : ScriptableObject
    {
        public string EntityTag;

        public GameObject Prefab;

        public abstract void ApplyTo(IPlayer player);
    }
}
