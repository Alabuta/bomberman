using System;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
    [RequireComponent(typeof(UnityEngine.InputSystem.PlayerInput))]
    public class PlayerInput : MonoBehaviour, IPlayerInput
    {
        private readonly float2 _horizontalMovementMask = new(1, 0);
        private readonly float2 _verticalMovementMask = new(0, 1);

        public event Action<float2> OnMoveEvent;

        public event Action OnBombPlantEvent;
        public event Action OnBombBlastEvent;

        [UsedImplicitly]
        public void OnMove(InputValue value)
        {
            var moveVector = (float2) value.Get<Vector2>();
            moveVector *= math.select(_horizontalMovementMask, _verticalMovementMask, moveVector.y != 0);

            OnMoveEvent?.Invoke(moveVector);
        }

        [UsedImplicitly]
        public void OnBombPlant(InputValue value)
        {
            OnBombPlantEvent?.Invoke();
        }

        [UsedImplicitly]
        public void OnBombBlast(InputValue value)
        {
            OnBombBlastEvent?.Invoke();
        }
    }
}
