using System;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
    public struct PlayerInputAction
    {
        public IPlayerInputProvider PlayerInputProvider;
        public float2 MovementVector;
        public bool BombPlant;
        public bool BombBlast;
    }

    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputProvider : MonoBehaviour, IPlayerInputProvider
    {
        public event Action<PlayerInputAction> OnInputActionEvent;

        [UsedImplicitly]
        public void OnMove(InputValue value)
        {
            OnInputActionEvent?.Invoke(new PlayerInputAction
            {
                PlayerInputProvider = this,
                MovementVector = value.Get<Vector2>()
            });
        }

        [UsedImplicitly]
        public void OnBombPlant(InputValue value)
        {
            OnInputActionEvent?.Invoke(new PlayerInputAction
            {
                PlayerInputProvider = this,
                MovementVector = value.Get<Vector2>(),
                BombPlant = true
            });
        }

        [UsedImplicitly]
        public void OnBombBlast(InputValue value)
        {
            OnInputActionEvent?.Invoke(new PlayerInputAction
            {
                PlayerInputProvider = this,
                MovementVector = value.Get<Vector2>(),
                BombBlast = true
            });
        }
    }
}
