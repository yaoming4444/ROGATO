using GameCore.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class OpenedCardView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;

        [Header("Rarity Frames")]
        [SerializeField] private Sprite commonFrame;
        [SerializeField] private Sprite rareFrame;
        [SerializeField] private Sprite epicFrame;
        [SerializeField] private Sprite legendaryFrame;

        public void Setup(StatCardDefinition definition, int level)
        {
            if (definition == null)
                return;

            if (iconImage != null)
                iconImage.sprite = definition.Icon;

            if (nameText != null)
                nameText.text = definition.DisplayName;

            if (levelText != null)
                levelText.text = $"Lv. {Mathf.Max(1, level)}";

            if (frameImage != null)
                frameImage.sprite = GetFrameByRarity(definition.Rarity);
        }

        private Sprite GetFrameByRarity(StatRarity rarity)
        {
            switch (rarity)
            {
                case StatRarity.Rare: return rareFrame;
                case StatRarity.Epic: return epicFrame;
                case StatRarity.Legendary: return legendaryFrame;
                default: return commonFrame;
            }
        }
    }
}