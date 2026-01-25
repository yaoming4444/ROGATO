using UnityEngine;
using UnityEngine.UI;

public class AutoOpenToggleBinder : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button autoButton;
    [SerializeField] private AutoOpenSettingsPopup settingsPopup;
    [SerializeField] private AutoChestRunner runner;

    [Header("Button Visuals")]
    [Tooltip("Image to tint when auto-open is RUNNING (usually the button background or icon).")]
    [SerializeField] private Image buttonImage;

    [Tooltip("Color when auto-open is OFF. If alpha is 0, it will keep current color.")]
    [SerializeField] private Color offColor = Color.white;

    [Tooltip("Color when auto-open is RUNNING.")]
    [SerializeField] private Color onColor = new Color(0.2f, 1f, 0.2f, 1f);

    [Header("Button VFX")]
    [Tooltip("Existing VFX GameObject to enable/disable when auto-open is running.")]
    [SerializeField] private GameObject vfxObject;

    private AutoOpenSettingsPopup.Settings _runtime = new AutoOpenSettingsPopup.Settings();

    private void Awake()
    {
        if (autoButton) autoButton.onClick.AddListener(OnButtonClick);

        if (runner != null)
            runner.RunningChanged += ApplyVisuals;

        if (settingsPopup != null)
        {
            // если отменили настройки — ничего не запускаем, просто оставляем визуал по факту (скорее всего OFF)
            settingsPopup.OnCanceled += () =>
            {
                ApplyVisuals(runner != null && runner.IsRunning);
            };
        }

        // init visuals based on real running state
        ApplyVisuals(runner != null && runner.IsRunning);
    }

    private void OnDestroy()
    {
        if (runner != null)
            runner.RunningChanged -= ApplyVisuals;
    }

    private void OnButtonClick()
    {
        if (runner == null || settingsPopup == null)
            return;

        // если уже запущено — повторный клик полностью выключает
        if (runner.IsRunning)
        {
            runner.DisableAuto();
            // визуал выключится через событие RunningChanged(false)
            return;
        }

        // если НЕ запущено — открываем настройки.
        // ВАЖНО: визуал НЕ включаем здесь. Он включится только когда реально стартанёт loop.
        settingsPopup.Show(_runtime, (s) =>
        {
            _runtime = s;
            runner.StartAuto(_runtime);
            // визуал включится через RunningChanged(true)
        });
    }

    private void ApplyVisuals(bool running)
    {
        // Tint image (optional)
        if (buttonImage != null)
        {
            if (!running && offColor.a <= 0f)
            {
                // keep current
            }
            else
            {
                buttonImage.color = running ? onColor : offColor;
            }
        }

        // Enable/disable existing VFX object (optional)
        if (vfxObject != null)
            vfxObject.SetActive(running);
    }
}
