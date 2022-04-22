using Configs.Game.Colliders;
using Configs.Items;
using Game.Colliders;
using Game.Components;
using Game.Items;
using Level;

namespace Items
{
    public abstract class Item<TConfig> : ITileLoad, IItem where TConfig : ItemConfig
    {
        public ItemConfig ItemConfig { get; }

        public ItemController Controller { get; set; }

        public int LayerMask { get; }

        public Component[] Components { get; protected set; }

        protected Item(TConfig itemConfig, ItemController controller)
        {
            ItemConfig = itemConfig;
            Controller = controller;

            LayerMask = itemConfig.LayerMask;

            ColliderComponent collider = itemConfig.Collider switch
            {
                BoxColliderComponentConfig config => new BoxColliderComponent(config),
                CircleColliderComponentConfig config => new CircleColliderComponent(config),
                _ => null
            };

            Components = new Component[]
            {
                collider
            };
        }
    }
}
