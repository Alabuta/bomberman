using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class GameStatsViewController : MonoBehaviour
    {
        [SerializeField]
        private Image HeroIcon;

        [SerializeField]
        private TextMeshProUGUI HeroHealthText;

        public void SetHeroIcon(Sprite sprite)
        {
            if (HeroIcon != null)
                HeroIcon.sprite = sprite;
        }

        public void SetHeroHealth(int health)
        {
            if (HeroHealthText != null)
                HeroHealthText.SetText(health.ToString());
        }
    }
}
