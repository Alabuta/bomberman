using System;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputProvider : MonoBehaviour, IPlayerInputProvider
    {
        public event Action<IPlayerInputProvider, float2> OnMoveActionEvent;
        public event Action<IPlayerInputProvider> OnBombPlantActionEvent;
        public event Action<IPlayerInputProvider> OnBombBlastActionEvent;

        [UsedImplicitly]
        public void OnMove(InputValue value)
        {
            OnMoveActionEvent?.Invoke(this, value.Get<Vector2>());
        }

        [UsedImplicitly]
        public void OnBombPlant(InputValue value)
        {
            OnBombPlantActionEvent?.Invoke(this);
        }

        [UsedImplicitly]
        public void OnBombBlast(InputValue value)
        {
            OnBombBlastActionEvent?.Invoke(this);
        }
    }
}
