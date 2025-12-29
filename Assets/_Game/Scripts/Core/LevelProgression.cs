using UnityEngine;

namespace GameCore.Progression
{
    /// <summary>
    /// Level progression driven by EXP thresholds.
    ///
    /// IMPORTANT DESIGN CHOICE:
    /// - PlayerState.Exp is treated as TOTAL EXP (never decreases).
    /// - Level ups happen when totalExp reaches the required total exp for the next level.
    ///
    /// Data:
    /// expToNext[0] = exp needed to go from level 1 -> 2
    /// expToNext[1] = exp needed to go from level 2 -> 3
    /// etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Progression/Level Progression", fileName = "LevelProgression")]
    public class LevelProgression : ScriptableObject
    {
        [Min(1)]
        [SerializeField] private int startLevel = 1;

        [Tooltip("expToNext[0] is exp needed from level 1 to 2, expToNext[1] from 2 to 3, etc.")]
        [SerializeField] private int[] expToNext;

        public int StartLevel => startLevel;

        /// <summary>
        /// If expToNext has N entries, max achievable level is startLevel + N.
        /// Example: startLevel=1, N=99 => max level = 100.
        /// </summary>
        public int MaxLevel => startLevel + (expToNext?.Length ?? 0);

        public bool IsMaxLevel(int level) => level >= MaxLevel;

        /// <summary>
        /// Returns exp needed to go from "level" to "level + 1".
        /// </summary>
        public int GetExpToNext(int level)
        {
            if (expToNext == null || expToNext.Length == 0)
                return int.MaxValue;

            if (level < startLevel) level = startLevel;

            int idx = level - startLevel;
            if (idx < 0) idx = 0;

            // If level is above table, treat as max level.
            if (idx >= expToNext.Length)
                return int.MaxValue;

            return Mathf.Max(1, expToNext[idx]);
        }

        /// <summary>
        /// Total EXP required to reach the specified level.
        /// - Required EXP for StartLevel is 0.
        /// - Required EXP for level 2 is expToNext[0].
        /// - Required EXP for level 3 is expToNext[0] + expToNext[1], etc.
        /// </summary>
        public int GetRequiredTotalExpForLevel(int level)
        {
            if (level <= startLevel) return 0;
            if (expToNext == null || expToNext.Length == 0) return int.MaxValue;

            // clamp above max
            if (level > MaxLevel) level = MaxLevel;

            int steps = level - startLevel; // how many "level ups" needed
            int sum = 0;

            // sum expToNext[0..steps-1]
            for (int i = 0; i < steps; i++)
            {
                int v = expToNext[i];
                sum += Mathf.Max(1, v);

                // protection from overflow / crazy values
                if (sum < 0) return int.MaxValue;
            }

            return sum;
        }

        /// <summary>
        /// Total EXP required to reach next level from currentLevel.
        /// Example: currentLevel=1 -> returns expToNext[0]
        /// </summary>
        public int GetTotalExpToNextLevel(int currentLevel)
        {
            if (IsMaxLevel(currentLevel)) return int.MaxValue;
            return GetRequiredTotalExpForLevel(currentLevel + 1);
        }

        /// <summary>
        /// True if totalExp is enough to go from currentLevel to currentLevel+1.
        /// </summary>
        public bool CanLevelUp(int currentLevel, int totalExp)
        {
            if (IsMaxLevel(currentLevel)) return false;
            if (totalExp < 0) totalExp = 0;

            int needTotal = GetTotalExpToNextLevel(currentLevel);
            return totalExp >= needTotal;
        }

        /// <summary>
        /// Calculates level based on totalExp.
        /// Useful when loading old saves or if you want to "recompute" level.
        /// </summary>
        public int GetLevelForTotalExp(int totalExp)
        {
            if (totalExp < 0) totalExp = 0;

            int lvl = startLevel;

            if (expToNext == null || expToNext.Length == 0)
                return lvl;

            int required = 0;

            for (int i = 0; i < expToNext.Length; i++)
            {
                required += Mathf.Max(1, expToNext[i]);

                if (totalExp < required)
                    return lvl;

                lvl++;
            }

            // reached/over max
            return MaxLevel;
        }
    }
}


