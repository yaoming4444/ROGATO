using System;
using System.Collections.Generic;
using UnityEngine;
using IDosGames;
using GameCore;

namespace OctoberStudio
{
    /// <summary>
    /// Сервис расчета и выдачи наград за Stage.
    ///
    /// Ожидает, что в StageData.Rewards лежат награды с типами:
    /// - StageRewardType.Coins
    /// - StageRewardType.Exp
    ///
    /// Coins выдаются через RewardSystem.ClaimCoinReward(...)
    /// Exp выдается через GameInstance.AddExp(...)
    /// </summary>
    public static class StageRewardService
    {
        // --- Настройки фейла (можно потом вынести в конфиг/ScriptableObject) ---
        private const float DEFAULT_STAGE_TOTAL_SECONDS = 360f; // 6 минут
        private const float FAIL_THRESHOLD_SECONDS = 180f;      // 3 минуты

        private const float FAILED_COINS_MULTIPLIER = 0.40f;    // 40% коинов
        private const float FAILED_EXP_MULTIPLIER = 0.50f;      // 50% опыта

        private const int EARLY_FAIL_CONSOLATION_EXP = 5;       // если умер раньше 3 мин

        // ----------------------------------------------------------------------

        public enum RewardOutcomeType
        {
            CompleteFull,
            FailedPartial,
            FailedConsolation
        }

        [Serializable]
        public struct CalculatedStageRewards
        {
            public RewardOutcomeType OutcomeType;
            public int Coins;
            public int Exp;
            public float ElapsedSeconds;
            public float TotalStageSeconds;

            public bool HasAnyReward => Coins > 0 || Exp > 0;
        }

        /// <summary>
        /// Рассчитать награды за успешное прохождение (100%).
        /// </summary>
        public static CalculatedStageRewards CalculateCompleteRewards(StageData stageData, float totalStageSeconds = DEFAULT_STAGE_TOTAL_SECONDS)
        {
            var result = new CalculatedStageRewards
            {
                OutcomeType = RewardOutcomeType.CompleteFull,
                Coins = 0,
                Exp = 0,
                ElapsedSeconds = totalStageSeconds,
                TotalStageSeconds = totalStageSeconds
            };

            if (stageData == null)
            {
                Debug.LogWarning("[StageRewardService] CalculateCompleteRewards: stageData is null");
                return result;
            }

            AddRewardsFromStageData(stageData, ref result, 1f, 1f, grantCoins: true, grantExp: true);

            return result;
        }

        /// <summary>
        /// Рассчитать награды за поражение:
        /// - Если elapsed >= threshold (по умолчанию 3 мин): частичные coins+exp
        /// - Иначе: только небольшой consolation EXP
        /// </summary>
        public static CalculatedStageRewards CalculateFailedRewards(
            StageData stageData,
            float elapsedSeconds,
            float totalStageSeconds = DEFAULT_STAGE_TOTAL_SECONDS)
        {
            var result = new CalculatedStageRewards
            {
                OutcomeType = RewardOutcomeType.FailedConsolation,
                Coins = 0,
                Exp = 0,
                ElapsedSeconds = Mathf.Max(0f, elapsedSeconds),
                TotalStageSeconds = Mathf.Max(1f, totalStageSeconds)
            };

            if (stageData == null)
            {
                Debug.LogWarning("[StageRewardService] CalculateFailedRewards: stageData is null");
                // Даже если stageData null, можно выдать consolation exp (если хочешь — оставь 0)
                if (elapsedSeconds < FAIL_THRESHOLD_SECONDS)
                    result.Exp = EARLY_FAIL_CONSOLATION_EXP;

                return result;
            }

            bool passedThreshold = elapsedSeconds >= FAIL_THRESHOLD_SECONDS;

            if (passedThreshold)
            {
                result.OutcomeType = RewardOutcomeType.FailedPartial;

                AddRewardsFromStageData(
                    stageData,
                    ref result,
                    FAILED_COINS_MULTIPLIER,
                    FAILED_EXP_MULTIPLIER,
                    grantCoins: true,
                    grantExp: true
                );
            }
            else
            {
                result.OutcomeType = RewardOutcomeType.FailedConsolation;
                result.Coins = 0;
                result.Exp = EARLY_FAIL_CONSOLATION_EXP;
            }

            return result;
        }

