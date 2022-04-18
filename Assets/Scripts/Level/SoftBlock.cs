using Configs.Game;
using Configs.Game.Colliders;
using Configs.Level.Tile;
using Game.Colliders;
using Game.Components;

namespace Level
{
    public class SoftBlock : ITileLoad
    {
        public GameTagConfig GameTag { get; }

        public Component[] Components { get; protected set; }

        public SoftBlock(SoftBlockConfig config)
        {
            GameTag = config.GameTag;

            var collider = new BoxColliderComponent(config.Collider as BoxColliderComponentConfig);
            Components = new Component[]
            {
                collider
            };
        }
    }
}
