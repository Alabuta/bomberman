using System;
using DG.Tweening;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(Animator))]
    public class MenuEntryAnimator : MonoBehaviour
    {
        private readonly int _animationId = Animator.StringToHash("Base Layer.Pressed State");

        [SerializeField]
        private Animator Animator;

        [SerializeField]
        private AnimationClip AnimationClip;

        private Sequence _sequence;

        public void StartAnimation(Action callback)
        {
            if (_sequence?.IsActive() ?? false)
                _sequence?.Complete();

            _sequence?.Kill();

            _sequence = DOTween.Sequence()
                .AppendCallback(() => Animator.Play(_animationId, 0, 0))
                .AppendInterval(AnimationClip.length)
                .AppendCallback(() =>
                {
                    ResetAnimation();

                    callback?.Invoke();
                });
        }

        private void ResetAnimation()
        {
            Animator.StopPlayback();
        }
    }
}
