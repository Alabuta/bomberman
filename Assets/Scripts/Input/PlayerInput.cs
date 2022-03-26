using System;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
    public struct PlayerInputAction
    {
        public IPlayerInput PlayerInput;
        public float2 MovementVector;
        public bool BombPlant;
        public bool BombBlast;
    }

    [RequireComponent(typeof(UnityEngine.InputSystem.PlayerInput))]
    public class PlayerInput : MonoBehaviour, IPlayerInput
    {
        private readonly float2 _horizontalMovementMask = new(1, 0);
        private readonly float2 _verticalMovementMask = new(0, 1);

        public event Action<PlayerInputAction> OnInputActionEvent;

        [UsedImplicitly]
        public void OnMove(InputValue value)
        {
            var moveVector = (float2) value.Get<Vector2>();
            moveVector *= math.select(_horizontalMovementMask, _verticalMovementMask, moveVector.y != 0);

            OnInputActionEvent?.Invoke(new PlayerInputAction { PlayerInput = this, MovementVector = moveVector });
        }

        [UsedImplicitly]
        public void OnBombPlant(InputValue value)
        {
            OnInputActionEvent?.Invoke(new PlayerInputAction { PlayerInput = this, BombPlant = true });
        }

        [UsedImplicitly]
        public void OnBombBlast(InputValue value)
        {
            OnInputActionEvent?.Invoke(new PlayerInputAction { PlayerInput = this, BombBlast = true });
        }
    }
}
