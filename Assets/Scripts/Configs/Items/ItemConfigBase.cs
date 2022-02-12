using Entity.Hero;
using UnityEngine;

namespace Configs.Items
{
    public abstract class ItemConfigBase : ConfigBase
    {
        public string ApplyObjectTag;

        public GameObject Prefab;

        public abstract void ApplyTo(HeroController hero);
    }
}
