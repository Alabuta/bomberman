using System;
using UniRx;
using UniRx.Triggers;
using Unity.Mathematics;
using UnityEngine;

namespace GameEntities
{
    [Obsolete]
    public sealed class LocalPlayerInput : MonoBehaviour
    {
        private static readonly float2 HorizontalMovementMask = new float2(1, 0);
        private static readonly float2 VerticalMovementMask = new float2(0, 1);

        private float2 _movementVector = float2.zero;

        private Subject<Unit> _bombPlanted;

        public IObservable<float2> MovementVector { get; private set; }

        public IObservable<Unit> BombPlanted => _bombPlanted;

        private void Awake()
        {
            MovementVector = this.FixedUpdateAsObservable().Select(_ => _movementVector);

            _bombPlanted = new Subject<Unit>().AddTo(this);
        }

        private void Update()
        {
            _movementVector = math.float2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            _movementVector *= math.select(HorizontalMovementMask, VerticalMovementMask, _movementVector.y != 0);

            if (Input.GetButtonDown("Jump"))
                _bombPlanted.OnNext(default);
        }
    }
}
