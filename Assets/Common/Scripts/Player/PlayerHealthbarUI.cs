using OctoberStudio.Easing;
using UnityEngine;
using UnityEngine.UI;

namespace OctoberStudio
{
    public class PlayerHealthbarUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] protected Slider slider;
        [SerializeField] protected CanvasGroup canvasGroup;

        public float MaxHP { get; private set; }
        public float HP { get; private set; }

        public bool IsZero => HP <= 0;
        public bool IsMax => HP >= MaxHP;

        protected bool autoShowOnChaned;
        protected bool autoHideWhenMax;

        protected IEasingCoroutine showHideCoroutine;
        protected bool isShown = true;

        protected virtual void Awake()
        {
            if (!slider) slider = GetComponentInChildren<Slider>(true);
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();

            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.wholeNumbers = false;
                slider.interactable = false;
            }
        }

        public virtual void Init(float maxHP)
        {
            MaxHP = Mathf.Max(1f, maxHP);
            HP = MaxHP;

            Redraw();
        }

        public virtual void SetAutoShowOnChanged(bool value)
        {
            autoShowOnChaned = value;
        }

        public virtual void SetAutoHideWhenMax(bool value)
        {
            autoHideWhenMax = value;

            if (HP >= MaxHP)
                ForceHide();
        }

        public virtual void AddHP(float value)
        {
            if (value < 0f)
            {
                Subtract(-value);
                return;
            }

            HP += value;

            if (HP > MaxHP)
            {
                HP = MaxHP;

                if (autoHideWhenMax)
                    Hide();
            }

            Redraw();
        }

        public virtual void AddPercentage(float percent)
        {
            AddHP(MaxHP * percent / 100f);
        }

        public virtual void Subtract(float value)
        {
            if (value < 0f)
            {
                AddHP(-value);
                return;
            }

            HP -= value;

            if (HP <= 0f)
            {
                HP = 0f;
                Redraw();
                Hide();
            }
            else
            {
                if (autoShowOnChaned && !isShown)
                    Show();

                Redraw();
            }
        }

        public virtual void ResetHP(float duration = 0f)
        {
            if (duration > 0f)
            {
                EasingManager.DoFloat(0f, MaxHP, duration, hp =>
                {
                    HP = hp;
                    Redraw();
                });

                Show();
            }
            else
            {
                HP = MaxHP;
                Redraw();
            }
        }

        public virtual void ChangeMaxHP(float newMaxHP, bool scaleHP = true)
        {
            float oldMaxHP = Mathf.Max(1f, MaxHP);
            MaxHP = Mathf.Max(1f, newMaxHP);

            if (scaleHP)
            {
                float normalized = HP / oldMaxHP;
                HP = MaxHP * normalized;
            }
            else
            {
                HP = Mathf.Clamp(HP, 0f, MaxHP);
            }

            Redraw();
        }

        public virtual void Redraw()
        {
            if (slider == null) return;

            float t = MaxHP > 0f ? HP / MaxHP : 0f;
            slider.value = Mathf.Clamp01(t);
        }

        public virtual void Show()
        {
            Redraw();
            isShown = true;

            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

            if (showHideCoroutine != null && showHideCoroutine.IsActive)
                showHideCoroutine.Stop();

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            showHideCoroutine = new FloatEasingCoroutine(startAlpha, 1f, 0.3f, 0f, SetAlpha)
                .SetEasing(EasingType.SineOut);
        }

        public virtual void Hide()
        {
            isShown = false;

            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

            if (showHideCoroutine != null && showHideCoroutine.IsActive)
                showHideCoroutine.Stop();

            showHideCoroutine = new FloatEasingCoroutine(startAlpha, 0f, 0.3f, 0f, SetAlpha)
                .SetEasing(EasingType.SineOut);
        }

        public virtual void ForceHide()
        {
            isShown = false;
            SetAlpha(0f);
        }

        public virtual void ForceShow()
        {
            isShown = true;
            SetAlpha(1f);
        }

        protected virtual void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }
    }
}