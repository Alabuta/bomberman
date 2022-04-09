using System;

namespace Game.Items
{
    public interface IPickUpItem
    {
        event Action<PickUpItem> ItemEffectAppliedEvent;
    }
}
