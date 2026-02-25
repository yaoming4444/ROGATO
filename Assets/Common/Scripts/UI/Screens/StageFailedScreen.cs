using System.Collections;
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

        [Tooltip("Button that forces reward panel to open immediately (old Exit button).")]
        [SerializeField] private Button showRewardsButton;

        [Tooltip("Real Exit button to return home. Hidden until rewards panel is shown.")]
        [SerializeField] private Button finalExitButton;

        [Header("Texts")]
        [SerializeField] private TMP_Text countdownText;   // Text_Timer
        [SerializeField] private TMP_Text gemValueText;    // Gem_Value

        [Header("Countdown")]
        [SerializeField] private int countdownSeconds = 5;

        [Header("Revives")]
        [SerializeField] private int maxRevivesPerRun = 3;
        [Tooltip("Costs in order: 1st / 2nd / 3rd revive")]
        [SerializeField] private int[] reviveCosts = new int[] { 10, 20, 50 };

        [Header("Currency (Server)")]
        [SerializeField] private VirtualCurrencyID reviveCurrencyId = VirtualCurrencyID.CO;

        [Header("Optional gating (old upgrade)")]
        [Tooltip("If enabled - revive is available only when UpgradeType.Revive is purchased")]
        [SerializeField] private bool requireReviveUpgrade = false;

        [Header("Rewards panel")]
        [Tooltip("Reward panel root GO that becomes visible when player can no longer revive.")]
        [SerializeField] private GameObject timeoutActivateGO;

        [Tooltip("UI builder that spawns reward slots into layout group.")]
        [SerializeField] private StageRewardsPanelUI rewardsPanelUI;

        [Header("Reward Icons (for calculated rewards UI)")]
        [SerializeField] private Sprite coinsRewardIcon;
        [SerializeField] private Sprite expRewardIcon;

        [Header("Debug / Fallback")]
        [Tooltip("Optional fallback if runtime StageData was not resolved.")]
        [SerializeField] private StageData fallbackStageData;

        [Tooltip("Use this elapsed time if you haven't wired real stage timer yet.")]
        [SerializeField] private bool useDebugElapsedTime = false;

        [Tooltip("Debug elapsed seconds for reward calculation (only if useDebugElapsedTime = true).")]
        [SerializeField] private float debugElapsedSeconds = 190f;

        [Tooltip("Total stage duration in seconds (default 6 minutes = 360).")]
        [SerializeField] private float stageTotalSeconds = 360f;

        [Header("Not enough coins popup (SDK)")]
        [Tooltip("Optional. If empty, script will try to find PopUp_Coin in DontDestroyOnLoad.")]
        [SerializeField] private GameObject coinOfferPopupRoot; // PopUp_Coin

        private PopupVisibilityNotifier _coinPopupNotifier;
        private bool _pausedByCoinPopup;

        private Coroutine _countdownCoroutine;
        private int _remainingSeconds;

        private bool _reviveInFlight;
        private int _revivesUsedThisRun;
        private bool _timedOut;
        private bool _rewardsShown;

        // Reward grant protection / cache
        private bool _failedRewardsGranted;
        private StageRewardService.CalculatedStageRewards _cachedFailedRewards;

        // Optional runtime override (can still be set externally if needed)
        private StageData _currentStageData;

        private void Awake()
        {
            if (reviveButton != null) reviveButton.onClick.AddListener(ReviveButtonClick);
            if (showRewardsButton != null) showRewardsButton.onClick.AddListener(ShowRewardsButtonClick);
            if (finalExitButton != null) finalExitButton.onClick.AddListener(FinalExitButtonClick);
        }

        /// <summary>
        /// Optional external setter. If not called, script will use StageController.Stage automatically.
        /// </summary>
        public void SetStageData(StageData stageData)
        {
            _currentStageData = stageData;
        }

        /// <summary>
        /// Optional overload.
        /// </summary>
        public void Show(StageData stageData)
        {
            _currentStageData = stageData;
            Show();
        }

        // Can be called externally at the start of a new run if needed.
        public void ResetRevivesForRun()
        {
            _revivesUsedThisRun = 0;
            _failedRewardsGranted = false;
            _cachedFailedRewards = default;
        }

        public void Show()
        {
            gameObject.SetActive(true);

            _timedOut = false;
            _rewardsShown = false;
            _reviveInFlight = false;
            _pausedByCoinPopup = false;

            // new run / new fail screen appearance => reset grant guard
            _failedRewardsGranted = false;
            _cachedFailedRewards = default;

            SetButtonsInteractable(true);

            if (timeoutActivateGO != null)
                timeoutActivateGO.SetActive(false);

            if (rewardsPanelUI != null)
                rewardsPanelUI.Clear();

            if (showRewardsButton != null)
                showRewardsButton.gameObject.SetActive(true);

            if (finalExitButton != null)
                finalExitButton.gameObject.SetActive(false);

            canvasGroup.alpha = 0;
            canvasGroup.DoAlpha(1, 0.3f).SetUnscaledTime(true);

            GameController.InputManager.onInputChanged += OnInputChanged;

            RefreshReviveUI();

            // If revive is not available, immediately show rewards panel
            if (!CanReviveNow())
            {
                ShowRewardsPanel(forceFromButton: false);
                return;
            }

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

            if (countdownText != null)
                countdownText.gameObject.SetActive(canRevive);

            if (gemValueText != null)
                gemValueText.gameObject.SetActive(canRevive);

            if (canRevive)
            {
                int cost = GetCurrentReviveCost();

                if (gemValueText != null)
                    gemValueText.text = cost.ToString();

                if (reviveButton != null)
                    EventSystem.current.SetSelectedGameObject(reviveButton.gameObject);
            }
            else
            {
                if (_rewardsShown && finalExitButton != null && finalExitButton.gameObject.activeSelf)
                    EventSystem.current.SetSelectedGameObject(finalExitButton.gameObject);
                else if (showRewardsButton != null && showRewardsButton.gameObject.activeSelf)
                    EventSystem.current.SetSelectedGameObject(showRewardsButton.gameObject);
            }
        }

        private bool CanReviveNow()
        {
            if (_timedOut) return false;
            if (_rewardsShown) return false;
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

            StopCountdown();

            int cost = GetCurrentReviveCost();
            int current = UserInventory.GetVirtualCurrencyAmount(reviveCurrencyId);

            if (current < cost)
            {
                TryShowCoinOfferPopup();
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
            SetButtonsInteractable(true);

            _revivesUsedThisRun++;

            Hide(StageController.ResurrectPlayer);
        }

        private void OnServerReviveChargeError()
        {
            CleanupServerCurrencyHandlers();

            _reviveInFlight = false;
            SetButtonsInteractable(true);

            TryShowCoinOfferPopup();
        }

        private void CleanupServerCurrencyHandlers()
        {
            UserInventory.SuccessSubtractVirtualCurrency -= OnServerReviveChargeSuccess;
            UserInventory.ErrorSubtractVirtualCurrency -= OnServerReviveChargeError;
        }

        // =========================
        // Rewards / Exit flow
        // =========================

        private void ShowRewardsButtonClick()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            ShowRewardsPanel(forceFromButton: true);
        }

        private void FinalExitButtonClick()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            ReturnToMainMenu();
        }

        /// <summary>
        /// Финальный экран фейла:
        /// - останавливает revive-flow
        /// - рассчитывает частичные/утешительные награды
        /// - показывает панель наград
        /// - выдает награды (один раз)
        /// </summary>
        private void ShowRewardsPanel(bool forceFromButton)
        {
            if (_rewardsShown)
                return;

            _timedOut = true;
            _rewardsShown = true;

            StopCountdown();
            CleanupServerCurrencyHandlers();
            UnhookCoinPopup();

            if (countdownText != null)
                UpdateCountdownText(0);

            if (reviveButton != null)
            {
                reviveButton.interactable = false;
                reviveButton.gameObject.SetActive(false);
            }

            if (showRewardsButton != null)
            {
                showRewardsButton.interactable = false;
                showRewardsButton.gameObject.SetActive(false);
            }

            if (timeoutActivateGO != null)
                timeoutActivateGO.SetActive(true);

            if (countdownText != null)
                countdownText.gameObject.SetActive(false);

            if (gemValueText != null)
                gemValueText.gameObject.SetActive(false);

            // 1) Calculate failed rewards (by elapsed stage time)
            CalculateFailedRewardsForThisRun();

            // 2) Build reward UI (calculated values)
            BuildRewardsUIForFailedState();

            // 3) Grant rewards once
            GrantFailedRewardsIfNeeded();

            if (finalExitButton != null)
            {
                finalExitButton.gameObject.SetActive(true);
                finalExitButton.interactable = true;
                EventSystem.current.SetSelectedGameObject(finalExitButton.gameObject);
            }
        }

        private void CalculateFailedRewardsForThisRun()
        {
            StageData stageData = ResolveStageDataForRewards();
            if (stageData == null)
            {
                _cachedFailedRewards = default;
                Debug.LogError("[StageFailedScreen] StageData is null. Failed rewards cannot be calculated.");
                return;
            }

            float elapsedSeconds = ResolveRunElapsedSecondsForRewards();

            _cachedFailedRewards = StageRewardService.CalculateFailedRewards(
                stageData,
                elapsedSeconds,
                stageTotalSeconds
            );

            Debug.Log($"[StageFailedScreen] Failed rewards calculated. " +
                      $"Elapsed={elapsedSeconds:F1}s, Coins={_cachedFailedRewards.Coins}, Exp={_cachedFailedRewards.Exp}, Outcome={_cachedFailedRewards.OutcomeType}");
        }

        private void BuildRewardsUIForFailedState()
        {
            if (rewardsPanelUI == null)
            {
                Debug.LogError("[StageFailedScreen] rewardsPanelUI is NULL");
                return;
            }

            rewardsPanelUI.Clear();

            // Preferred: show calculated rewards (matches actual grant)
            if (_cachedFailedRewards.Coins > 0 || _cachedFailedRewards.Exp > 0)
            {
                rewardsPanelUI.ShowCalculatedRewards(_cachedFailedRewards, coinsRewardIcon, expRewardIcon);
                return;
            }

            // If zero rewards (edge case), still allow panel to show empty.
            // Optional fallback preview:
            // var stageData = ResolveStageDataForRewards();
            // if (stageData != null) rewardsPanelUI.ShowStageRewards(stageData);
        }

        private void GrantFailedRewardsIfNeeded()
        {
            if (_failedRewardsGranted)
                return;

            _failedRewardsGranted = true;

            if (_cachedFailedRewards.Coins <= 0 && _cachedFailedRewards.Exp <= 0)
            {
                Debug.Log("[StageFailedScreen] No failed rewards to grant.");
                return;
            }

            StageRewardService.GrantRewards(_cachedFailedRewards);
            GameCore.GameInstance.I?.SaveLocalNow();
            GameCore.GameInstance.I?.SaveServerNow();
        }

        private StageData ResolveStageDataForRewards()
        {
            // 1) If explicitly assigned somewhere
            if (_currentStageData != null)
                return _currentStageData;

            // 2) Main source: active stage from StageController
            if (StageController.Stage != null)
                return StageController.Stage;

            // 3) Fallback from inspector (testing)
            if (fallbackStageData != null)
                return fallbackStageData;

            Debug.LogError("[StageFailedScreen] Could not resolve StageData (_currentStageData, StageController.Stage, fallbackStageData are null)");
            return null;
        }

        /// <summary>
        /// Возвращает elapsed time игрового stage (НЕ revive countdown).
        /// Подставь здесь точное поле/свойство из StageController.
        /// </summary>
        private float ResolveRunElapsedSecondsForRewards()
        {
            return Mathf.Max(0f, (float)StageController.Director.time);
        }

        private void ReturnToMainMenu()
        {
            StopCountdown();
            CleanupServerCurrencyHandlers();
            UnhookCoinPopup();

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
            if (_rewardsShown) return;
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

        private IEnumerator CountdownRoutine()
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
            ShowRewardsPanel(forceFromButton: false);
        }

        private void UpdateCountdownText(int value)
        {
            if (countdownText != null)
                countdownText.text = value.ToString();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (reviveButton != null) reviveButton.interactable = interactable;
            if (showRewardsButton != null) showRewardsButton.interactable = interactable;

            if (finalExitButton != null && finalExitButton.gameObject.activeSelf)
                finalExitButton.interactable = interactable;
        }

        // =========================
        // Input focus
        // =========================

        private void OnInputChanged(InputType prevInput, InputType inputType)
        {
            if (prevInput != InputType.UIJoystick)
                return;

            if (_rewardsShown)
            {
                if (finalExitButton != null && finalExitButton.gameObject.activeSelf)
                    EventSystem.current.SetSelectedGameObject(finalExitButton.gameObject);
                return;
            }

            if (CanReviveNow() && reviveButton != null && reviveButton.gameObject.activeSelf)
                EventSystem.current.SetSelectedGameObject(reviveButton.gameObject);
            else if (showRewardsButton != null && showRewardsButton.gameObject.activeSelf)
                EventSystem.current.SetSelectedGameObject(showRewardsButton.gameObject);
        }

        // =========================
        // SDK popup: Not enough coins
        // =========================

        private void TryShowCoinOfferPopup()
        {
            if (coinOfferPopupRoot != null)
            {
                HookCoinPopup(coinOfferPopupRoot);
                coinOfferPopupRoot.SetActive(true);
                return;
            }

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