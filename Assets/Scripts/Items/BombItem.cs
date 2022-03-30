using Configs.Items;

namespace Items
{
    public class BombItem : Item<BombItemConfig>
    {
        public BombItem(BombItemConfig itemConfig)
            : base(itemConfig)
        {
        }
    }
}
