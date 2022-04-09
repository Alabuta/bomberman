using Configs.Items;
using Game.Items;

namespace Items
{
    public class BombItem : Item<BombItemConfig>
    {
        public BombItem(BombItemConfig itemConfig, ItemController controller)
            : base(itemConfig, controller)
        {
        }
    }
}