        /// <summary>
        /// Выдать уже рассчитанные награды.
        /// Используй этот метод после того, как показал награды в UI (чтобы UI и факт совпадали).
        /// </summary>
        public static void GrantRewards(CalculatedStageRewards rewards)
        {
            if (!rewards.HasAnyReward)
            {
                Debug.Log("[StageRewardService] GrantRewards: nothing to grant");
                return;
            }

            // ВАЖНО:
            // Если у тебя у RewardSystem.ClaimCoinReward другая сигнатура (например с ключом валюты / callback),
            // просто подправь этот блок под свою реализацию.
            if (rewards.Coins > 0)
            {
                try
                {
                    ClaimRewardSystem.ClaimCoinReward(rewards.Coins, 0);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[StageRewardService] ClaimCoinReward failed: {e}");
                }
            }

            if (rewards.Exp > 0)
            {
                try
                {
                    GameInstance.I.AddExp(rewards.Exp);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[StageRewardService] AddExp failed: {e}");
                }
            }
        }

        /// <summary>
        /// Удобный метод: сразу рассчитать и выдать награды за победу.
        /// </summary>
        public static CalculatedStageRewards CalculateAndGrantCompleteRewards(StageData stageData, float totalStageSeconds = DEFAULT_STAGE_TOTAL_SECONDS)
        {
            var rewards = CalculateCompleteRewards(stageData, totalStageSeconds);
            GrantRewards(rewards);
            return rewards;
        }

        /// <summary>
        /// Удобный метод: сразу рассчитать и выдать награды за поражение.
        /// </summary>
        public static CalculatedStageRewards CalculateAndGrantFailedRewards(StageData stageData, float elapsedSeconds, float totalStageSeconds = DEFAULT_STAGE_TOTAL_SECONDS)
        {
            var rewards = CalculateFailedRewards(stageData, elapsedSeconds, totalStageSeconds);
            GrantRewards(rewards);
            return rewards;
        }

        // ----------------------------------------------------------------------
        // Internal helpers
        // ----------------------------------------------------------------------

        private static void AddRewardsFromStageData(
            StageData stageData,
            ref CalculatedStageRewards result,
            float coinsMultiplier,
            float expMultiplier,
            bool grantCoins,
            bool grantExp)
        {
            // Ниже предполагается, что:
            // - StageData имеет свойство Rewards
            // - Rewards это коллекция StageReward
            // - У StageReward есть:
            //      int Amount
            //      StageRewardType RewardType
            //
            // Если у тебя названия свойств другие (amount/RewardAmount и т.п.) — просто поправь здесь.

            IReadOnlyList<StageReward> rewards = stageData.Rewards;
            if (rewards == null)
                return;

            for (int i = 0; i < rewards.Count; i++)
            {
                StageReward reward = rewards[i];
                if (reward == null) continue;

                int baseAmount = Mathf.Max(0, reward.Amount);
                if (baseAmount <= 0) continue;

                switch (reward.RewardType)
                {
                    case StageRewardType.Coins:
                        if (grantCoins)
                            result.Coins += Mathf.RoundToInt(baseAmount * coinsMultiplier);
                        break;

                    case StageRewardType.Exp:
                        if (grantExp)
                            result.Exp += Mathf.RoundToInt(baseAmount * expMultiplier);
                        break;

                    default:
                        Debug.LogWarning($"[StageRewardService] Unknown reward type: {reward.RewardType}");
                        break;
                }
            }

            // Подстраховка: не уходим в минус
            if (result.Coins < 0) result.Coins = 0;
            if (result.Exp < 0) result.Exp = 0;
        }
    }
}