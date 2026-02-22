using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    public class StageRewardSlotView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amountText;

        public void Bind(Sprite backgroundIcon,Sprite rewardIcon, int amount)
        {
            if (icon != null)
            {
                icon.enabled = (rewardIcon != null);
                icon.sprite = rewardIcon;
            }

            if (background != null)
            {
                background.enabled = (backgroundIcon != null);
                background.sprite = backgroundIcon;
            }

            if (amountText != null)
            {
                amountText.text = amount > 1 ? amount.ToString() : ""; // если 1 Ч можно скрыть
            }
        }
    }
}