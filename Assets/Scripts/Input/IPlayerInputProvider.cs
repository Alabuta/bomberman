using System;

namespace Input
{
    public interface IPlayerInputProvider
    {
        event Action<PlayerInputAction> OnInputActionEvent;
    }
}
