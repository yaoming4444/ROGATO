using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Input;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    public class StageCompleteScreen : MonoBehaviour
    {
        private Canvas canvas;

        private static readonly int STAGE_COMPLETE_HASH = "Stage Complete".GetHashCode();

        [Header("Main UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button button;

        [Header("Rewards UI")]
        [SerializeField] private StageRewardsPanelUI rewardsPanelUI;
        [SerializeField] private Sprite coinsRewardIcon;
        [SerializeField] private Sprite expRewardIcon;

        [Header("Reward Calc")]
        [Tooltip("Usually 360 seconds (6 min). Used only for reward metadata.")]
        [SerializeField] private float stageTotalSeconds = 360f;

        [Header("Debug / Fallback")]
        [Tooltip("Optional fallback if StageController.Stage is null (for testing).")]
        [SerializeField] private StageData fallbackStageData;

        private bool _completeRewardsGranted;
        private StageRewardService.CalculatedStageRewards _cachedCompleteRewards;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();

            if (button != null)
                button.onClick.AddListener(OnButtonClicked);
        }

        public void Show(UnityAction onFinish = null)
        {
            // Reset per-open state
            _completeRewardsGranted = false;
            _cachedCompleteRewards = default;

            // Calculate + show + grant rewards before/while showing screen
            CalculateCompleteRewardsForThisRun();
            BuildRewardsUIForCompleteState();
            GrantCompleteRewardsIfNeeded();

            canvasGroup.alpha = 0;
            canvasGroup.DoAlpha(1f, 0.3f).SetUnscaledTime(true).SetOnFinish(onFinish);

            gameObject.SetActive(true);

            GameController.AudioManager.PlaySound(STAGE_COMPLETE_HASH);

            if (button != null)
                EventSystem.current.SetSelectedGameObject(button.gameObject);

            GameController.InputManager.onInputChanged += OnInputChanged;
        }

        public void Hide(UnityAction onFinish = null)
        {
            canvasGroup.DoAlpha(0f, 0.3f).SetUnscaledTime(true).SetOnFinish(() => {
                gameObject.SetActive(false);
                onFinish?.Invoke();
            });

            GameController.InputManager.onInputChanged -= OnInputChanged;
        }

        private void CalculateCompleteRewardsForThisRun()
        {
            StageData stageData = ResolveStageDataForRewards();
            if (stageData == null)
            {
                _cachedCompleteRewards = default;
                Debug.LogError("[StageCompleteScreen] StageData is null. Complete rewards cannot be calculated.");
                return;
            }

            _cachedCompleteRewards = StageRewardService.CalculateCompleteRewards(stageData, stageTotalSeconds);

            Debug.Log($"[StageCompleteScreen] Complete rewards calculated. Coins={_cachedCompleteRewards.Coins}, Exp={_cachedCompleteRewards.Exp}");
        }

        private void BuildRewardsUIForCompleteState()
        {
            if (rewardsPanelUI == null)
            {
                Debug.LogWarning("[StageCompleteScreen] rewardsPanelUI is not assigned.");
                return;
            }

            rewardsPanelUI.Clear();

            // Preferred: calculated rewards (UI matches actual grant)
            if (_cachedCompleteRewards.Coins > 0 || _cachedCompleteRewards.Exp > 0)
            {
                rewardsPanelUI.ShowCalculatedRewards(_cachedCompleteRewards, coinsRewardIcon, expRewardIcon);
                return;
            }

            // Optional fallback: show raw stage rewards if calculated is empty
            StageData stageData = ResolveStageDataForRewards();
            if (stageData != null)
                rewardsPanelUI.ShowStageRewards(stageData);
        }

        private void GrantCompleteRewardsIfNeeded()
        {
            if (_completeRewardsGranted)
                return;

            _completeRewardsGranted = true;

            if (_cachedCompleteRewards.Coins <= 0 && _cachedCompleteRewards.Exp <= 0)
            {
                Debug.Log("[StageCompleteScreen] No complete rewards to grant.");
                return;
            }

            StageRewardService.GrantRewards(_cachedCompleteRewards);
        }

        private StageData ResolveStageDataForRewards()
        {
            // Main source: active stage from StageController
            if (StageController.Stage != null)
                return StageController.Stage;

            // Fallback for tests
            if (fallbackStageData != null)
                return fallbackStageData;

            Debug.LogError("[StageCompleteScreen] Could not resolve StageData (StageController.Stage and fallbackStageData are null)");
            return null;
        }

        private void OnButtonClicked()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            Time.timeScale = 1;
            GameController.LoadMainMenu();

            GameController.InputManager.onInputChanged -= OnInputChanged;
        }

        private void OnInputChanged(InputType prevInput, InputType inputType)
        {
            if (prevInput == InputType.UIJoystick && button != null)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }
    }
}