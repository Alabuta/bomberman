using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HUDView : MonoBehaviour
    {
        [SerializeField]
        private Image HeroIcon;

        [SerializeField]
        private TextMeshProUGUI HeroHpText;

        public void SetHeroIcon(Sprite sprite)
        {
            if (HeroIcon != null)
                HeroIcon.sprite = sprite;
        }

        public void SetHeroHp(int hp)
        {
            if (HeroHpText != null)
                HeroHpText.SetText(hp.ToString());
        }
    }
}
