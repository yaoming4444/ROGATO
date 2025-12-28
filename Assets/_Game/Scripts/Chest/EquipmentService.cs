using System.Collections.Generic;

namespace GameCore.Items
{
    /// <summary>
    /// Convenience stat helpers that work on top of PlayerState + ItemDatabase.
    ///
    /// NOTE:
    /// You already have similar helpers inside GameInstance.
    /// This service can be used by UI or gameplay to compute totals.
    /// </summary>
    public static class EquipmentService
    {
        /// <summary>
        /// Gets currently equipped itemId from PlayerState for a given slot.
        /// Returns empty string if not equipped or invalid state.
        ///
        /// IMPORTANT:
        /// This method assumes GameInstance is already created and State is not null.
        /// </summary>
        public static string GetEquippedId(EquipSlot slot)
        {
            var eq = GameCore.GameInstance.I.State.Equipped;

            // Hard-coded 12 check (works only if you always have 12 slots).
            // If you change slot count, prefer PlayerState.SlotCount.
            if (eq == null || eq.Length < 12) return "";

            return eq[(int)slot];
        }

        /// <summary>
        /// Resolves equipped ItemDef from the database.
        /// Returns null if not equipped or itemId is not found.
        /// </summary>
        public static ItemDef GetEquippedDef(EquipSlot slot)
        {
            var id = GetEquippedId(slot);
            if (string.IsNullOrWhiteSpace(id)) return null;

            return ItemDatabase.I.GetById(id);
        }

        /// <summary>
        /// Sums base stats of all equipped items.
        /// </summary>
        public static ItemStats GetTotalBaseStats()
        {
            var total = new ItemStats();

            // Iterate through all slots (assumes enum values are contiguous)
            foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
            {
                var def = GetEquippedDef(slot);
                if (def == null) continue;

                total += def.Stats;
            }

            return total;
        }

        /// <summary>
        /// Aggregates extra stats (percentage / flat bonuses) across all equipped items.
        ///
        /// Output:
        /// Dictionary<ExtraStatType, float> where float is summed Value.
        /// </summary>
        public static Dictionary<ExtraStatType, float> GetTotalExtraStats()
        {
            var map = new Dictionary<ExtraStatType, float>();

            foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
            {
                var def = GetEquippedDef(slot);
                if (def == null) continue;

                var arr = def.ExtraStats;
                if (arr == null || arr.Length == 0) continue;

                for (int k = 0; k < arr.Length; k++)
                {
                    var s = arr[k];

                    if (map.TryGetValue(s.Type, out var v))
                        map[s.Type] = v + s.Value;
                    else
                        map[s.Type] = s.Value;
                }
            }

            return map;
        }
    }
}


