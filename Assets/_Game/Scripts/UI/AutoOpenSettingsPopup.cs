using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoOpenSettingsPopup : MonoBehaviour
{
    [Serializable]
    public class Settings
    {
        public int MinClass = 0;          // класс >=
        public bool IncreasePower = true; // повышение мощи
        public float OpenInterval = 0.6f; // пауза между открытиями (после решения)
    }

    [Header("UI")]
    [SerializeField] private TMP_Dropdown minClassDropdown;
    [SerializeField] private Toggle increasePowerToggle;
    [SerializeField] private TMP_Dropdown speedDropdown;

    [Header("Buttons")]
    [SerializeField] private Button startButton;   // GO
    [SerializeField] private Button cancelButton;  // X / Cancel

    public Action OnCanceled;

    private Action<Settings> _onStart;

    private void Awake()
    {
        if (startButton) startButton.onClick.AddListener(StartClicked);
        if (cancelButton) cancelButton.onClick.AddListener(CancelClicked);
        gameObject.SetActive(false);
    }

    public void Show(Settings current, Action<Settings> onStart)
    {
        _onStart = onStart;
        gameObject.SetActive(true);

        if (minClassDropdown)
            minClassDropdown.value = Mathf.Clamp(current.MinClass, 0, minClassDropdown.options.Count - 1);

        if (increasePowerToggle)
            increasePowerToggle.isOn = current.IncreasePower;

        if (speedDropdown)
            speedDropdown.value = IntervalToSpeedIndex(current.OpenInterval);
    }

    private void StartClicked()
    {
        var s = new Settings
        {
            MinClass = minClassDropdown ? minClassDropdown.value : 0,
            IncreasePower = increasePowerToggle && increasePowerToggle.isOn,
            OpenInterval = SpeedIndexToInterval(speedDropdown ? speedDropdown.value : 1),
        };

        gameObject.SetActive(false);
        _onStart?.Invoke(s);
    }

    private void CancelClicked()
    {
        gameObject.SetActive(false);
        OnCanceled?.Invoke();
    }

    private float SpeedIndexToInterval(int idx)
    {
        // ВАЖНО: это НЕ скорость анимации, а пауза МЕЖДУ открытиями
        // Сама анимация теперь всегда ждётся полностью (ChestAnimDriver).
        return idx switch
        {
            0 => 1.2f,  // медленно
            1 => 0.8f,  // нормально
            2 => 0.6f,  // быстро
            3 => 0.4f,  // очень быстро
            _ => 0.8f
        };
    }

    private int IntervalToSpeedIndex(float interval)
    {
        if (interval >= 1.1f) return 0;
        if (interval >= 0.75f) return 1;
        if (interval >= 0.55f) return 2;
        return 3;
    }
}
