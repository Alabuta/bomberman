using System;
using Unity.Mathematics;

namespace Input
{
    public interface IPlayerInput
    {
        event Action<float2> OnMoveEvent;
        event Action OnBombPlantEvent;
        event Action OnBombBlastEvent;
    }
}
