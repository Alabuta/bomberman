using Configs.Game;
using Configs.Game.Colliders;
using Configs.Level.Tile;
using Game.Colliders;
using Game.Components;

namespace Level
{
    public class HardBlock : ITileLoad
    {
        public GameTagConfig GameTag { get; }

        public Component[] Components { get; protected set; }

        public HardBlock(HardBlockConfig config)
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
