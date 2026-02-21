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

        [SerializeField] private Image abilityIcon;

        [Space]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Level (Text / Background)")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Image levelBackgroundImage;
        [SerializeField] private Color levelBackgroundColor;
        [SerializeField] private Color levelBackgroundNewColor;
        [SerializeField] private Color levelBackgroundEvoColor;

        [Header("Level Stars (Active parts only)")]
        [Tooltip("Assign 3 objects: Stars/Star(1)/Active, Stars/Star(2)/Active, Stars/Star(3)/Active")]
        [SerializeField] private GameObject[] starActives = new GameObject[3];

        [Space]
        [SerializeField] private GameObject evolutionBlock;
        [SerializeField] private Image evolutionIcon;

        [Space]
        [SerializeField] private Button button;
        public Selectable Selectable => button;

        [Space]
        [SerializeField] private RectTransform shineRect;
        [SerializeField] private float shineStartY = -1000f;
        [SerializeField] private float shineEndY = 1000f;
        [SerializeField] private float shineDuration = 0.5f;

        [Header("Icon Background")]
        [SerializeField] private Image iconBackgroundImage;
        [SerializeField] private Color iconBackgroundColor;
        [SerializeField] private Color iconBackgroundEvoColor;

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

            // --- Stars Root is ALWAYS ON, we only toggle starActives ---
            // Decide how many stars:
            //  - Normal: displayLevel = level + 2 (как у теб€ было LVL {level+2})
            //  - NEW/EVO: ставим 0 (или можно 1/3 Ч см. ниже)
            int starsCount = 0;

            if (abilityData.IsEvolution)
            {
                levelBackgroundImage.color = levelBackgroundEvoColor;

                // ≈сли EVO хочешь показывать как максимум:
                starsCount = 3;

                // ≈сли хочешь EVO текстом Ч оставь:
                if (levelText != null)
                {
                    levelText.gameObject.SetActive(true);
                    levelText.text = "EVO";
                }
            }
            else if (level == -1 || abilityData.IsEndgameAbility)
            {
                levelBackgroundImage.color = levelBackgroundNewColor;

                // NEW: обычно это первый уровень => 1 звезда (логичнее)
                starsCount = 1;

                if (levelText != null)
                {
                    levelText.gameObject.SetActive(true);
                    levelText.text = "NEW!";
                }
            }
            else
            {
                levelBackgroundImage.color = levelBackgroundColor;

                // ѕр€чем текст, если он есть
                if (levelText != null)
                    levelText.gameObject.SetActive(false);

                int displayLevel = level + 2;               // как раньше
                starsCount = Mathf.Clamp(displayLevel, 1, 3);
            }

            SetStars(starsCount);

            // ----- Icon background -----
            if (abilityData.IsEvolution)
                iconBackgroundImage.color = iconBackgroundEvoColor;
            else
                iconBackgroundImage.color = iconBackgroundColor;

            // ----- Evolution block -----
            if (StageController.AbilityManager.HasEvolution(Data.AbilityType, out var otherType))
            {
                var otherData = StageController.AbilityManager.GetAbilityData(otherType);
                evolutionBlock.SetActive(true);
                evolutionIcon.sprite = otherData.Icon;
            }
            else
            {
                evolutionBlock.SetActive(false);
            }
        }

        private void SetStars(int activeCount)
        {
            if (starActives == null || starActives.Length == 0) return;

            activeCount = Mathf.Clamp(activeCount, 0, 3);

            for (int i = 0; i < starActives.Length; i++)
            {
                if (starActives[i] != null)
                    starActives[i].SetActive(i < activeCount);
            }
        }

        private void ApplyTypeStyle(bool isActive)
        {
            if (backgroundImage != null)
            {
                var sprite = isActive ? backgroundSpriteActive : backgroundSpritePassive;
                if (sprite != null) backgroundImage.sprite = sprite;
            }

            if (titleImage != null)
            {
                var sprite = isActive ? titleSpriteActive : titleSpritePassive;
                if (sprite != null) titleImage.sprite = sprite;
            }

            if (abilityTypeImage != null)
            {
                var sprite = isActive ? abilityTypeSpriteActive : abilityTypeSpritePassive;
                if (sprite != null) abilityTypeImage.sprite = sprite;
            }

            if (abilityTypeText != null)
            {
                abilityTypeText.text = isActive ? "ACTIVE" : "PASSIVE";
                abilityTypeText.color = isActive ? textcolor_active : textcolor_passive;
            }
        }

        public void Show(float delay)
        {
            var from = shineRect.anchoredPosition;
            from.y = shineStartY;

            var to = shineRect.anchoredPosition;
            to.y = shineEndY;

            // ¬—≈√ƒј сбрасываем в старт
            shineRect.anchoredPosition = from;

            shineRect
                .DoAnchorPosition(to, shineDuration, delay)
                .SetUnscaledTime(true);
        }

        private void OnAbilitySelected()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            onAbilitySelected?.Invoke(Data);
        }
    }
}