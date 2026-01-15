using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Input;
using OctoberStudio.Save;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using IDosGames;

namespace OctoberStudio.UI
{
    public class LobbyWindowBehavior : MonoBehaviour
    {
        [SerializeField] StagesDatabase stagesDatabase;

        [Space]
        [SerializeField] Image stageIcon;
        [SerializeField] Image lockImage;
        [SerializeField] TMP_Text stageLabel;
        [SerializeField] TMP_Text stageNumberLabel;

        [Space]
        [SerializeField] Button playButton;
        [SerializeField] Button leftButton;
        [SerializeField] Button rightButton;

        [Space]
        [SerializeField] Sprite playButtonEnabledSprite;
        [SerializeField] Sprite playButtonDisabledSprite;

        [Space]
        [SerializeField] Image continueBackgroundImage;
        [SerializeField] RectTransform contituePopupRect;
        [SerializeField] Button confirmButton;
        [SerializeField] Button cancelButton;

        // =========================
        // Серверная валюта на старт
        // =========================
        [Header("Server Run Cost")]
        [SerializeField] private VirtualCurrencyID startCurrencyId = VirtualCurrencyID.CO;
        [SerializeField] private int startRunCost = 1;

        [Tooltip("Если включить, то при Continue тоже будет списываться валюта")]
        [SerializeField] private bool chargeOnContinue = false;

        [Tooltip("Опционально: текст для ошибок (не хватает валюты / ошибка сети)")]
        [SerializeField] private TMP_Text startErrorLabel;

        // =========================
        // AllData refresh flow (before/after charge)
        // =========================
        [Header("RequestUserAllData")]
        [Tooltip("Перед списанием сделать RequestUserAllData (актуализировать баланс)")]
        [SerializeField] private bool refreshAllDataBeforeCharge = true;

        [Tooltip("После успешного списания сделать RequestUserAllData (актуализировать UI/баланс)")]
        [SerializeField] private bool refreshAllDataAfterCharge = true;

        [Tooltip("Таймаут ожидания обновления данных (сек). Если таймаут — продолжаем сценарий дальше.")]
        [SerializeField] private float allDataRefreshTimeoutSeconds = 3f;

        private bool _startInFlight;
        private bool _pendingResetStageData;
        private bool _pendingShouldCharge;

        private bool _waitingAllDataRefresh;
        private Coroutine _allDataTimeoutCoroutine;

        // какой шаг сейчас ждём
        private enum FlowStep
        {
            None,
            PreChargeRefresh,
            PostChargeRefresh
        }
        private FlowStep _flowStep = FlowStep.None;

        private StageSave save;

        private void Awake()
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
            leftButton.onClick.AddListener(DecrementSelectedStageId);
            rightButton.onClick.AddListener(IncremenSelectedStageId);

            confirmButton.onClick.AddListener(ConfirmButtonClicked);
            cancelButton.onClick.AddListener(CancelButtonClicked);
        }

        private void Start()
        {
            save = GameController.SaveManager.GetSave<StageSave>("Stage");
            save.onSelectedStageChanged += InitStage;

            if (save.IsPlaying && GameController.FirstTimeLoaded)
            {
                continueBackgroundImage.gameObject.SetActive(true);
                contituePopupRect.gameObject.SetActive(true);

                EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);

                InitStage(save.SelectedStageId);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(playButton.gameObject);
                save.SetSelectedStageId(save.MaxReachedStageId);
            }

