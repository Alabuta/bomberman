using Configs.Items;
using Game.Items;

namespace Items
{
    public abstract class Item<TConfig> : IItem where TConfig : ItemConfig
    {
        public ItemConfig ItemConfig { get; }

        public ItemController Controller { get; set; }

        protected Item(TConfig itemConfig, ItemController controller)
        {
            ItemConfig = itemConfig;
            Controller = controller;
        }
    }
}
