using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(MenuEntryAnimator))]
    public class MenuEntryController : MonoBehaviour
    {
        [SerializeField]
        private Image PointerImage;

        [SerializeField]
        private MenuEntryAnimator MenuEntryAnimator;

        public void SetPointed(bool isPointed)
        {
            if (PointerImage != null)
                PointerImage.gameObject.SetActive(isPointed);
        }

        public void SubmitAndPlayAnimation(Action callback)
        {
            MenuEntryAnimator.StartAnimation(callback);
        }
    }
}
