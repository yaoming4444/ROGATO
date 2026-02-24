using System.Collections.Generic;
using UnityEngine;
using OctoberStudio.UI;

namespace OctoberStudio
{
    public class StageRewardsPanelUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform contentRoot;                  // сюда спавним слоты (LayoutGroup)
        [SerializeField] private StageRewardSlotView rewardSlotPrefab;   // твой слот
        [SerializeField] private Sprite defaultSlotBackground;           // фон слота (если один общий)

        [Header("Options")]
        [SerializeField] private bool clearOnAwake = false;
        [SerializeField] private bool hideIfEmpty = false;

        private readonly List<StageRewardSlotView> _spawnedSlots = new();

        private void Awake()
        {
            if (clearOnAwake)
                Clear();
        }

        /// <summary>
        /// ѕоказать награды пр€мо из StageData (сырые значени€ из stage).
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

                // ≈сли у StageReward нет Icon/Amount (другие названи€) Ч просто помен€й тут
                CreateSlot(reward.BackgroundIcon, reward.Icon, amount);
            }

            UpdateActiveState();
        }

        /// <summary>
        /// ѕоказать уже рассчитанные награды (после StageRewardService).
        /// Ёто лучший вариант дл€ Complete/Failed, т.к. UI = фактическа€ выдача.
        /// </summary>
        public void ShowCalculatedRewards(
            StageRewardService.CalculatedStageRewards calculated,
            Sprite coinsIcon,
            Sprite expIcon)
        {
            Clear();

            if (calculated.Coins > 0)
                CreateSlot(defaultSlotBackground, coinsIcon, calculated.Coins);

            if (calculated.Exp > 0)
                CreateSlot(defaultSlotBackground, expIcon, calculated.Exp);

            UpdateActiveState();
        }

        /// <summary>
        /// ”ниверсальный вариант Ч вручную передать список наград дл€ отображени€.
        /// </summary>
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

        [System.Serializable]
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