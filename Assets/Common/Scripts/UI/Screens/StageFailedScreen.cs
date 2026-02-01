using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Input;
using OctoberStudio.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using IDosGames;

namespace OctoberStudio.UI
{
    public class StageFailedScreen : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button reviveButton;
        [SerializeField] private Button exitButton;

        [Header("Texts")]
        [SerializeField] private TMP_Text countdownText;   // Text_Timer
        [SerializeField] private TMP_Text gemValueText;    // Gem_Value

        [Header("Countdown")]
        [SerializeField] private int countdownSeconds = 5;

        [Header("Revives")]
        [SerializeField] private int maxRevivesPerRun = 3;
        [Tooltip("Цены по порядку: 1й/2й/3й ревив")]
        [SerializeField] private int[] reviveCosts = new int[] { 10, 20, 50 };

        [Header("Currency (Server)")]
        [SerializeField] private VirtualCurrencyID reviveCurrencyId = VirtualCurrencyID.CO;

        [Header("Optional gating (old upgrade)")]
        [Tooltip("Если включить — ревив доступен только если куплен UpgradeType.Revive")]
        [SerializeField] private bool requireReviveUpgrade = false;

        [Header("On timeout")]
        [Tooltip("Когда таймер дошёл до 0 — активируем этот GO (и НЕ выходим в лобби)")]
        [SerializeField] private GameObject timeoutActivateGO;

        [Header("Not enough coins popup (SDK)")]
        [Tooltip("Можно не задавать. Если пусто — попробуем найти объект PopUp_Coin в DontDestroyOnLoad.")]
        [SerializeField] private GameObject coinOfferPopupRoot; // PopUp_Coin

        private PopupVisibilityNotifier _coinPopupNotifier;
        private bool _pausedByCoinPopup;

        private Coroutine _countdownCoroutine;
        private int _remainingSeconds;

        private bool _reviveInFlight;
        private int _revivesUsedThisRun; // сколько успешных ревивов уже было в этом ранe
        private bool _timedOut; // чтобы не пытаться ресюмить после таймаута

        private void Awake()
        {
            if (reviveButton != null) reviveButton.onClick.AddListener(ReviveButtonClick);
            if (exitButton != null) exitButton.onClick.AddListener(ExitButtonClick);
        }

        // Можно дернуть снаружи при старте нового забега, если нужно.
        public void ResetRevivesForRun()
        {
            _revivesUsedThisRun = 0;
        }

        public void Show()
        {
            gameObject.SetActive(true);

            _timedOut = false;
            _reviveInFlight = false;
            _pausedByCoinPopup = false;

            // ? ВАЖНО: сбрасываем интерактивность при каждом показе
            SetButtonsInteractable(true);

            if (timeoutActivateGO != null)
                timeoutActivateGO.SetActive(false);

            canvasGroup.alpha = 0;
            canvasGroup.DoAlpha(1, 0.3f).SetUnscaledTime(true);

            GameController.InputManager.onInputChanged += OnInputChanged;

            RefreshReviveUI();
            TryStartCountdownIfAllowed();
        }

        public void Hide(UnityAction onFinish)
        {
            StopCountdown();
            CleanupServerCurrencyHandlers();
            UnhookCoinPopup();

            canvasGroup.DoAlpha(0, 0.3f).SetUnscaledTime(true).SetOnFinish(() =>
            {
                gameObject.SetActive(false);
                onFinish?.Invoke();
            });

            GameController.InputManager.onInputChanged -= OnInputChanged;
        }

        // =========================
        // Revive logic
        // =========================

        private void RefreshReviveUI()
        {
            bool canRevive = CanReviveNow();

            if (reviveButton != null)
                reviveButton.gameObject.SetActive(canRevive);

            // после 3-го ревива таймер/стоимость не показываем вообще
            if (countdownText != null)
                countdownText.gameObject.SetActive(canRevive);

            if (gemValueText != null)
                gemValueText.gameObject.SetActive(canRevive);

            if (canRevive)
            {
                int cost = GetCurrentReviveCost();
                if (gemValueText != null) gemValueText.text = cost.ToString();

                if (reviveButton != null)
                    EventSystem.current.SetSelectedGameObject(reviveButton.gameObject);
            }
            else
            {
                if (exitButton != null)
                    EventSystem.current.SetSelectedGameObject(exitButton.gameObject);
            }
        }

