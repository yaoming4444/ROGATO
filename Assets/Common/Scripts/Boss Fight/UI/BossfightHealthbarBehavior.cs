using OctoberStudio.Easing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OctoberStudio.Bossfight
{
    public class BossfightHealthbarBehavior : MonoBehaviour
    {
        [Header("Optional")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("New Slider Bar")]
        [SerializeField] private Slider slider;          // Boss HP Slider

        public EnemyBehavior Boss { get; private set; }

        private void Awake()
        {
            if (!canvasGroup)
                canvasGroup = GetComponent<CanvasGroup>();

            if (!slider)
                slider = GetComponentInChildren<Slider>(true);

            // Чтобы игрок не мог двигать ползунок
            if (slider)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.wholeNumbers = false;
                slider.interactable = false;
                slider.value = 1f;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);

            if (canvasGroup)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DoAlpha(1f, 0.3f);
            }
        }

        public void Hide()
        {
            if (canvasGroup)
            {
                canvasGroup.DoAlpha(0f, 0.3f).SetOnFinish(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void SetBoss(EnemyBehavior boss)
        {
            // На всякий случай отписываемся от старого босса,
            // если этот healthbar был переиспользован.
            if (Boss != null)
            {
                Boss.onEnemyDied -= OnBossDied;
                Boss.onHealthChanged -= OnBossHealthChanged;
            }

            Boss = boss;

            if (Boss == null)
                return;

            Boss.onEnemyDied += OnBossDied;
            Boss.onHealthChanged += OnBossHealthChanged;
        }

        private void OnBossHealthChanged(float hp, float maxHP)
        {
            float progress = maxHP > 0f ? hp / maxHP : 0f;

            SetProgress(progress);
        }

        private void OnBossDied(EnemyBehavior enemy)
        {
            if (Boss != null)
            {
                Boss.onEnemyDied -= OnBossDied;
                Boss.onHealthChanged -= OnBossHealthChanged;
            }

            Boss = null;
            SetProgress(0f);
        }

        public void Init(BossfightData data)
        {
            SetProgress(1f);
        }

        /// <param name="progress">0..1</param>
        private void SetProgress(float progress)
        {
            if (!slider)
                return;

            slider.value = Mathf.Clamp01(progress);
        }
    }
}