using Configs.Game.Colliders;
using Configs.Level.Tile;
using Game.Colliders;
using Game.Components;

namespace Level
{
    public class SoftBlock : TileLoad
    {
        public SoftBlock(SoftBlockConfig config)
        {
            var collider = new BoxColliderComponent(config.Collider as BoxColliderComponentConfig);

            _components = new Component[]
            {
                collider
            };
        }
    }
}
