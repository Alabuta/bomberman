﻿using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;

namespace Entity
{
    public abstract class EntityController : MonoBehaviour, IEntityController
    {
        [SerializeField]
        [HideInInspector]
        private float3 MovementConvertMask = new(1, 1, 0);

        [SerializeField]
        protected Transform Transform;

        public abstract fix Speed { get; set; }
        public float PlaybackSpeed => EntityAnimator.PlaybackSpeed;

        public abstract int2 Direction { get; set; }

        public fix2 WorldPosition
        {
            get => (fix2) Transform.position;
            set => Transform.position = fix2.ToXY(value);
        }

        protected abstract EntityAnimator EntityAnimator { get; }

        private void Revive()
        {
            EntityAnimator.PlaybackSpeed = 1;
            EntityAnimator.SetAlive();
        }

        public void Kill()
        {
            EntityAnimator.PlaybackSpeed = 1;
            EntityAnimator.SetDead();
        }

        protected virtual void Start()
        {
            Revive();
        }

        private void FixedUpdate()
        {
            Transform.Translate((float3) Direction.xyy * (float) Speed * MovementConvertMask * Time.fixedDeltaTime);
        }

        /*public void Update(fix deltaTime)
        {
            // Transform.Translate(Direction.xyy * Speed * MovementConvertMask * (float) deltaTime);
        }*/
    }
}