        private bool CanReviveNow()
        {
            if (_timedOut) return false;
            if (_reviveInFlight) return false;

            if (requireReviveUpgrade && !GameController.UpgradesManager.IsUpgradeAquired(UpgradeType.Revive))
                return false;

            if (_revivesUsedThisRun >= maxRevivesPerRun)
                return false;

            if (reviveCosts == null || reviveCosts.Length == 0)
                return false;

            if (_revivesUsedThisRun >= reviveCosts.Length)
                return false;

            return true;
        }

        private int GetCurrentReviveCost()
        {
            int idx = Mathf.Clamp(_revivesUsedThisRun, 0, reviveCosts.Length - 1);
            return reviveCosts[idx];
        }

        private void ReviveButtonClick()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            if (!CanReviveNow())
                return;

            // стопаем таймер, пока ждём сервер
            StopCountdown();

            int cost = GetCurrentReviveCost();

            // локальная проверка (чтобы не слать лишний запрос)
            int current = UserInventory.GetVirtualCurrencyAmount(reviveCurrencyId);
            if (current < cost)
            {
                TryShowCoinOfferPopup();
                // Не ресюмим тут вручную — попап сам остановит таймер, а при закрытии возобновит
                return;
            }

            _reviveInFlight = true;
            SetButtonsInteractable(false);

            UserInventory.SuccessSubtractVirtualCurrency += OnServerReviveChargeSuccess;
            UserInventory.ErrorSubtractVirtualCurrency += OnServerReviveChargeError;

