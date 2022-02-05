using Configs.Entity;
using Configs.Game;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(menuName = "Configs/Player", fileName = "Player")]
    public class PlayerConfig : ConfigBase
    {
        public PlayerTagConfig PlayerTagConfig;

        public HeroConfig HeroConfig;

        public GameObject PlayerInputHolder;
    }
}