            GameController.InputManager.onInputChanged += OnInputChanged;
            GameController.InputManager.InputAsset.UI.Settings.performed += OnSettingsInputClicked;
        }

        public void InitStage(int stageId)
        {
            var stage = stagesDatabase.GetStage(stageId);

            stageLabel.text = stage.DisplayName;
            stageNumberLabel.text = $"Stage {stageId + 1}";
            stageIcon.sprite = stage.Icon;

            if (save.SelectedStageId > save.MaxReachedStageId)
            {
                lockImage.gameObject.SetActive(true);
                playButton.interactable = false;
                playButton.image.sprite = playButtonDisabledSprite;
            }
            else
            {
                lockImage.gameObject.SetActive(false);
                playButton.interactable = true;
                playButton.image.sprite = playButtonEnabledSprite;
            }

            leftButton.gameObject.SetActive(!save.IsFirstStageSelected);
            rightButton.gameObject.SetActive(save.SelectedStageId != stagesDatabase.StagesCount - 1);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            EasingManager.DoNextFrame(() => EventSystem.current.SetSelectedGameObject(playButton.gameObject));

            GameController.InputManager.onInputChanged += OnInputChanged;
            GameController.InputManager.InputAsset.UI.Settings.performed += OnSettingsInputClicked;
        }

        public void Close()
        {
            gameObject.SetActive(false);

            GameController.InputManager.onInputChanged -= OnInputChanged;
            GameController.InputManager.InputAsset.UI.Settings.performed -= OnSettingsInputClicked;

            CleanupServerCurrencyHandlers();
            CleanupAllDataRefreshHandlers();

            _startInFlight = false;
            _flowStep = FlowStep.None;
        }

        // =========================
        // UI handlers
        // =========================
        public void OnPlayButtonClicked()
        {
            TryStartStage(resetStageData: true, shouldCharge: true);
        }

        private void ConfirmButtonClicked()
        {
            TryStartStage(resetStageData: false, shouldCharge: chargeOnContinue);
        }

        private void TryStartStage(bool resetStageData, bool shouldCharge)
        {
            if (_startInFlight) return;
            _startInFlight = true;

            if (startErrorLabel != null)
                startErrorLabel.text = "";

            SetButtonsInteractable(false);
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            _pendingResetStageData = resetStageData;
            _pendingShouldCharge = shouldCharge;

            if (!shouldCharge)
            {
                ApplySaveAndStart(resetStageData);
                return;
            }

            // Перед списанием делаем AllData refresh (чтобы баланс был актуальный)
            if (refreshAllDataBeforeCharge)
            {
                BeginAllDataRefresh(FlowStep.PreChargeRefresh);
                return;
            }

            // если не делаем refresh — идём сразу к списанию по текущему кэшу
            StartChargeNow();
        }

        // =========================
        // Charge flow
        // =========================
        private void StartChargeNow()
        {
            // Проверка баланса уже после PreChargeRefresh (или без него)
            int current = UserInventory.GetVirtualCurrencyAmount(startCurrencyId);
            if (current < startRunCost)
            {
                ShowStartError("Не хватает энергии / билетов");
                SetButtonsInteractable(true);
                _startInFlight = false;
                _flowStep = FlowStep.None;
                return;
            }

            UserInventory.SuccessSubtractVirtualCurrency += OnServerChargeSuccess;
            UserInventory.ErrorSubtractVirtualCurrency += OnServerChargeError;

            UserInventory.SubtractVirtualCurrency(startCurrencyId, startRunCost);
        }

        private void OnServerChargeSuccess()
        {
            CleanupServerCurrencyHandlers();

            if (refreshAllDataAfterCharge)
            {
                BeginAllDataRefresh(FlowStep.PostChargeRefresh);
                return;
            }

            ApplySaveAndStart(_pendingResetStageData);
        }

        private void OnServerChargeError()
        {
            CleanupServerCurrencyHandlers();

            ShowStartError("Ошибка списания / нет соединения");
            SetButtonsInteractable(true);
            _startInFlight = false;
            _flowStep = FlowStep.None;
        }

        private void CleanupServerCurrencyHandlers()
        {
            UserInventory.SuccessSubtractVirtualCurrency -= OnServerChargeSuccess;
            UserInventory.ErrorSubtractVirtualCurrency -= OnServerChargeError;
        }

        // =========================
        // AllData refresh (both before and after charge)
        // =========================
        private void BeginAllDataRefresh(FlowStep step)
        {
            CleanupAllDataRefreshHandlers();

            _flowStep = step;
            _waitingAllDataRefresh = true;

            // Сигнал: InventoryUpdated (обычно обновляется в рамках RequestUserAllData)
            UserInventory.InventoryUpdated += OnAllDataRefreshed;

            UserDataService.RequestUserAllData();

            _allDataTimeoutCoroutine = StartCoroutine(AllDataRefreshTimeoutCoroutine());
        }

        private void OnAllDataRefreshed()
        {
            if (!_waitingAllDataRefresh) return;

            CleanupAllDataRefreshHandlers();

            if (_flowStep == FlowStep.PreChargeRefresh)
            {
                // теперь баланс актуальный — можно списывать
                StartChargeNow();
                return;
            }

            if (_flowStep == FlowStep.PostChargeRefresh)
            {
                // данные обновили — стартуем
                ApplySaveAndStart(_pendingResetStageData);
                return;
            }

            // fallback
            ApplySaveAndStart(_pendingResetStageData);
        }

        private System.Collections.IEnumerator AllDataRefreshTimeoutCoroutine()
        {
            float t = 0f;
            while (_waitingAllDataRefresh && t < allDataRefreshTimeoutSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_waitingAllDataRefresh) yield break;

            Debug.LogWarning("[Lobby] RequestUserAllData timeout — continuing flow anyway.");
            CleanupAllDataRefreshHandlers();

            if (_flowStep == FlowStep.PreChargeRefresh)
            {
                StartChargeNow();
                yield break;
            }

            if (_flowStep == FlowStep.PostChargeRefresh)
            {
                ApplySaveAndStart(_pendingResetStageData);
                yield break;
            }

            ApplySaveAndStart(_pendingResetStageData);
        }

        private void CleanupAllDataRefreshHandlers()
        {
            _waitingAllDataRefresh = false;

            UserInventory.InventoryUpdated -= OnAllDataRefreshed;

            if (_allDataTimeoutCoroutine != null)
            {
                StopCoroutine(_allDataTimeoutCoroutine);
                _allDataTimeoutCoroutine = null;
            }
        }

        // =========================
        // Start game
        // =========================
        private void ApplySaveAndStart(bool resetStageData)
        {
            save.IsPlaying = true;
            save.ResetStageData = resetStageData;

            if (resetStageData)
            {
                save.Time = 0f;
                save.XP = 0f;
                save.XPLEVEL = 0;
            }

            GameController.LoadStage();

            _startInFlight = false;
            _flowStep = FlowStep.None;
        }

        // =========================
        // UI helpers
        // =========================
        private void SetButtonsInteractable(bool value)
        {
            if (lockImage != null && lockImage.gameObject.activeSelf)
            {
                // play оставляем залоченным
            }
            else
            {
                playButton.interactable = value;
            }

            leftButton.interactable = value;
            rightButton.interactable = value;

            confirmButton.interactable = value;
            cancelButton.interactable = value;
        }

        private void ShowStartError(string msg)
        {
            if (startErrorLabel != null)
                startErrorLabel.text = msg;
            else
                Debug.LogWarning(msg);
        }

        private void IncremenSelectedStageId()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            save.SetSelectedStageId(save.SelectedStageId + 1);

            if (!rightButton.gameObject.activeSelf)
            {
                if (leftButton.gameObject.activeSelf)
                    EventSystem.current.SetSelectedGameObject(leftButton.gameObject);
                else
                    EventSystem.current.SetSelectedGameObject(playButton.gameObject);
            }
        }

        private void DecrementSelectedStageId()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            save.SetSelectedStageId(save.SelectedStageId - 1);

            if (!leftButton.gameObject.activeSelf)
            {
                if (rightButton.gameObject.activeSelf)
                    EventSystem.current.SetSelectedGameObject(rightButton.gameObject);
                else
                    EventSystem.current.SetSelectedGameObject(playButton.gameObject);
            }
        }

        private void OnDestroy()
        {
            if (save != null)
                save.onSelectedStageChanged -= InitStage;

            GameController.InputManager.onInputChanged -= OnInputChanged;

            CleanupServerCurrencyHandlers();
            CleanupAllDataRefreshHandlers();
        }

        private void OnSettingsInputClicked(InputAction.CallbackContext context)
        {
            /*settingsButton.onClick?.Invoke();*/
        }

        private void CancelButtonClicked()
        {
            if (_startInFlight)
            {
                CleanupServerCurrencyHandlers();
                CleanupAllDataRefreshHandlers();

                _startInFlight = false;
                _flowStep = FlowStep.None;

                SetButtonsInteractable(true);
            }

            save.IsPlaying = false;

            continueBackgroundImage.DoAlpha(0, 0.3f).SetOnFinish(() => continueBackgroundImage.gameObject.SetActive(false));
            contituePopupRect.DoAnchorPosition(Vector2.down * 2500, 0.3f)
                .SetEasing(EasingType.SineIn)
                .SetOnFinish(() => contituePopupRect.gameObject.SetActive(false));

            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }

        private void OnInputChanged(InputType prevInputType, InputType inputType)
        {
            if (prevInputType == InputType.UIJoystick)
            {
                if (continueBackgroundImage.gameObject.activeSelf)
                    EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
                else
                    EventSystem.current.SetSelectedGameObject(playButton.gameObject);
            }
        }
    }
}



