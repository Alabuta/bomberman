using System;

namespace Entity
{
    public interface IPickUpItem
    {
        event Action<PickUpItem> ItemEffectAppliedEvent;
    }
}
