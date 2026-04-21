using GameCore.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class RolledCardPopupView : MonoBehaviour
    {
        [Header("Card UI")]
        [SerializeField] private Image rootImg;
        [SerializeField] private Image frameImg;
        [SerializeField] private Image levelLabelImg;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text valueText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backgroundCloseButton;

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

            initialized = true;
        }

        public void Show(StatCardDefinition definition, RolledStatCard runtimeCard, bool isUpgrade)
        {
            InitializeIfNeeded();

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (definition == null || runtimeCard == null)
                return;

            if (iconImage != null)
                iconImage.sprite = definition.Icon;

            if (nameText != null)
                nameText.text = definition.DisplayName;

            if (levelText != null)
                levelText.text = $"Lv. {runtimeCard.Level}";

            if (valueText != null)
                valueText.text = FormatValue(definition.StatType, runtimeCard.CurrentValue);

            ApplyRarityColors(definition.Rarity);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
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
                    return $"ATK +{value:0}";
                case StatType.Health:
                    return $"HP +{value:0}";
                case StatType.Defense:
                    return $"DEF +{value:0}";
                case StatType.MoveSpeed:
                    return $"Move Speed +{value:0.##}";
                case StatType.CritChance:
                    return $"Crit Chance +{value:0.##}";
                case StatType.CritDamage:
                    return $"Crit Damage +{value:0.##}";
                case StatType.PickupRange:
                    return $"Pickup Range +{value:0.##}";
                default:
                    return $"+{value:0.##}";
            }
        }
    }
}