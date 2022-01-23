using Configs.Entity;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(menuName = "Configs/Player", fileName = "Player")]
    public class PlayerConfig : ConfigBase
    {
        public HeroConfig HeroConfig;

        public GameObject PlayerInputHolder;
    }
}
