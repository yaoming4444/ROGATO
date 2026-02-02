using OctoberStudio.Audio;
using OctoberStudio.Easing;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OctoberStudio.Abilities.UI
{
    public class AbilityCardBehavior : MonoBehaviour
    {
        [Header("Type UI (Active/Passive)")]
        [SerializeField] private Image backgroundImage;          // whole card bg
        [SerializeField] private Sprite backgroundSpriteActive;
        [SerializeField] private Sprite backgroundSpritePassive;

        [Space]
        [SerializeField] private Image titleImage;               // top ribbon/bg behind "ACTIVE/PASSIVE"
        [SerializeField] private Sprite titleSpriteActive;
        [SerializeField] private Sprite titleSpritePassive;

        [Space]
        [SerializeField] private Image abilityTypeImage;         // optional icon/badge near type text
        [SerializeField] private Sprite abilityTypeSpriteActive;
        [SerializeField] private Sprite abilityTypeSpritePassive;
        [SerializeField] private TMP_Text abilityTypeText;       // "ACTIVE" / "PASSIVE"
        [SerializeField] private Color textcolor_active = Color.white;
        [SerializeField] private Color textcolor_passive = Color.white;

        [SerializeField] Image abilityIcon;

        [Space]
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descriptionText;

        [Header("Level Text")]
        [SerializeField] TMP_Text levelText;
        [SerializeField] Image levelBackgroundImage;
        [SerializeField] Color levelBackgroundColor;
        [SerializeField] Color levelBackgroundNewColor;
        [SerializeField] Color levelBackgroundEvoColor;

        [Space]
        [SerializeField] GameObject evolutionBlock;
        [SerializeField] Image evolutionIcon;

        [Space]
        [SerializeField] Button button;
        public Selectable Selectable => button;

        [Space]
        [SerializeField] RectTransform shineRect;

        [Header("Icon Background")]
        [SerializeField] Image iconBackgroundImage;
        [SerializeField] Color iconBackgroundColor;
        [SerializeField] Color iconBackgroundEvoColor;

        private Vector2 shineStartPosition;

        public AbilityData Data { get; private set; }

        private Action<AbilityData> onAbilitySelected;

        private void Awake()
        {
            button.onClick.AddListener(OnAbilitySelected);

            shineStartPosition = shineRect.anchoredPosition;
        }

        public void Init(Action<AbilityData> onAbilitySelected)
        {
            this.onAbilitySelected = onAbilitySelected;
        }

        public void SetData(AbilityData abilityData, int level)
        {
            Data = abilityData;

            ApplyTypeStyle(abilityData != null && abilityData.IsActiveAbility);

            abilityIcon.sprite = abilityData.Icon;

            titleText.text = abilityData.Title;
            descriptionText.text = abilityData.Description;

            if (abilityData.IsEvolution)
            {
                levelBackgroundImage.color = levelBackgroundEvoColor;
                levelText.text = $"EVO";
            }
            else if (level == -1 || abilityData.IsEndgameAbility)
            {
                levelBackgroundImage.color = levelBackgroundNewColor;
                levelText.text = $"NEW!";
            }
            else
            {
                levelBackgroundImage.color = levelBackgroundColor;
                levelText.text = $"LVL {level + 2}";
            }

            if (abilityData.IsEvolution)
            {
                iconBackgroundImage.color = iconBackgroundEvoColor;
            }
            else
            {
                iconBackgroundImage.color = iconBackgroundColor;
            }

            if (StageController.AbilityManager.HasEvolution(Data.AbilityType, out var otherType))
            {
                var otherData = StageController.AbilityManager.GetAbilityData(otherType);
                var otherIcon = otherData.Icon;

                evolutionBlock.SetActive(true);
                evolutionIcon.sprite = otherIcon;
            }
            else
            {
                evolutionBlock.SetActive(false);
            }
        }

        private void ApplyTypeStyle(bool isActive)
        {
            // Background
            if (backgroundImage != null)
            {
                var sprite = isActive ? backgroundSpriteActive : backgroundSpritePassive;
                if (sprite != null) backgroundImage.sprite = sprite;
            }

            // Title/Ribbon
            if (titleImage != null)
            {
                var sprite = isActive ? titleSpriteActive : titleSpritePassive;
                if (sprite != null) titleImage.sprite = sprite;
            }

            // Type badge/icon
            if (abilityTypeImage != null)
            {
                var sprite = isActive ? abilityTypeSpriteActive : abilityTypeSpritePassive;
                if (sprite != null) abilityTypeImage.sprite = sprite;
            }

            // Type label text
            if (abilityTypeText != null)
            {
                abilityTypeText.text = isActive ? "ACTIVE" : "PASSIVE";
                abilityTypeText.color = isActive ? textcolor_active : textcolor_passive;
            }
        }

        public void Show(float delay)
        {
            var targetShinePosition = shineStartPosition;
            targetShinePosition.x *= -1;

            shineRect.anchoredPosition = shineStartPosition;
            shineRect.DoAnchorPosition(targetShinePosition, 0.5f, delay).SetUnscaledTime(true);
        }

        private void OnAbilitySelected()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            onAbilitySelected?.Invoke(Data);
        }
    }
}
