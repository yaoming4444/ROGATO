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

        [Header("Level")]
        [SerializeField] private TMP_Text levelText; // NEW: separate level text (e.g. "LVL 5")

        [Header("Stats")]
        [SerializeField] private TMP_Text powerText;
        [SerializeField] private TMP_Text atkText;
        [SerializeField] private TMP_Text hpText;

        private void Awake()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        /// <summary>
        /// Backward compatible: shows preview level from ScriptableObject (legacy/preview only).
        /// Prefer Show(item, itemLevel) from gameplay.
        /// </summary>
        public void Show(ItemDef item)
        {
            if (item == null) return;
            Show(item, item.Level); // preview only
        }

        /// <summary>
        /// Main method: shows item with explicit level (core instance level).
        /// Uses Variant A computed stats/power:
        /// item.GetStats(level), item.GetPower(level)
        /// </summary>
        public void Show(ItemDef item, int itemLevel)
        {
            if (item == null) return;

            int lvl = Mathf.Max(1, itemLevel);

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            // Icon
            if (itemIcon != null)
            {
                itemIcon.enabled = (item.Icon != null);
                itemIcon.sprite = item.Icon;
                // don't touch itemIcon.color
            }

            // Rarity BG (tint only, sprite stays from prefab)
            if (rarityBG != null)
            {
                var c = item.RarityColor;
                rarityBG.enabled = (c.a > 0.001f);
                if (rarityBG.enabled)
                    rarityBG.color = c;
            }

            // Name (no level inside name)
            if (nameText)
                nameText.text = item.DisplayName;

            // Level (separate)
            if (levelText)
                levelText.text = $"LVL {lvl}";

            // Computed Power (Variant A)
            if (powerText)
                powerText.text = item.GetPower(lvl).ToString();

            // Computed stats at level (digits only)
            ItemStats s = item.GetStats(lvl);

            if (atkText) atkText.text = s.Atk.ToString();
            if (hpText) hpText.text = s.Hp.ToString();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}

