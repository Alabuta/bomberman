using Configs.Items;

namespace Items
{
    public abstract class Item<TConfig> : IItem where TConfig : ItemConfig
    {
        public ItemConfig ItemConfig { get; }

        protected Item(TConfig itemConfig)
        {
            ItemConfig = itemConfig;
        }
    }
}
