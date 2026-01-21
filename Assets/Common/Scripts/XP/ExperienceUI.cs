using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OctoberStudio
{
    public class ExperienceUI : MonoBehaviour
    {
        [Header("Optional")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("New Slider Bar")]
        [SerializeField] private Slider slider;      // Slider_Basic04_Green (на корне)
        [SerializeField] private TMP_Text levelText; // XP Level Text

        private void Awake()
        {
            if (!slider) slider = GetComponentInChildren<Slider>(true);

            // чтобы игрок не мог двигать ползунок
            if (slider)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.wholeNumbers = false;
                slider.interactable = false;
            }
        }

        /// <param name="progress">0..1</param>
        public void SetProgress(float progress)
        {
            if (!slider) return;
            slider.value = Mathf.Clamp01(progress);
        }

        public void SetLevelText(int levelNumber)
        {
            if (!levelText) return;
            levelText.text = $"{levelNumber}";
        }

        // опционально: показать/скрыть панель (если надо)
        public void SetVisible(bool visible)
        {
            if (!canvasGroup) return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }
    }
}
