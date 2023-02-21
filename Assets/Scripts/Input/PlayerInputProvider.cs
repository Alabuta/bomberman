﻿using System;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
    public struct PlayerInputAction
    {
        public float2 MovementVector;
        public bool BombPlant;
        public bool BombBlast;
    }

    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputProvider : MonoBehaviour, IPlayerInputProvider
    {
        private readonly float2 _horizontalMovementMask = new(1, 0);
        private readonly float2 _verticalMovementMask = new(0, 1);
        private float2 _moveVector;

        public event Action<PlayerInputAction> OnInputActionEvent;

        [UsedImplicitly]
        public void OnMove(InputValue value)
        {
            _moveVector = value.Get<Vector2>();
            _moveVector *= math.select(_horizontalMovementMask, _verticalMovementMask, _moveVector.y != 0);

            OnInputActionEvent?.Invoke(new PlayerInputAction { MovementVector = _moveVector });
        }

        [UsedImplicitly]
        public void OnBombPlant(InputValue value)
        {
            OnInputActionEvent?.Invoke(new PlayerInputAction { MovementVector = _moveVector, BombPlant = true });
        }

        [UsedImplicitly]
        public void OnBombBlast(InputValue value)
        {
            OnInputActionEvent?.Invoke(new PlayerInputAction { MovementVector = _moveVector, BombBlast = true });
        }
    }
}
