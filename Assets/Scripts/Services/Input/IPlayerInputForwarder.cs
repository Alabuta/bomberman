using System;
using Unity.Mathematics;

namespace Input
{
    public interface IPlayerInputForwarder
    {
        event Action<float2> OnMoveEvent;
        event Action OnBombPlantEvent;
    }
}
