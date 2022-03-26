using System;

namespace Input
{
    public interface IPlayerInput
    {
        event Action<PlayerInputAction> OnInputActionEvent;
    }
}
