using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCore.Items;

namespace GameCore.UI
{
    public class ItemInfoPopup : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private Image rarityBG;   // sprite stays in prefab, we only tint it
        [SerializeField] private TMP_Text nameText;

        [Header("Stats")]
        [SerializeField] private TMP_Text powerText;
        [SerializeField] private TMP_Text atkText;
        [SerializeField] private TMP_Text hpText;


        private void Awake()
        {

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        public void Show(ItemDef item)
        {
            if (item == null) return;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            // Icon
            if (itemIcon != null)
            {
                itemIcon.enabled = (item.Icon != null);
                itemIcon.sprite = item.Icon;
                // не трогаем itemIcon.color
            }

            // Rarity BG (tint only, sprite stays from prefab)
            if (rarityBG != null)
            {
                var c = item.RarityColor;
                rarityBG.enabled = (c.a > 0.001f);
                if (rarityBG.enabled)
                    rarityBG.color = c;
            }

            if (nameText) nameText.text = item.DisplayName;

            if (powerText) powerText.text = item.Power.ToString();

            // digits only
            if (atkText) atkText.text = item.Stats.Atk.ToString();
            if (hpText) hpText.text = item.Stats.Hp.ToString();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
