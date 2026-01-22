using UnityEngine;
using UnityEngine.UI;

public class AutoOpenToggleBinder : MonoBehaviour
{
    [SerializeField] private Toggle autoToggle;
    [SerializeField] private AutoOpenSettingsPopup settingsPopup;
    [SerializeField] private AutoChestRunner runner;

    private AutoOpenSettingsPopup.Settings _runtime = new AutoOpenSettingsPopup.Settings();
    private bool _internalChange;

    private void Awake()
    {
        if (autoToggle) autoToggle.onValueChanged.AddListener(OnToggle);

        if (settingsPopup != null)
        {
            settingsPopup.OnCanceled += () =>
            {
                // если отменили Ч выключаем toggle
                _internalChange = true;
                autoToggle.isOn = false;
                _internalChange = false;
            };
        }
    }

    private void OnToggle(bool on)
    {
        if (_internalChange) return;

        if (!on)
        {
            if (runner) runner.StopAuto();
            return;
        }

        if (settingsPopup == null || runner == null)
            return;

        settingsPopup.Show(_runtime, (s) =>
        {
            _runtime = s;
            runner.StartAuto(_runtime);
        });
    }
}
