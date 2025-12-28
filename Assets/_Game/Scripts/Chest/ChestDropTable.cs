using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Items
{
    /// <summary>
    /// ScriptableObject that defines chest rarity chances per "Chest Level".
    ///
    /// You can create this asset via:
    /// Assets -> Create -> Game -> Chest -> ChestDropTable
    ///
    /// At runtime we call RollRarity(chestLevel) to roll an item rarity based on:
    ///  - the current player's ChestLevel
    ///  - the configured chance weights for that level
    ///
    /// NOTE:
    /// - "Chance" is treated as a weight (not necessarily summing to 100).
    /// - If the level is not found, we fallback to the closest/first entry.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Chest/ChestDropTable", fileName = "ChestDropTable")]
    public class ChestDropTable : ScriptableObject
    {
        [Serializable]
        public struct RarityChance
        {
            public Rarity Rarity;

            // Weight in percent-like units (we treat it as a weight).
            // Example: 80 + 15 + 5 = 100, but it can be any positive values.
            [Range(0, 100)] public float Chance; // %
        }

        [Serializable]
        public struct LevelEntry
        {
            public int Level;

            // A list of rarity weights for this chest level.
            public List<RarityChance> Chances;
        }

        [Header("Levels")]
        [SerializeField] private List<LevelEntry> levels = new List<LevelEntry>();

        /// <summary>
        /// Rolls a rarity for a given chest level.
        /// Logic:
        /// 1) Find matching LevelEntry.
        /// 2) Compute total positive weight sum.
        /// 3) Roll random in [0..sum)
        /// 4) Walk cumulative weights and pick the first that crosses the roll.
        ///
        /// If configuration is invalid/empty, returns the lowest rarity (G) as a safe fallback.
        /// </summary>
        public Rarity RollRarity(int chestLevel)
        {
            if (levels == null || levels.Count == 0)
                return Rarity.G;

            // Find best matching entry.
            // If exact level not found, we fallback to first entry (simple behavior).
            LevelEntry entry = levels[0];
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].Level == chestLevel)
                {
                    entry = levels[i];
                    break;
                }
            }

            // If this entry has no chances, fallback.
            if (entry.Chances == null || entry.Chances.Count == 0)
                return Rarity.G;

            // Sum all non-negative weights.
            float sum = 0f;
            for (int i = 0; i < entry.Chances.Count; i++)
                sum += Mathf.Max(0f, entry.Chances[i].Chance);

            if (sum <= 0f)
                return Rarity.G;

            // Roll in [0..sum)
            float r = UnityEngine.Random.Range(0f, sum);
            float acc = 0f;

            for (int i = 0; i < entry.Chances.Count; i++)
            {
                acc += Mathf.Max(0f, entry.Chances[i].Chance);
                if (r <= acc) return entry.Chances[i].Rarity;
            }

            // Fallback (should not happen, but safe)
            return entry.Chances[entry.Chances.Count - 1].Rarity;
        }
    }
}


