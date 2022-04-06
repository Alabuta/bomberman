using System;
using DG.Tweening;
using UnityEngine;

namespace UI
{
    public class LoadingScreenController : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup CanvasGroup;

        [SerializeField]
        private Animation CanvasGroupAnimation;

        private Sequence _sequence;

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        public void Show()
        {
            gameObject.SetActive(true);

            if (CanvasGroup != null)
                CanvasGroup.alpha = 1;
        }

        public void Hide(Action callback)
        {
            FadeIn(callback);
        }

        private void FadeIn(Action callback)
        {
            if (_sequence?.IsActive() ?? false)
                _sequence?.Complete();

            _sequence?.Kill();

            _sequence = DOTween.Sequence()
                .AppendCallback(() => { CanvasGroupAnimation.Play(CanvasGroupAnimation.clip.name); })
                .AppendInterval(CanvasGroupAnimation.clip.length)
                .AppendCallback(() =>
                {
                    CanvasGroupAnimation.Stop();

                    gameObject.SetActive(false);

                    callback?.Invoke();
                });
        }
    }
}
