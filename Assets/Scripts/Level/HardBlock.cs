using System.Linq;
using Configs.Game.Colliders;
using Configs.Level.Tile;
using Game.Colliders;
using UnityEngine;
using Component = Game.Components.Component;

namespace Level
{
    public class HardBlock : ITileLoad
    {
        public int LayerMask { get; }

        public Component[] Components { get; protected set; }
        public GameObject DestroyEffectPrefab => null;

        public HardBlock(HardBlockConfig config)
        {
            LayerMask = config.LayerMask;

            var collider = new BoxColliderComponent2(config.Collider as BoxColliderComponentConfig);
            Components = new Component[]
            {
                collider
            };
        }

        public bool TryGetComponent<T>(out T component) where T : Component
        {
            component = Components.OfType<T>().FirstOrDefault();
            return component != default;
        }
    }
}
