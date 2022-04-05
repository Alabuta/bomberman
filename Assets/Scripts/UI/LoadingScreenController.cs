using System.Collections;
using TMPro;
using UnityEngine;

namespace UI
{
    public class LoadingScreenController : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup CanvasGroup;

        [SerializeField]
        private TextMeshProUGUI LevelAndStageText;

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        public void Show(int level, int stage)
        {
            gameObject.SetActive(true);

            if (LevelAndStageText != null)
                LevelAndStageText.text = $"{level}<color=#F6E500><voffset=0.2em>‒</voffset></color>{stage}";

            if (CanvasGroup != null)
                CanvasGroup.alpha = 1;
        }

        public void Hide()
        {
            StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            if (CanvasGroup == null)
            {
                gameObject.SetActive(false);
                yield break;
            }

            while (CanvasGroup.alpha < 0)
            {
                CanvasGroup.alpha -= .04f;
                yield return new WaitForSeconds(0.04f);
            }

            gameObject.SetActive(false);
        }
    }
}