            UserInventory.SubtractVirtualCurrency(reviveCurrencyId, cost);
        }

        private void OnServerReviveChargeSuccess()
        {
            CleanupServerCurrencyHandlers();

            _reviveInFlight = false;

            // ? На всякий случай возвращаем интерактивность (хотя экран сейчас скроется)
            SetButtonsInteractable(true);

            _revivesUsedThisRun++;

            // ресаем игрока
            Hide(StageController.ResurrectPlayer);
        }

        private void OnServerReviveChargeError()
        {
            CleanupServerCurrencyHandlers();

            _reviveInFlight = false;
            SetButtonsInteractable(true);

            // если сервер сказал "нет" — покажем попап
            TryShowCoinOfferPopup();
        }

        private void CleanupServerCurrencyHandlers()
        {
            UserInventory.SuccessSubtractVirtualCurrency -= OnServerReviveChargeSuccess;
            UserInventory.ErrorSubtractVirtualCurrency -= OnServerReviveChargeError;
        }

        // =========================
        // Exit
        // =========================

        private void ExitButtonClick()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            StopCountdown();
            CleanupServerCurrencyHandlers();
            UnhookCoinPopup();

            // При выходе из забега — сброс ревивов
            _revivesUsedThisRun = 0;

            Time.timeScale = 1;
            StageController.ReturnToMainMenu();

            GameController.InputManager.onInputChanged -= OnInputChanged;
        }

        // =========================
        // Countdown
        // =========================

        private void TryStartCountdownIfAllowed()
        {
            if (!CanReviveNow())
                return;

            _remainingSeconds = Mathf.Max(0, countdownSeconds);
            UpdateCountdownText(_remainingSeconds);

            if (_remainingSeconds <= 0)
            {
                OnCountdownFinished();
                return;
            }

            StopCountdown();
            _countdownCoroutine = StartCoroutine(CountdownRoutine());
        }

        private void TryResumeCountdown()
        {
            if (_timedOut) return;
            if (_pausedByCoinPopup) return;
            if (_reviveInFlight) return;
            if (!CanReviveNow()) return;

            if (_remainingSeconds <= 0)
            {
                OnCountdownFinished();
                return;
            }

            StopCountdown();
            UpdateCountdownText(_remainingSeconds);
            _countdownCoroutine = StartCoroutine(CountdownRoutine());
        }

        private void StopCountdown()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }
        }

        private System.Collections.IEnumerator CountdownRoutine()
        {
            while (_remainingSeconds > 0)
            {
                yield return new WaitForSecondsRealtime(1f);

                if (!gameObject.activeInHierarchy)
                    yield break;

                if (_reviveInFlight || _pausedByCoinPopup)
                    yield break;

                _remainingSeconds--;
                UpdateCountdownText(_remainingSeconds);
            }

            OnCountdownFinished();
        }

        private void OnCountdownFinished()
        {
            _timedOut = true;

            StopCountdown();
            UpdateCountdownText(0);

            // НЕ выходим в лобби — активируем GO
            if (timeoutActivateGO != null)
                timeoutActivateGO.SetActive(true);

            // блокируем revive
            if (reviveButton != null)
                reviveButton.interactable = false;

            // фокус на Exit
            if (exitButton != null)
                EventSystem.current.SetSelectedGameObject(exitButton.gameObject);
        }

        private void UpdateCountdownText(int value)
        {
            if (countdownText != null)
                countdownText.text = value.ToString();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (reviveButton != null) reviveButton.interactable = interactable;
            if (exitButton != null) exitButton.interactable = interactable;
        }

        // =========================
        // Input focus
        // =========================

        private void OnInputChanged(InputType prevInput, InputType inputType)
        {
            if (prevInput != InputType.UIJoystick)
                return;

            if (CanReviveNow() && reviveButton != null && reviveButton.gameObject.activeSelf)
                EventSystem.current.SetSelectedGameObject(reviveButton.gameObject);
            else if (exitButton != null)
                EventSystem.current.SetSelectedGameObject(exitButton.gameObject);
        }

        // =========================
        // SDK popup: Not enough coins
        // =========================

        private void TryShowCoinOfferPopup()
        {
            // 1) если задан в инспекторе
            if (coinOfferPopupRoot != null)
            {
                HookCoinPopup(coinOfferPopupRoot);
                coinOfferPopupRoot.SetActive(true);
                return;
            }

            // 2) попытка найти по имени даже если объект не активен (DontDestroyOnLoad)
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (all[i].name == "PopUp_Coin")
                {
                    coinOfferPopupRoot = all[i];
                    HookCoinPopup(coinOfferPopupRoot);
                    coinOfferPopupRoot.SetActive(true);
                    return;
                }
            }
        }

        private void HookCoinPopup(GameObject popupRoot)
        {
            if (!popupRoot) return;

            var notifier = popupRoot.GetComponent<PopupVisibilityNotifier>();
            if (!notifier) notifier = popupRoot.AddComponent<PopupVisibilityNotifier>();

            if (_coinPopupNotifier == notifier) return;

            UnhookCoinPopup();

            _coinPopupNotifier = notifier;
            _coinPopupNotifier.Shown += OnCoinPopupShown;
            _coinPopupNotifier.Hidden += OnCoinPopupHidden;
        }

        private void UnhookCoinPopup()
        {
            if (_coinPopupNotifier == null) return;

            _coinPopupNotifier.Shown -= OnCoinPopupShown;
            _coinPopupNotifier.Hidden -= OnCoinPopupHidden;
            _coinPopupNotifier = null;
        }

        private void OnCoinPopupShown()
        {
            _pausedByCoinPopup = true;
            StopCountdown();
        }

        private void OnCoinPopupHidden()
        {
            _pausedByCoinPopup = false;

            if (!gameObject.activeInHierarchy)
                return;

            TryResumeCountdown();
        }

        private void OnDisable()
        {
            StopCountdown();
            CleanupServerCurrencyHandlers();
            UnhookCoinPopup();
        }
    }
}
