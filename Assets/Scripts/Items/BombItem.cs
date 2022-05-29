using Configs.Items;
using Game.Items;

namespace Items
{
    public class BombItem : Item<BombItemConfig>
    {
        public BombItemConfig Config { get; }

        public BombItem(BombItemConfig itemConfig, ItemController controller)
            : base(itemConfig, controller)
        {
            Config = itemConfig;
        }
    }
}
