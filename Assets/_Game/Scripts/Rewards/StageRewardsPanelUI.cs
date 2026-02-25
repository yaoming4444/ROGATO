using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OctoberStudio.UI;

namespace OctoberStudio
{
    public class StageRewardsPanelUI : MonoBehaviour
    {
        public enum ResultMode
        {
            Complete,
            Failed
        }

        [Header("Stage Header (auto from StageController.Stage)")]
        [SerializeField] private TMP_Text stageNameText;
        [SerializeField] private TMP_Text stageNumberText;
        [SerializeField] private Image stageImage;

        [Header("Result Flags (debug/inspector view)")]
        [SerializeField] private bool isFailed;
        [SerializeField] private bool isComplete = true;

        [Header("Failed Mode UI")]
        [Tooltip("These 4 objects will be hidden in Failed mode.")]
        [SerializeField] private GameObject failedHideObject1;
        [SerializeField] private GameObject failedHideObject2;
        [SerializeField] private GameObject failedHideObject3;
        [SerializeField] private GameObject failedHideObject4;

        [Tooltip("Shown only in Failed mode.")]
        [SerializeField] private TMP_Text survivedTimeText;

        [Header("Rewards Slots")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private StageRewardSlotView rewardSlotPrefab;
        [SerializeField] private Sprite defaultSlotBackground;

        [Header("Options")]
        [SerializeField] private bool clearOnAwake = false;
        [SerializeField] private bool hideIfEmpty = false;

        private readonly List<StageRewardSlotView> _spawnedSlots = new();

        private void Awake()
        {
            if (clearOnAwake)
                Clear();

            // Автообновим шапку из текущего stage при старте (если есть)
            RefreshStageHeaderFromCurrentStage();

            // Применим текущий режим из инспектора
            ApplyMode(isFailed ? ResultMode.Failed : ResultMode.Complete, 0f);
        }

        // =========================================================
        // PUBLIC API
        // =========================================================

        /// <summary>
        /// Подтягивает данные текущего stage напрямую из StageController.Stage
        /// (имя, номер, картинка).
        /// </summary>
        public void RefreshStageHeaderFromCurrentStage()
        {
            var stageData = StageController.Stage;
            SetStageInfo(stageData);
        }

        /// <summary>
        /// Полная настройка шапки + режима из текущего StageController.Stage.
        /// </summary>
        public void SetupFromCurrentStage(ResultMode mode, float survivedSeconds = 0f)
        {
            RefreshStageHeaderFromCurrentStage();
            SetResultMode(mode, survivedSeconds);
        }

        /// <summary>
        /// Ручная установка stage данных (если вдруг нужно переиспользовать панель не в рантайме stage).
        /// </summary>
        public void SetStageInfo(StageData stageData)
        {
            if (stageData == null)
            {
                if (stageNameText != null) stageNameText.text = string.Empty;
                if (stageNumberText != null) stageNumberText.text = string.Empty;

                if (stageImage != null)
                {
                    stageImage.sprite = null;
                    stageImage.enabled = false;
                }

                Debug.LogWarning("[StageRewardsPanelUI] SetStageInfo: stageData is null");
                return;
            }

            // Имя
            if (stageNameText != null)
                stageNameText.text = ResolveStageName(stageData);

            // Номер
            if (stageNumberText != null)
                stageNumberText.text = ResolveStageNumberText(stageData);

            // Картинка
            if (stageImage != null)
            {
                Sprite s = ResolveStageSprite(stageData);
                stageImage.sprite = s;
                stageImage.enabled = s != null;
            }
        }

        /// <summary>
        /// Режим результата: Complete / Failed.
        /// В Failed скрывает 4 объекта и показывает время выживания.
        /// </summary>
        public void SetResultMode(ResultMode mode, float survivedSeconds = 0f)
        {
            ApplyMode(mode, survivedSeconds);
        }

        /// <summary>
        /// Удобный метод: только обновить время выживания (для fail).
        /// </summary>
        public void SetSurvivedTime(float survivedSeconds)
        {
            if (survivedTimeText == null) return;

            survivedTimeText.text = FormatSurvivedTime(survivedSeconds);
        }

        /// <summary>
        /// Показать награды из StageData (сырые).
        /// </summary>
        public void ShowStageRewards(StageData stageData)
        {
            Clear();

            if (stageData == null)
            {
                UpdateActiveState();
                return;
            }

            var rewards = stageData.Rewards;
            if (rewards == null || rewards.Count == 0)
            {
                UpdateActiveState();
                return;
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                if (reward == null) continue;

                int amount = Mathf.Max(0, reward.Amount);
                if (amount <= 0) continue;

                CreateSlot(defaultSlotBackground, reward.Icon, amount);
            }

            UpdateActiveState();
        }

        /// <summary>
        /// Показать рассчитанные награды (рекомендуется для failed/complete экранов).
        /// </summary>
        public void ShowCalculatedRewards(StageRewardService.CalculatedStageRewards calculated, Sprite coinsIcon, Sprite expIcon)
        {
            Clear();

            if (calculated.Coins > 0)
                CreateSlot(defaultSlotBackground, coinsIcon, calculated.Coins);

            if (calculated.Exp > 0)
                CreateSlot(defaultSlotBackground, expIcon, calculated.Exp);

            UpdateActiveState();
        }

        public void ShowRewards(IReadOnlyList<RewardViewData> rewards)
        {
            Clear();

            if (rewards == null || rewards.Count == 0)
            {
                UpdateActiveState();
                return;
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                var r = rewards[i];
                if (r.Amount <= 0) continue;

                CreateSlot(r.BackgroundIcon != null ? r.BackgroundIcon : defaultSlotBackground, r.RewardIcon, r.Amount);
            }

            UpdateActiveState();
        }

        public void Clear()
        {
            for (int i = _spawnedSlots.Count - 1; i >= 0; i--)
            {
                if (_spawnedSlots[i] != null)
                    Destroy(_spawnedSlots[i].gameObject);
            }

            _spawnedSlots.Clear();
            UpdateActiveState();
        }

        // =========================================================
        // INTERNAL
        // =========================================================

        private void ApplyMode(ResultMode mode, float survivedSeconds)
        {
            bool failed = mode == ResultMode.Failed;
            bool complete = mode == ResultMode.Complete;

            isFailed = failed;
            isComplete = complete;

            // В failed скрываем 4 объекта
            SetGOActive(failedHideObject1, !failed);
            SetGOActive(failedHideObject2, !failed);
            SetGOActive(failedHideObject3, !failed);
            SetGOActive(failedHideObject4, !failed);

            // Время выживания показываем только в failed
            if (survivedTimeText != null)
            {
                survivedTimeText.gameObject.SetActive(failed);

                if (failed)
                {
                    float time = Mathf.Max(0f, (float)StageController.Director.time);
                    survivedTimeText.text = FormatSurvivedTime(time);
                }
            }
        }

        private void SetGOActive(GameObject go, bool active)
        {
            if (go != null)
                go.SetActive(active);
        }

        private string FormatSurvivedTime(float seconds)
        {
            var ts = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
            return $"{ts:mm\\:ss}";
        }

        private void CreateSlot(Sprite backgroundIcon, Sprite rewardIcon, int amount)
        {
            if (contentRoot == null)
            {
                Debug.LogError("[StageRewardsPanelUI] contentRoot is not assigned");
                return;
            }

            if (rewardSlotPrefab == null)
            {
                Debug.LogError("[StageRewardsPanelUI] rewardSlotPrefab is not assigned");
                return;
            }

            var slot = Instantiate(rewardSlotPrefab, contentRoot);
            slot.Bind(backgroundIcon, rewardIcon, amount);
            _spawnedSlots.Add(slot);
        }

        private void UpdateActiveState()
        {
            if (!hideIfEmpty) return;

            bool hasSlots = _spawnedSlots.Count > 0;
            if (gameObject.activeSelf != hasSlots)
                gameObject.SetActive(hasSlots);
        }

        // =========================================================
        // STAGE DATA RESOLVERS (через reflection, чтобы не ломаться от имен полей)
        // =========================================================

        private string ResolveStageName(StageData stageData)
        {
            // Попробуем частые варианты имен
            object value =
                GetMemberValue(stageData, "StageName") ??
                GetMemberValue(stageData, "Name") ??
                GetMemberValue(stageData, "Title") ??
                GetMemberValue(stageData, "DisplayName");

            if (value is string s && !string.IsNullOrWhiteSpace(s))
                return s;

            // Фоллбек — имя asset
            return stageData.name;
        }

        private string ResolveStageNumberText(StageData stageData)
        {
            object value =
                GetMemberValue(stageData, "StageId") ??
                GetMemberValue(stageData, "Id") ??
                GetMemberValue(stageData, "StageNumber") ??
                GetMemberValue(stageData, "Number") ??
                GetMemberValue(stageData, "Index");

            if (value != null)
            {
                try
                {
                    int num = Convert.ToInt32(value);
                    return $"Stage {num}";
                }
                catch
                {
                    return $"Stage {value}";
                }
            }

            // Если не нашли — просто "Stage"
            return "Stage";
        }

        private Sprite ResolveStageSprite(StageData stageData)
        {
            object value =
                GetMemberValue(stageData, "Image") ??
                GetMemberValue(stageData, "Icon") ??
                GetMemberValue(stageData, "Preview") ??
                GetMemberValue(stageData, "Sprite") ??
                GetMemberValue(stageData, "StageSprite");

            return value as Sprite;
        }

        private object GetMemberValue(object target, string memberName)
        {
            if (target == null) return null;

            var type = target.GetType();

            // field
            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                return field.GetValue(target);

            // property
            PropertyInfo prop = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.CanRead)
                return prop.GetValue(target);

            return null;
        }

        [Serializable]
        public struct RewardViewData
        {
            public Sprite BackgroundIcon;
            public Sprite RewardIcon;
            public int Amount;

            public RewardViewData(Sprite rewardIcon, int amount, Sprite backgroundIcon = null)
            {
                RewardIcon = rewardIcon;
                Amount = amount;
                BackgroundIcon = backgroundIcon;
            }
        }
    }
}