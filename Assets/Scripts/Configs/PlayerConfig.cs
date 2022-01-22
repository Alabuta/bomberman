using Configs.Entity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Configs
{
    [CreateAssetMenu(menuName = "Configs/Player", fileName = "Player")]
    public class PlayerConfig : ConfigBase
    {
        public BombermanConfig HeroConfig;

        public GameObject PlayerInputHolder;
    }
}
