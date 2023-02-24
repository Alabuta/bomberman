using System;
using Unity.Mathematics;

namespace Input
{
    public interface IPlayerInputProvider
    {
        event Action<IPlayerInputProvider, float2> OnMoveActionEvent;
        event Action<IPlayerInputProvider> OnBombPlantActionEvent;
        event Action<IPlayerInputProvider> OnBombBlastActionEvent;
    }
}
