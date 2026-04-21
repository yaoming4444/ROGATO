using System.Collections.Generic;
using GameCore.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class CardCollectionPopupView : MonoBehaviour
    {
        [System.Serializable]
        public class CardPopupData
        {
            public string cardId;
            public StatCardDefinition definition;
            public int level;
            public float value;
        }

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backgroundCloseButton;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;

        [Header("Card UI")]
        [SerializeField] private Image rootImg;
        [SerializeField] private Image frameImg;
        [SerializeField] private Image levelLabelImg;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text valueText;

        [Header("Common Colors")]
        [SerializeField] private Color commonRootColor = Color.white;
        [SerializeField] private Color commonFrameColor = Color.white;
        [SerializeField] private Color commonLevelLabelColor = Color.white;

        [Header("Rare Colors")]
        [SerializeField] private Color rareRootColor = Color.white;
        [SerializeField] private Color rareFrameColor = Color.white;
        [SerializeField] private Color rareLevelLabelColor = Color.white;

        [Header("Epic Colors")]
        [SerializeField] private Color epicRootColor = Color.white;
        [SerializeField] private Color epicFrameColor = Color.white;
        [SerializeField] private Color epicLevelLabelColor = Color.white;

        [Header("Legendary Colors")]
        [SerializeField] private Color legendaryRootColor = Color.white;
        [SerializeField] private Color legendaryFrameColor = Color.white;
        [SerializeField] private Color legendaryLevelLabelColor = Color.white;

        private readonly List<CardPopupData> openedCards = new();
        private int currentIndex = -1;
        private bool initialized;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
                return;

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            if (backgroundCloseButton != null)
                backgroundCloseButton.onClick.AddListener(Hide);

            if (leftButton != null)
                leftButton.onClick.AddListener(ShowPrevious);

            if (rightButton != null)
                rightButton.onClick.AddListener(ShowNext);

            initialized = true;
        }

        public void Show(List<CardPopupData> data, int startIndex)
        {
            InitializeIfNeeded();

            if (data == null || data.Count == 0)
                return;

            openedCards.Clear();
            openedCards.AddRange(data);

            currentIndex = Mathf.Clamp(startIndex, 0, openedCards.Count - 1);

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            RefreshCurrent();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ShowPrevious()
        {
            if (openedCards.Count == 0)
                return;

            if (currentIndex <= 0)
                return;

            currentIndex--;
            RefreshCurrent();
        }

        private void ShowNext()
        {
            if (openedCards.Count == 0)
                return;

            if (currentIndex >= openedCards.Count - 1)
                return;

            currentIndex++;
            RefreshCurrent();
        }

        private void RefreshCurrent()
        {
            if (currentIndex < 0 || currentIndex >= openedCards.Count)
                return;

            CardPopupData data = openedCards[currentIndex];
            if (data == null || data.definition == null)
                return;

            if (iconImage != null)
                iconImage.sprite = data.definition.Icon;

            if (nameText != null)
                nameText.text = data.definition.DisplayName;

            if (levelText != null)
                levelText.text = data.level.ToString();

            if (valueText != null)
                valueText.text = FormatValue(data.definition.StatType, data.value);

            ApplyRarityColors(data.definition.Rarity);

            bool hasMany = openedCards.Count > 1;

            if (leftButton != null)
                leftButton.gameObject.SetActive(hasMany && currentIndex > 0);

            if (rightButton != null)
                rightButton.gameObject.SetActive(hasMany && currentIndex < openedCards.Count - 1);
        }

        private void ApplyRarityColors(StatRarity rarity)
        {
            switch (rarity)
            {
                case StatRarity.Rare:
                    SetColors(rareRootColor, rareFrameColor, rareLevelLabelColor);
                    break;

                case StatRarity.Epic:
                    SetColors(epicRootColor, epicFrameColor, epicLevelLabelColor);
                    break;

                case StatRarity.Legendary:
                    SetColors(legendaryRootColor, legendaryFrameColor, legendaryLevelLabelColor);
                    break;

                default:
                    SetColors(commonRootColor, commonFrameColor, commonLevelLabelColor);
                    break;
            }
        }

        private void SetColors(Color rootColor, Color frameColor, Color levelColor)
        {
            if (rootImg != null)
                rootImg.color = rootColor;

            if (frameImg != null)
                frameImg.color = frameColor;

            if (levelLabelImg != null)
                levelLabelImg.color = levelColor;
        }

        private string FormatValue(StatType statType, float value)
        {
            switch (statType)
            {
                case StatType.Attack:
                    return $"Skill Attack +{value:0}";
                case StatType.Health:
                    return $"Skill HP +{value:0}";
                case StatType.Defense:
                    return $"Skill Defense +{value:0}";
                case StatType.MoveSpeed:
                    return $"Skill Move Speed +{value:0.##}";
                case StatType.CritChance:
                    return $"Skill Crit Chance +{value:0.##}";
                case StatType.CritDamage:
                    return $"Skill Crit Damage +{value:0.##}";
                case StatType.PickupRange:
                    return $"Skill Pickup Range +{value:0.##}";
                default:
                    return $"+{value:0.##}";
            }
        }
    }
}