using GameCore.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class OpenedCardView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private TMP_Text nameText;

        [Header("Frames By Rarity")]
        [SerializeField] private Sprite commonFrame;
        [SerializeField] private Sprite rareFrame;
        [SerializeField] private Sprite epicFrame;
        [SerializeField] private Sprite legendaryFrame;

        public void Setup(StatCardDefinition definition)
        {
            if (definition == null)
                return;

            if (iconImage != null)
                iconImage.sprite = definition.Icon;

            if (nameText != null)
                nameText.text = definition.DisplayName;

            if (frameImage != null)
                frameImage.sprite = GetFrame(definition.Rarity);
        }

        private Sprite GetFrame(StatRarity rarity)
        {
            switch (rarity)
            {
                case StatRarity.Rare:
                    return rareFrame;
                case StatRarity.Epic:
                    return epicFrame;
                case StatRarity.Legendary:
                    return legendaryFrame;
                default:
                    return commonFrame;
            }
        }
    }
}