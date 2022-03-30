using System.Collections.Generic;
using System.Linq;
using Configs.Items;
using Items;

namespace Level
{
    public class ItemHolder
    {
        private readonly List<IItem> _items = new();

        public void AddItem(IItem item)
        {
            _items.Add(item);
        }

        public bool HasItemType(ItemConfig itemConfig)
        {
            return _items.Any(i => i.ItemConfig == itemConfig);
        }
    }
}
