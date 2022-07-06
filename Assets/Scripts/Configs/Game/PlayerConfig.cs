using Configs.Entity;
using UnityEngine;

namespace Configs.Game
{
    [CreateAssetMenu(menuName = "Configs/Player", fileName = "Player")]
    public class PlayerConfig : ConfigBase
    {
        public PlayerTagConfig PlayerTagConfig;

        public HeroConfig HeroConfig;

        public GameObject PlayerInputHolder;
    }
}
